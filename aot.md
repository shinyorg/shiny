# AOT Compliance Status

This document tracks known AOT (Ahead-of-Time compilation) and trimming issues across the Shiny codebase.

## Completed

### Unified ISerializer with JsonSerializerContext
All serialization now flows through `ISerializer` / `DefaultSerializer` backed by a single `TypeInfoResolverChain`. Both `IRepository` and `IKeyValueStore` paths share the same resolver. Unregistered types throw `InvalidOperationException` at runtime with a clear message.

Modules register their contexts via `services.AddJsonContext(MyContext.Default)`:

- `ShinyJobsJsonContext` — `JobInfo`
- `ShinyHttpJsonContext` — `HttpTransfer`
- `ShinyLocationsJsonContext` — `GeofenceRegion`
- `ShinyNotificationsJsonContext` — platform-conditional (`AndroidNotification`/`AndroidChannel`, `AppleNotification`/`AppleChannel`, `Notification`/`Channel`)

`RepositoryJsonOptions` and `AddRepositoryContext` have been removed. `FileSystemRepository` and `LocalStorageRepository` now take `ISerializer`.

---

## Critical — Will Break AOT

### 1. TypeJsonConverter uses Type.GetType()
**File:** `src/Shiny.Core/Stores/Impl/TypeJsonConverter.cs:13`
```csharp
return Type.GetType(typeName!);
```
Dynamic type loading by assembly-qualified name. The trimmer cannot statically determine which types to preserve. This converter is registered in `DefaultSerializer`, so it affects every serialization path. Specifically impacts `JobInfo.JobType` and `AndroidNotification.LaunchActivityType`.

**Options:**
- Change `JobInfo.JobType` from `Type` to `string` and resolve types via an explicit registry at startup
- Use `[DynamicDependency]` annotations for all known job types (brittle)

### 2. Windows SettingsKeyValueStore uses untyped JsonSerializer
**File:** `src/Shiny.Core/Platforms/Windows/SettingsKeyValueStore.cs:146,163`
```csharp
this.fileValues = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? ...;
File.WriteAllText(this.filePath, JsonSerializer.Serialize(this.fileValues));
```
Generic `Deserialize<T>()` and `Serialize()` without a `JsonTypeInfo<T>` parameter. Only affects Windows unpackaged apps. Fix by passing `JsonSerializerOptions` with a source-generated context for `Dictionary<string, string>`.

### 3. Consumer types need JsonSerializerContext registration
Any user-defined type stored via `IKeyValueStore` or `IRepository` must have a registered `JsonSerializerContext`. Without one, `DefaultSerializer.GetRequiredTypeInfo()` will throw. This is the correct AOT contract, but consumers need documentation and clear guidance on calling `services.AddJsonContext(...)` for their own types.

---

## High — May Break AOT

### 4. RemoteConfigurationProvider uses untyped Serialize
**File:** `src/Shiny.Extensions.Configuration/Infrastructure/RemoteConfigurationProvider.cs:86`
```csharp
var json = JsonSerializer.Serialize(obj);
```
Serializing an arbitrary `object` without `JsonTypeInfo`. Triggered when a custom `getData` delegate returns an object.

### 5. TransferHttpContent.FromJson serializes arbitrary objects
**File:** `src/Shiny.Net.Http/Models.cs:137`
```csharp
var json = JsonSerializer.Serialize(obj, jsonOptions);
```
Public API that accepts `object` — callers should use strongly-typed overloads or pass `JsonSerializerOptions` with a registered context.

---

## Medium — Reflection Without Trimmer Annotations

### 6. Reflection extensions in Shiny.Core
**File:** `src/Shiny.Core/Reflection/Extensions.cs`
- **Line 20:** `obj.GetType().GetProperty(propertyName)` — runtime property lookup by string
- **Line 47:** `sender.GetType().GetRuntimeProperty(member.Member.Name)` — runtime property from expression tree
- **Line 60:** `Activator.CreateInstance(t)` — creating value type defaults

None of these have `[DynamicallyAccessedMembers]` annotations.

### 7. ObjectStoreBinder property/attribute reflection
**File:** `src/Shiny.Core/Stores/Impl/ObjectStoreBinder.cs`
- **Line 39:** `npc.GetType().GetCustomAttribute<ObjectStoreBinderAttribute>()` — attribute lookup
- **Line 127:** `type.GetProperties()` — enumerating all properties for store binding

These need `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]` annotations.

### 8. AddShinyService uses GetInterfaces and ActivatorUtilities
**File:** `src/Shiny.Core/ServiceProviderExtensions.cs`
- **Line 80:** `implementationType.GetInterfaces()` — auto-discovers interfaces to register
- **Line 91:** `ActivatorUtilities.CreateInstance(services, implementationType)` — reflection-based constructor resolution

### 9. Job execution creates instances via reflection
**File:** `src/Shiny.Jobs/AbstractJobManager.cs:196`
```csharp
jobDelegate = (IJob)ActivatorUtilities.GetServiceOrCreateInstance(this.container, job.JobType);
```
Even with correct serialization of `JobInfo`, the runtime job execution depends on `ActivatorUtilities` resolving `job.JobType` — which is a `Type` loaded from JSON via `TypeJsonConverter`.

### 10. BLE Hosting uses reflection for GATT discovery
**File:** `src/Shiny.BluetoothLE.Hosting/Platforms/Shared/BleHostingManager.cs`
- **Line 130:** `type.GetMethod(methodName, flags)` — checking if virtual methods are overridden
- **Line 249:** `type.GetCustomAttribute(typeof(BleGattCharacteristicAttribute))` — attribute discovery

### 11. Android BLE GATT refresh workaround
**File:** `src/Shiny.BluetoothLE/Platforms/Android/Peripheral_Services.cs:79,86`
```csharp
var method = this.Gatt!.Class.GetMethod("refresh");
var result = (bool)method.Invoke(this.Gatt);
```
Java reflection to call internal Android method. Has try/catch fallback — low risk, graceful degradation.

---

## StoreExtensions value comparison
**File:** `src/Shiny.Core/Stores/StoreExtensions.cs:25`
```csharp
var result = Activator.CreateInstance(type).Equals(obj);
```
Creates value type instances for default comparison in `IsNullOrDefault()`.

---

## Recommended Priority

1. **TypeJsonConverter / JobInfo.JobType** — change `Type` to `string` to eliminate `Type.GetType()`. Resolve job types via an explicit DI-based registry instead of runtime type loading.
2. **Add `[DynamicallyAccessedMembers]` annotations** to reflection extension methods and `ObjectStoreBinder`.
3. **Windows SettingsKeyValueStore** — use source-generated `JsonTypeInfo<Dictionary<string, string>>` for the file-based fallback path.
4. **TransferHttpContent.FromJson** — add a generic overload `FromJson<T>(T obj, JsonTypeInfo<T> typeInfo)`.
5. **Consumer documentation** — document the `AddJsonContext(...)` requirement for user-defined types stored in repositories or key-value stores.
