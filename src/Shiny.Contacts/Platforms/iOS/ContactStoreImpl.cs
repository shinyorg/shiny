using Contacts;
using Foundation;
using Shiny.Contacts.Internals;

namespace Shiny.Contacts;

public class ContactStoreImpl : IContactStore
{
    public AccessState GetCurrentAccess()
    {
        var status = CNContactStore.GetAuthorizationStatus(CNEntityType.Contacts);
        if (OperatingSystem.IsIOSVersionAtLeast(18) && status == CNAuthorizationStatus.Limited)
            return AccessState.Restricted;

        return FromNative(status);
    }

    public Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<AccessState>();
        new CNContactStore().RequestAccess(CNEntityType.Contacts, (granted, error) =>
        {
            if (error != null)
                tcs.TrySetResult(AccessState.Denied);
            else
                tcs.TrySetResult(granted ? AccessState.Available : AccessState.Denied);
        });
        return tcs.Task;
    }

    static AccessState FromNative(CNAuthorizationStatus status) => status switch
    {
        CNAuthorizationStatus.Authorized => AccessState.Available,
        CNAuthorizationStatus.Restricted => AccessState.Restricted,
        CNAuthorizationStatus.Denied => AccessState.Denied,
        _ => AccessState.Unknown
    };

    // NOTE: CNContactKey.ImageData (the full-resolution photo) is intentionally NOT in the base
    // keys. Apple warns that imageData is large; requesting it during a bulk EnumerateContacts
    // loads every contact's full photo into memory at once, which on a real device with many
    // photo contacts spikes memory and gets the app jetsam-killed (an uncatchable native
    // termination). Bulk fetches use the lightweight ThumbnailImageData; the full photo is only
    // pulled for single-contact operations (GetById/Update/Delete) via GetFetchKeys(includeFullPhoto: true).
    static readonly NSString[] BaseFetchKeys =
    [
        CNContactKey.Identifier,
        CNContactKey.NamePrefix,
        CNContactKey.GivenName,
        CNContactKey.MiddleName,
        CNContactKey.FamilyName,
        CNContactKey.NameSuffix,
        CNContactKey.Nickname,
        CNContactKey.EmailAddresses,
        CNContactKey.PhoneNumbers,
        CNContactKey.PostalAddresses,
        CNContactKey.OrganizationName,
        CNContactKey.JobTitle,
        CNContactKey.DepartmentName,
        CNContactKey.Birthday,
        CNContactKey.Dates,
        CNContactKey.UrlAddresses,
        CNContactKey.ThumbnailImageData,
        CNContactKey.Type
    ];

    static bool? hasNotesEntitlement;

    static bool HasNotesEntitlement
    {
        get
        {
            if (hasNotesEntitlement == null)
            {
                try
                {
                    var store = new CNContactStore();
                    var keys = new NSString[] { CNContactKey.Identifier, CNContactKey.Note };
                    var request = new CNContactFetchRequest(keys);
                    request.Predicate = CNContact.GetPredicateForContacts(["__entitlement_check__"]);
                    store.EnumerateContacts(request, out var error, (_, ref stop) => { stop = true; });
                    hasNotesEntitlement = error == null;
                }
                catch
                {
                    hasNotesEntitlement = false;
                }
            }
            return hasNotesEntitlement.Value;
        }
    }

    static NSString[] GetFetchKeys(bool includeFullPhoto)
    {
        var keys = new List<NSString>(BaseFetchKeys);
        if (includeFullPhoto)
            keys.Add(CNContactKey.ImageData);

        if (HasNotesEntitlement)
        {
            keys.Add(CNContactKey.Relations);
            keys.Add(CNContactKey.Note);
        }
        return keys.ToArray();
    }

    static List<CNContact> FetchContacts(CNContactStore store, NSPredicate? predicate = null, bool includeFullPhoto = false)
    {
        var request = new CNContactFetchRequest(GetFetchKeys(includeFullPhoto));
        if (predicate != null)
            request.Predicate = predicate;

        var results = new List<CNContact>();
        store.EnumerateContacts(request, out var error, (contact, ref stop) =>
        {
            results.Add(contact);
        });

        if (error != null)
            throw new InvalidOperationException($"Failed to fetch contacts: {error.LocalizedDescription}");

        return results;
    }

    public Task<IReadOnlyList<Contact>> GetAll(CancellationToken ct = default) => Task.Run(() =>
    {
        var store = new CNContactStore();
        var contacts = FetchContacts(store);

        var results = new List<Contact>();
        foreach (var cn in contacts)
        {
            ct.ThrowIfCancellationRequested();
            results.Add(ToContact(cn));
        }

        return (IReadOnlyList<Contact>)results;
    }, ct);

    public Task<Contact?> GetById(string contactId, CancellationToken ct = default)
    {
        var store = new CNContactStore();
        var predicate = CNContact.GetPredicateForContacts([contactId]);
        var contacts = FetchContacts(store, predicate, includeFullPhoto: true);

        var cn = contacts.FirstOrDefault();
        var result = cn == null ? null : ToContact(cn);
        return Task.FromResult(result);
    }

    public ContactQuery Query()
        => new((descriptor, ct) => Task.Run(() => ExecuteQuery(descriptor), ct));

    IEnumerable<Contact> ExecuteQuery(ContactQueryDescriptor descriptor)
    {
        var store = new CNContactStore();

        // CNContact only offers a name-matching predicate, and it matches on name-token PREFIX. So it
        // can only be pushed down when the result is guaranteed to be a superset of the filter:
        //  - Match.All only, otherwise a phone/email-only match would be dropped before the builder's
        //    in-memory pass ever sees it.
        //  - StartsWith/Equals only, since a prefix match is not a superset of Contains/EndsWith.
        var nameFilter = descriptor.Match == ContactFilterMatch.All
            ? descriptor.Filters.FirstOrDefault(f =>
                (f.Field is ContactField.GivenName or ContactField.FamilyName or ContactField.DisplayName) &&
                (f.Operation is ContactFilterOperation.StartsWith or ContactFilterOperation.Equals))
            : null;

        NSPredicate? predicate = null;
        if (nameFilter != null)
            predicate = CNContact.GetPredicateForContacts(nameFilter.Value);

        return FetchContacts(store, predicate).Select(ToContact);
    }

    public Task<string> Create(Contact contact, CancellationToken ct = default)
    {
        var store = new CNContactStore();
        var cnContact = new CNMutableContact();
        PopulateCNContact(cnContact, contact);

        var saveRequest = new CNSaveRequest();
        saveRequest.AddContact(cnContact, null);

        if (!store.ExecuteSaveRequest(saveRequest, out var error))
            throw new InvalidOperationException($"Failed to create contact: {error?.LocalizedDescription}");

        return Task.FromResult(cnContact.Identifier);
    }

    public Task Update(Contact contact, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contact.Id))
            throw new ArgumentException("Contact Id is required for update.", nameof(contact));

        var store = new CNContactStore();
        var predicate = CNContact.GetPredicateForContacts([contact.Id]);
        var contacts = FetchContacts(store, predicate, includeFullPhoto: true);

        var existing = contacts.FirstOrDefault()
            ?? throw new InvalidOperationException($"Contact with Id '{contact.Id}' not found.");

        var mutable = existing.MutableCopy() as CNMutableContact
            ?? throw new InvalidOperationException("Failed to create mutable copy of contact.");

        PopulateCNContact(mutable, contact);

        var saveRequest = new CNSaveRequest();
        saveRequest.UpdateContact(mutable);

        if (!store.ExecuteSaveRequest(saveRequest, out var error))
            throw new InvalidOperationException($"Failed to update contact: {error?.LocalizedDescription}");

        return Task.CompletedTask;
    }

    public Task Delete(string contactId, CancellationToken ct = default)
    {
        var store = new CNContactStore();
        var predicate = CNContact.GetPredicateForContacts([contactId]);
        var contacts = FetchContacts(store, predicate);

        var existing = contacts.FirstOrDefault()
            ?? throw new InvalidOperationException($"Contact with Id '{contactId}' not found.");

        var mutable = existing.MutableCopy() as CNMutableContact
            ?? throw new InvalidOperationException("Failed to create mutable copy of contact.");

        var saveRequest = new CNSaveRequest();
        saveRequest.DeleteContact(mutable);

        if (!store.ExecuteSaveRequest(saveRequest, out var error))
            throw new InvalidOperationException($"Failed to delete contact: {error?.LocalizedDescription}");

        return Task.CompletedTask;
    }

    // ── Mapping: CNContact → Contact ──────────────────────────────────

    static Contact ToContact(CNContact cn)
    {
        var contact = new Contact
        {
            Id = cn.Identifier,
            NamePrefix = cn.NamePrefix,
            GivenName = cn.GivenName,
            MiddleName = cn.MiddleName,
            FamilyName = cn.FamilyName,
            NameSuffix = cn.NameSuffix,
            Nickname = cn.Nickname,
            Note = HasNotesEntitlement ? cn.Note : null,
            Organization = new ContactOrganization
            {
                Company = cn.OrganizationName,
                Title = cn.JobTitle,
                Department = cn.DepartmentName
            },
            // ImageData is only fetched for single-contact operations (see BaseFetchKeys note);
            // accessing an unfetched key raises a native CNContactPropertyNotFetchedException, so
            // guard on IsKeyAvailable rather than assuming the full photo was requested.
            Photo = cn.IsKeyAvailable(CNContactKey.ImageData) ? cn.ImageData?.ToArray() : null,
            Thumbnail = cn.ThumbnailImageData?.ToArray()
        };

        if (cn.PhoneNumbers != null)
        {
            foreach (var pv in cn.PhoneNumbers)
            {
                var (type, label) = ToPhoneType(pv.Label);
                contact.Phones.Add(new ContactPhone(pv.Value.StringValue, type, label));
            }
        }

        if (cn.EmailAddresses != null)
        {
            foreach (var ev in cn.EmailAddresses)
            {
                var (type, label) = ToEmailType(ev.Label);
                contact.Emails.Add(new ContactEmail(ev.Value.ToString(), type, label));
            }
        }

        if (cn.PostalAddresses != null)
        {
            foreach (var av in cn.PostalAddresses)
            {
                var (type, label) = ToAddressType(av.Label);
                var addr = av.Value;
                contact.Addresses.Add(new ContactAddress(
                    addr.Street, addr.City, addr.State,
                    addr.PostalCode, addr.Country, type, label
                ));
            }
        }

        // Birthday is a special property on CNContact
        if (cn.Birthday != null)
        {
            var bd = ToDateOnly(cn.Birthday);
            if (bd.HasValue)
                contact.Dates.Add(new ContactDate(bd.Value, ContactDateType.Birthday));
        }

        if (cn.Dates != null)
        {
            foreach (var dv in cn.Dates)
            {
                var dateOnly = ToDateOnly(dv.Value);
                if (dateOnly.HasValue)
                {
                    var (type, label) = ToContactDateType(dv.Label);
                    contact.Dates.Add(new ContactDate(dateOnly.Value, type, label));
                }
            }
        }

        if (HasNotesEntitlement && cn.ContactRelations != null)
        {
            foreach (var rv in cn.ContactRelations)
            {
                var (type, label) = ToRelationshipType(rv.Label);
                contact.Relationships.Add(new ContactRelationship(rv.Value.Name, type, label));
            }
        }

        if (cn.UrlAddresses != null)
        {
            foreach (var wv in cn.UrlAddresses)
            {
                contact.Websites.Add(new ContactWebsite(wv.Value.ToString(), wv.Label));
            }
        }

        return contact;
    }

    // ── Mapping: Contact → CNMutableContact ──────────────────────────

    static void PopulateCNContact(CNMutableContact cn, Contact contact)
    {
        cn.NamePrefix = contact.NamePrefix ?? string.Empty;
        cn.GivenName = contact.GivenName ?? string.Empty;
        cn.MiddleName = contact.MiddleName ?? string.Empty;
        cn.FamilyName = contact.FamilyName ?? string.Empty;
        cn.NameSuffix = contact.NameSuffix ?? string.Empty;
        cn.Nickname = contact.Nickname ?? string.Empty;
        if (HasNotesEntitlement)
            cn.Note = contact.Note ?? string.Empty;

        cn.OrganizationName = contact.Organization?.Company ?? string.Empty;
        cn.JobTitle = contact.Organization?.Title ?? string.Empty;
        cn.DepartmentName = contact.Organization?.Department ?? string.Empty;

        cn.PhoneNumbers = contact.Phones
            .Select(p => new CNLabeledValue<CNPhoneNumber>(
                FromPhoneType(p.Type, p.Label),
                new CNPhoneNumber(p.Number)))
            .ToArray();

        cn.EmailAddresses = contact.Emails
            .Select(e => new CNLabeledValue<NSString>(
                FromEmailType(e.Type, e.Label),
                new NSString(e.Address)))
            .ToArray();

        cn.PostalAddresses = contact.Addresses
            .Select(a =>
            {
                var addr = new CNMutablePostalAddress
                {
                    Street = a.Street ?? string.Empty,
                    City = a.City ?? string.Empty,
                    State = a.State ?? string.Empty,
                    PostalCode = a.PostalCode ?? string.Empty,
                    Country = a.Country ?? string.Empty
                };
                return new CNLabeledValue<CNPostalAddress>(
                    FromAddressType(a.Type, a.Label), addr);
            })
            .ToArray();

        // Birthday is set separately on CNContact
        var birthday = contact.Dates.FirstOrDefault(d => d.Type == ContactDateType.Birthday);
        cn.Birthday = birthday != null ? ToNSDateComponents(birthday.Date) : null;

        var otherDates = contact.Dates.Where(d => d.Type != ContactDateType.Birthday).ToList();
        cn.Dates = otherDates
            .Select(d => new CNLabeledValue<NSDateComponents>(
                FromContactDateType(d.Type, d.Label),
                ToNSDateComponents(d.Date)))
            .ToArray();

        if (HasNotesEntitlement)
        {
            cn.ContactRelations = contact.Relationships
                .Select(r => new CNLabeledValue<CNContactRelation>(
                    FromRelationshipType(r.Type, r.Label),
                    new CNContactRelation(r.Name)))
                .ToArray();
        }

        cn.UrlAddresses = contact.Websites
            .Select(w => new CNLabeledValue<NSString>(
                w.Label != null ? new NSString(w.Label) : CNLabelKey.Other,
                new NSString(w.Url)))
            .ToArray();

        if (contact.Photo != null)
            cn.ImageData = NSData.FromArray(contact.Photo);
    }

    // ── Phone type mapping ───────────────────────────────────────────

    static (PhoneType Type, string? Label) ToPhoneType(string? label)
    {
        if (label == null) return (PhoneType.Other, null);

        if (label == CNLabelPhoneNumberKey.Mobile) return (PhoneType.Mobile, null);
        if (label == CNLabelPhoneNumberKey.iPhone) return (PhoneType.Mobile, null);
        if (label == CNLabelPhoneNumberKey.Main) return (PhoneType.Work, null);
        if (label == CNLabelPhoneNumberKey.HomeFax) return (PhoneType.FaxHome, null);
        if (label == CNLabelPhoneNumberKey.WorkFax) return (PhoneType.FaxWork, null);
        if (label == CNLabelPhoneNumberKey.Pager) return (PhoneType.Pager, null);
        if (label == CNLabelKey.Home) return (PhoneType.Home, null);
        if (label == CNLabelKey.Work) return (PhoneType.Work, null);
        if (label == CNLabelKey.Other) return (PhoneType.Other, null);

        return (PhoneType.Custom, label);
    }

    static NSString FromPhoneType(PhoneType type, string? customLabel) => type switch
    {
        PhoneType.Home => CNLabelKey.Home,
        PhoneType.Mobile => CNLabelPhoneNumberKey.Mobile,
        PhoneType.Work => CNLabelKey.Work,
        PhoneType.FaxWork => CNLabelPhoneNumberKey.WorkFax,
        PhoneType.FaxHome => CNLabelPhoneNumberKey.HomeFax,
        PhoneType.Pager => CNLabelPhoneNumberKey.Pager,
        PhoneType.Other => CNLabelKey.Other,
        PhoneType.Custom => new NSString(customLabel ?? string.Empty),
        _ => CNLabelKey.Other
    };

    // ── Email type mapping ───────────────────────────────────────────

    static (EmailType Type, string? Label) ToEmailType(string? label)
    {
        if (label == null) return (EmailType.Other, null);

        if (label == CNLabelKey.Home) return (EmailType.Home, null);
        if (label == CNLabelKey.Work) return (EmailType.Work, null);
        if (label == CNLabelKey.Other) return (EmailType.Other, null);

        return (EmailType.Custom, label);
    }

    static NSString FromEmailType(EmailType type, string? customLabel) => type switch
    {
        EmailType.Home => CNLabelKey.Home,
        EmailType.Work => CNLabelKey.Work,
        EmailType.Other => CNLabelKey.Other,
        EmailType.Custom => new NSString(customLabel ?? string.Empty),
        _ => CNLabelKey.Other
    };

    // ── Address type mapping ─────────────────────────────────────────

    static (AddressType Type, string? Label) ToAddressType(string? label)
    {
        if (label == null) return (AddressType.Other, null);

        if (label == CNLabelKey.Home) return (AddressType.Home, null);
        if (label == CNLabelKey.Work) return (AddressType.Work, null);
        if (label == CNLabelKey.Other) return (AddressType.Other, null);

        return (AddressType.Custom, label);
    }

    static NSString FromAddressType(AddressType type, string? customLabel) => type switch
    {
        AddressType.Home => CNLabelKey.Home,
        AddressType.Work => CNLabelKey.Work,
        AddressType.Other => CNLabelKey.Other,
        AddressType.Custom => new NSString(customLabel ?? string.Empty),
        _ => CNLabelKey.Other
    };

    // ── Contact date type mapping ────────────────────────────────────

    static (ContactDateType Type, string? Label) ToContactDateType(string? label)
    {
        if (label == null) return (ContactDateType.Other, null);

        if (label == CNLabelKey.DateAnniversary) return (ContactDateType.Anniversary, null);
        if (label == CNLabelKey.Other) return (ContactDateType.Other, null);

        return (ContactDateType.Custom, label);
    }

    static NSString FromContactDateType(ContactDateType type, string? customLabel) => type switch
    {
        ContactDateType.Anniversary => CNLabelKey.DateAnniversary,
        ContactDateType.Other => CNLabelKey.Other,
        ContactDateType.Custom => new NSString(customLabel ?? string.Empty),
        _ => CNLabelKey.Other
    };

    // ── Relationship type mapping ────────────────────────────────────

    static (RelationshipType Type, string? Label) ToRelationshipType(string? label)
    {
        if (label == null) return (RelationshipType.Other, null);

        if (label == CNLabelContactRelationKey.Father) return (RelationshipType.Father, null);
        if (label == CNLabelContactRelationKey.Mother) return (RelationshipType.Mother, null);
        if (label == CNLabelContactRelationKey.Parent) return (RelationshipType.Parent, null);
        if (label == CNLabelContactRelationKey.Brother) return (RelationshipType.Brother, null);
        if (label == CNLabelContactRelationKey.Sister) return (RelationshipType.Sister, null);
        if (label == CNLabelContactRelationKey.Child) return (RelationshipType.Child, null);
        if (label == CNLabelContactRelationKey.Friend) return (RelationshipType.Friend, null);
        if (label == CNLabelContactRelationKey.Spouse) return (RelationshipType.Spouse, null);
        if (label == CNLabelContactRelationKey.Partner) return (RelationshipType.Partner, null);
        if (label == CNLabelContactRelationKey.Assistant) return (RelationshipType.Assistant, null);
        if (label == CNLabelContactRelationKey.Manager) return (RelationshipType.Manager, null);
        if (label == CNLabelKey.Other) return (RelationshipType.Other, null);

        return (RelationshipType.Custom, label);
    }

    static NSString FromRelationshipType(RelationshipType type, string? customLabel) => type switch
    {
        RelationshipType.Father => CNLabelContactRelationKey.Father,
        RelationshipType.Mother => CNLabelContactRelationKey.Mother,
        RelationshipType.Parent => CNLabelContactRelationKey.Parent,
        RelationshipType.Brother => CNLabelContactRelationKey.Brother,
        RelationshipType.Sister => CNLabelContactRelationKey.Sister,
        RelationshipType.Child => CNLabelContactRelationKey.Child,
        RelationshipType.Friend => CNLabelContactRelationKey.Friend,
        RelationshipType.Spouse => CNLabelContactRelationKey.Spouse,
        RelationshipType.Partner => CNLabelContactRelationKey.Partner,
        RelationshipType.Assistant => CNLabelContactRelationKey.Assistant,
        RelationshipType.Manager => CNLabelContactRelationKey.Manager,
        RelationshipType.Other => CNLabelKey.Other,
        RelationshipType.Custom => new NSString(customLabel ?? string.Empty),
        _ => CNLabelKey.Other
    };

    // ── Date helpers ─────────────────────────────────────────────────

    static DateOnly? ToDateOnly(NSDateComponents? components)
    {
        if (components == null) return null;

        var year = (int)components.Year;
        var month = (int)components.Month;
        var day = (int)components.Day;

        if (month < 1 || month > 12 || day < 1 || day > 31)
            return null;

        // Year may be unset (NSDateComponentUndefined) for birthday without year
        if (year == nint.MaxValue || year < 1 || year > 9999)
            year = 1;

        return new DateOnly(year, month, day);
    }

    static NSDateComponents ToNSDateComponents(DateOnly date) => new()
    {
        Year = date.Year,
        Month = date.Month,
        Day = date.Day
    };
}
