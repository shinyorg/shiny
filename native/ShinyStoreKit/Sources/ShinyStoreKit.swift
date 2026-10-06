import Foundation
import StoreKit
import UIKit

// C-ABI bridge over StoreKit 2 for Shiny.Mobile.InAppPurchases.
// StoreKit 2 is Swift-only, so .NET cannot bind it directly. Every call exchanges UTF-8 JSON and completes by
// invoking the callback exactly once, synchronously inside withCString - the managed side copies the strings
// before returning, so nothing needs to be freed across the boundary.

public typealias ShinyStoreKitCallback = @convention(c) (UnsafeMutableRawPointer?, UnsafePointer<CChar>?, UnsafePointer<CChar>?) -> Void
public typealias ShinyStoreKitUpdateCallback = @convention(c) (UnsafePointer<CChar>?) -> Void


struct CallbackContext: @unchecked Sendable {
    let raw: UnsafeMutableRawPointer?
    let callback: ShinyStoreKitCallback

    func success(_ value: Any) {
        guard
            let data = try? JSONSerialization.data(withJSONObject: value, options: [.fragmentsAllowed]),
            let json = String(data: data, encoding: .utf8)
        else {
            self.failure(BridgeError(code: "Unknown", message: "Failed to serialize StoreKit result"))
            return
        }
        json.withCString { self.callback(self.raw, $0, nil) }
    }

    func failure(_ error: Error) {
        let info = mapError(error)
        var dict: [String: Any] = ["code": info.code, "message": info.message]
        if let native = info.native {
            dict["native"] = native
        }
        let data = (try? JSONSerialization.data(withJSONObject: dict)) ?? Data("{\"code\":\"Unknown\"}".utf8)
        let json = String(data: data, encoding: .utf8) ?? "{\"code\":\"Unknown\"}"
        json.withCString { self.callback(self.raw, nil, $0) }
    }
}


struct BridgeError: Error {
    let code: String
    let message: String
}


// MARK: - Exports

@_cdecl("shiny_storekit_can_make_payments")
public func shiny_storekit_can_make_payments() -> Bool {
    AppStore.canMakePayments
}


@_cdecl("shiny_storekit_get_products")
public func shiny_storekit_get_products(_ idsJson: UnsafePointer<CChar>, _ context: UnsafeMutableRawPointer?, _ callback: ShinyStoreKitCallback) {
    let json = String(cString: idsJson)
    let ctx = CallbackContext(raw: context, callback: callback)

    Task {
        do {
            let ids = (try JSONSerialization.jsonObject(with: Data(json.utf8)) as? [String]) ?? []
            let products = try await Product.products(for: ids)
            var result: [[String: Any]] = []
            for product in products {
                result.append(await serializeProduct(product))
            }
            ctx.success(result)
        } catch {
            ctx.failure(error)
        }
    }
}


@_cdecl("shiny_storekit_purchase")
public func shiny_storekit_purchase(_ productId: UnsafePointer<CChar>, _ optionsJson: UnsafePointer<CChar>, _ context: UnsafeMutableRawPointer?, _ callback: ShinyStoreKitCallback) {
    let id = String(cString: productId)
    let json = String(cString: optionsJson)
    let ctx = CallbackContext(raw: context, callback: callback)

    Task { @MainActor in
        do {
            guard let product = try await Product.products(for: [id]).first else {
                throw BridgeError(code: "ProductNotFound", message: "Product '\(id)' was not found in the App Store")
            }
            let options = (try? JSONSerialization.jsonObject(with: Data(json.utf8)) as? [String: Any]) ?? [:]
            var purchaseOptions = Set<Product.PurchaseOption>()

            if let token = options["appAccountToken"] as? String, let uuid = UUID(uuidString: token) {
                purchaseOptions.insert(.appAccountToken(uuid))
            }
            if let quantity = options["quantity"] as? Int, quantity > 1 {
                purchaseOptions.insert(.quantity(quantity))
            }

            let result: Product.PurchaseResult
            if #available(iOS 17.0, *), let scene = activeWindowScene() {
                result = try await product.purchase(confirmIn: scene, options: purchaseOptions)
            } else {
                result = try await product.purchase(options: purchaseOptions)
            }

            switch result {
            case .success(let verification):
                let transaction = try verified(verification)
                let payload = await serializeTransaction(transaction, jws: verification.jwsRepresentation, isFinished: false)
                ctx.success(["status": "success", "transaction": payload])

            case .userCancelled:
                ctx.success(["status": "cancelled"])

            case .pending:
                ctx.success(["status": "pending"])

            @unknown default:
                throw BridgeError(code: "Unknown", message: "Unknown StoreKit purchase result")
            }
        } catch {
            ctx.failure(error)
        }
    }
}


@_cdecl("shiny_storekit_current_entitlements")
public func shiny_storekit_current_entitlements(_ context: UnsafeMutableRawPointer?, _ callback: ShinyStoreKitCallback) {
    let ctx = CallbackContext(raw: context, callback: callback)
    Task {
        let unfinished = await unfinishedTransactionIds()
        var result: [[String: Any]] = []
        for await verification in Transaction.currentEntitlements {
            // an entitlement that fails on-device verification is never reported as owned
            guard case .verified(let transaction) = verification else { continue }
            result.append(await serializeTransaction(
                transaction,
                jws: verification.jwsRepresentation,
                isFinished: !unfinished.contains(transaction.id)
            ))
        }
        ctx.success(result)
    }
}


@_cdecl("shiny_storekit_unfinished")
public func shiny_storekit_unfinished(_ context: UnsafeMutableRawPointer?, _ callback: ShinyStoreKitCallback) {
    let ctx = CallbackContext(raw: context, callback: callback)
    Task {
        var result: [[String: Any]] = []
        for await verification in Transaction.unfinished {
            guard case .verified(let transaction) = verification else { continue }
            result.append(await serializeTransaction(transaction, jws: verification.jwsRepresentation, isFinished: false))
        }
        ctx.success(result)
    }
}


@_cdecl("shiny_storekit_finish")
public func shiny_storekit_finish(_ transactionId: UnsafePointer<CChar>, _ context: UnsafeMutableRawPointer?, _ callback: ShinyStoreKitCallback) {
    let idString = String(cString: transactionId)
    let ctx = CallbackContext(raw: context, callback: callback)

    Task {
        guard let id = UInt64(idString) else {
            ctx.failure(BridgeError(code: "DeveloperError", message: "'\(idString)' is not a StoreKit transaction id"))
            return
        }
        for await verification in Transaction.unfinished {
            // unverified transactions are still finished - otherwise they are redelivered forever
            let transaction = verification.unsafePayloadValue
            if transaction.id == id {
                await transaction.finish()
                ctx.success(["finished": true])
                return
            }
        }
        // not in the unfinished queue - already finished, nothing to do
        ctx.success(["finished": false])
    }
}


@_cdecl("shiny_storekit_sync")
public func shiny_storekit_sync(_ context: UnsafeMutableRawPointer?, _ callback: ShinyStoreKitCallback) {
    let ctx = CallbackContext(raw: context, callback: callback)
    Task {
        do {
            try await AppStore.sync()
            ctx.success(["synced": true])
        } catch {
            ctx.failure(error)
        }
    }
}


@_cdecl("shiny_storekit_show_manage_subscriptions")
public func shiny_storekit_show_manage_subscriptions(_ context: UnsafeMutableRawPointer?, _ callback: ShinyStoreKitCallback) {
    let ctx = CallbackContext(raw: context, callback: callback)
    Task { @MainActor in
        do {
            guard let scene = activeWindowScene() else {
                throw BridgeError(code: "NoUserInterface", message: "No active window scene to present subscription management")
            }
            try await AppStore.showManageSubscriptions(in: scene)
            ctx.success(["shown": true])
        } catch {
            ctx.failure(error)
        }
    }
}


nonisolated(unsafe) private var updatesTask: Task<Void, Never>?
private let updatesLock = NSLock()

@_cdecl("shiny_storekit_start_updates")
public func shiny_storekit_start_updates(_ callback: ShinyStoreKitUpdateCallback) {
    updatesLock.lock()
    defer { updatesLock.unlock() }
    if updatesTask != nil {
        return
    }

    updatesTask = Task.detached(priority: .background) {
        for await verification in Transaction.updates {
            guard case .verified(let transaction) = verification else {
                continue
            }
            let unfinished = await unfinishedTransactionIds()
            let payload = await serializeTransaction(
                transaction,
                jws: verification.jwsRepresentation,
                isFinished: !unfinished.contains(transaction.id)
            )
            if
                let data = try? JSONSerialization.data(withJSONObject: payload),
                let json = String(data: data, encoding: .utf8)
            {
                json.withCString { callback($0) }
            }
        }
    }
}


// MARK: - Serialization

func verified<T>(_ result: VerificationResult<T>) throws -> T {
    switch result {
    case .verified(let value):
        return value
    case .unverified(_, let error):
        throw error
    }
}


@MainActor
func activeWindowScene() -> UIWindowScene? {
    let scenes = UIApplication.shared.connectedScenes.compactMap { $0 as? UIWindowScene }
    return scenes.first { $0.activationState == .foregroundActive } ?? scenes.first
}


func unfinishedTransactionIds() async -> Set<UInt64> {
    var ids = Set<UInt64>()
    for await verification in Transaction.unfinished {
        ids.insert(verification.unsafePayloadValue.id)
    }
    return ids
}


func productTypeString(_ type: Product.ProductType) -> String {
    switch type {
    case .consumable: return "consumable"
    case .nonConsumable: return "nonConsumable"
    case .nonRenewable: return "nonRenewable"
    case .autoRenewable: return "autoRenewable"
    default: return type.rawValue
    }
}


func iso8601(_ period: Product.SubscriptionPeriod) -> String {
    switch period.unit {
    case .day: return "P\(period.value)D"
    case .week: return "P\(period.value)W"
    case .month: return "P\(period.value)M"
    case .year: return "P\(period.value)Y"
    @unknown default: return "P\(period.value)D"
    }
}


func millis(_ date: Date) -> Double {
    (date.timeIntervalSince1970 * 1000).rounded()
}


func serializeProduct(_ product: Product) async -> [String: Any] {
    var dict: [String: Any] = [
        "id": product.id,
        "displayName": product.displayName,
        "description": product.description,
        "displayPrice": product.displayPrice,
        // Decimal.description is locale-invariant - sent as a string so no precision is lost in JSON doubles
        "price": product.price.description,
        "currencyCode": product.priceFormatStyle.currencyCode,
        "type": productTypeString(product.type)
    ]

    if let subscription = product.subscription {
        dict["subscriptionGroupId"] = subscription.subscriptionGroupID
        dict["period"] = iso8601(subscription.subscriptionPeriod)

        var offers: [[String: Any]] = []
        if let intro = subscription.introductoryOffer, await subscription.isEligibleForIntroOffer {
            offers.append(serializeOffer(intro, type: "introductory"))
        }
        for promo in subscription.promotionalOffers {
            offers.append(serializeOffer(promo, type: "promotional"))
        }
        dict["offers"] = offers
    }
    return dict
}


func serializeOffer(_ offer: Product.SubscriptionOffer, type: String) -> [String: Any] {
    var dict: [String: Any] = [
        "type": type,
        "displayPrice": offer.displayPrice,
        "price": offer.price.description,
        "period": iso8601(offer.period),
        "periodCount": offer.periodCount
    ]
    if let id = offer.id {
        dict["id"] = id
    }
    switch offer.paymentMode {
    case .freeTrial: dict["paymentMode"] = "freeTrial"
    case .payAsYouGo: dict["paymentMode"] = "payAsYouGo"
    case .payUpFront: dict["paymentMode"] = "payUpFront"
    default: dict["paymentMode"] = offer.paymentMode.rawValue
    }
    return dict
}


func serializeTransaction(_ transaction: Transaction, jws: String, isFinished: Bool) async -> [String: Any] {
    var dict: [String: Any] = [
        "id": String(transaction.id),
        "originalId": String(transaction.originalID),
        "productId": transaction.productID,
        "productType": productTypeString(transaction.productType),
        "purchaseDate": millis(transaction.purchaseDate),
        "quantity": transaction.purchasedQuantity,
        "isUpgraded": transaction.isUpgraded,
        "isFinished": isFinished,
        "jws": jws,
        "json": String(decoding: transaction.jsonRepresentation, as: UTF8.self),
        "bundleId": transaction.appBundleID
    ]

    if let expiration = transaction.expirationDate {
        dict["expirationDate"] = millis(expiration)
    }
    if let revocation = transaction.revocationDate {
        dict["revocationDate"] = millis(revocation)
    }
    if let token = transaction.appAccountToken {
        dict["appAccountToken"] = token.uuidString
    }

    if #available(iOS 16.0, *) {
        dict["environment"] = transaction.environment.rawValue
    } else {
        dict["environment"] = transaction.environmentStringRepresentation
    }

    dict["isAutoRenewing"] = await isAutoRenewing(transaction)
    return dict
}


func isAutoRenewing(_ transaction: Transaction) async -> Bool {
    guard transaction.productType == .autoRenewable, transaction.revocationDate == nil else {
        return false
    }
    if #available(iOS 17.2, *) {
        if
            let status = await transaction.subscriptionStatus,
            case .verified(let renewal) = status.renewalInfo
        {
            return renewal.willAutoRenew
        }
    }
    guard let expiration = transaction.expirationDate else {
        return false
    }
    return expiration > Date()
}


// MARK: - Errors (codes match Shiny.Mobile.InAppPurchases.InAppPurchaseErrorCode names, plus "Cancelled")

func mapError(_ error: Error) -> (code: String, message: String, native: String?) {
    if let bridge = error as? BridgeError {
        return (bridge.code, bridge.message, nil)
    }

    if let storeKit = error as? StoreKitError {
        let native = "StoreKitError.\(storeKit)"
        switch storeKit {
        case .userCancelled:
            return ("Cancelled", "The user cancelled", native)
        case .networkError(let urlError):
            return ("Network", urlError.localizedDescription, native)
        case .notAvailableInStorefront:
            return ("ProductUnavailable", "The product is not available in the current storefront", native)
        case .notEntitled:
            return ("NotAllowed", "The app is not entitled to perform this operation", native)
        case .systemError(let inner):
            return ("Unknown", inner.localizedDescription, native)
        default:
            return ("Unknown", storeKit.localizedDescription, native)
        }
    }

    if let purchase = error as? Product.PurchaseError {
        let native = "Product.PurchaseError.\(purchase)"
        switch purchase {
        case .productUnavailable:
            return ("ProductUnavailable", "The product is not available for purchase", native)
        case .purchaseNotAllowed:
            return ("NotAllowed", "This user/device is not allowed to make purchases", native)
        case .invalidQuantity, .ineligibleForOffer, .invalidOfferIdentifier, .invalidOfferPrice, .invalidOfferSignature, .missingOfferParameters:
            return ("DeveloperError", purchase.localizedDescription, native)
        default:
            return ("Unknown", purchase.localizedDescription, native)
        }
    }

    if let verification = error as? VerificationResult<Transaction>.VerificationError {
        return ("VerificationFailed", verification.localizedDescription, "VerificationError.\(verification)")
    }

    let ns = error as NSError
    return ("Unknown", ns.localizedDescription, "\(ns.domain).\(ns.code)")
}
