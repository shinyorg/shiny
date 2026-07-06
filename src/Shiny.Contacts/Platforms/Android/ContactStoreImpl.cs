using Android.Content;
using Android.Database;
using Android.Provider;
using Shiny.Contacts.Internals;
using Application = Android.App.Application;
using CommonColumns = Android.Provider.ContactsContract.CommonDataKinds;
using RawContactsColumns = Android.Provider.ContactsContract.RawContacts;
using DataColumns = Android.Provider.ContactsContract.Data;

namespace Shiny.Contacts;

public class ContactStoreImpl(AndroidPlatform platform) : IContactStore
{
    static ContentResolver Resolver => Application.Context.ContentResolver!;

    public AccessState GetCurrentAccess()
    {
        var read = Application.Context.CheckSelfPermission(Android.Manifest.Permission.ReadContacts) == Android.Content.PM.Permission.Granted;
        var write = Application.Context.CheckSelfPermission(Android.Manifest.Permission.WriteContacts) == Android.Content.PM.Permission.Granted;

        if (read && write)
            return AccessState.Available;

        return read || write ? AccessState.Restricted : AccessState.Denied;
    }

    public async Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        var result = await platform
            .RequestPermissions(ct, Android.Manifest.Permission.ReadContacts, Android.Manifest.Permission.WriteContacts)
            .ConfigureAwait(false);

        var read = result.IsGranted(Android.Manifest.Permission.ReadContacts);
        var write = result.IsGranted(Android.Manifest.Permission.WriteContacts);

        if (read && write)
            return AccessState.Available;

        return read || write ? AccessState.Restricted : AccessState.Denied;
    }

    public Task<IReadOnlyList<Contact>> GetAll(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var contacts = ReadContacts(null, null);
            return (IReadOnlyList<Contact>)contacts;
        }, ct);
    }

    public Task<Contact?> GetById(string contactId, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var contacts = ReadContacts(
                ContactsContract.Contacts.InterfaceConsts.Id + " = ?",
                new[] { contactId }
            );
            return contacts.Count > 0 ? contacts[0] : null;
        }, ct);
    }

    public IQueryable<Contact> Query()
    {
        return new ContactQueryable(new ContactQueryProvider(ExecuteQuery));
    }

    public Task<string> Create(Contact contact, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var ops = new List<ContentProviderOperation>();
            var backRef = 0;

            // Insert raw contact
            ops.Add(ContentProviderOperation
                .NewInsert(RawContactsColumns.ContentUri)!
                .WithValue(RawContactsColumns.InterfaceConsts.AccountType, null)
                .WithValue(RawContactsColumns.InterfaceConsts.AccountName, null)
                .Build()!);

            // StructuredName
            AddStructuredNameInsert(ops, contact, backRef);

            // Nickname
            if (!string.IsNullOrWhiteSpace(contact.Nickname))
            {
                ops.Add(ContentProviderOperation
                    .NewInsert(DataColumns.ContentUri)!
                    .WithValueBackReference(DataColumns.InterfaceConsts.RawContactId, backRef)
                    .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Nickname.ContentItemType)
                    .WithValue(CommonColumns.Nickname.Name, contact.Nickname)
                    .Build()!);
            }

            // Phones
            foreach (var phone in contact.Phones)
                AddPhoneInsert(ops, phone, backRef);

            // Emails
            foreach (var email in contact.Emails)
                AddEmailInsert(ops, email, backRef);

            // Addresses
            foreach (var address in contact.Addresses)
                AddAddressInsert(ops, address, backRef);

            // Organization
            if (contact.Organization != null)
                AddOrganizationInsert(ops, contact.Organization, backRef);

            // Note
            if (!string.IsNullOrWhiteSpace(contact.Note))
                AddNoteInsert(ops, contact.Note, backRef);

            // Dates
            foreach (var date in contact.Dates)
                AddDateInsert(ops, date, backRef);

            // Relationships
            foreach (var rel in contact.Relationships)
                AddRelationshipInsert(ops, rel, backRef);

            // Websites
            foreach (var website in contact.Websites)
                AddWebsiteInsert(ops, website, backRef);

            // Photo
            if (contact.Photo != null && contact.Photo.Length > 0)
                AddPhotoInsert(ops, contact.Photo, backRef);

            var results = Resolver.ApplyBatch(ContactsContract.Authority, ops)!;
            var rawContactUri = results[0]!.Uri!;
            var rawContactId = ContentUris.ParseId(rawContactUri);

            // Resolve the aggregate contact ID from the raw contact
            using var cursor = Resolver.Query(
                RawContactsColumns.ContentUri,
                new[] { RawContactsColumns.InterfaceConsts.ContactId },
                RawContactsColumns.InterfaceConsts.Id + " = ?",
                new[] { rawContactId.ToString() },
                null
            );

            if (cursor != null && cursor.MoveToFirst())
            {
                var contactId = cursor.GetLong(0);
                return contactId.ToString();
            }

            return rawContactId.ToString();
        }, ct);
    }

    public Task Update(Contact contact, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrWhiteSpace(contact.Id))
                throw new ArgumentException("Contact.Id is required for update");

            var rawContactId = GetRawContactId(contact.Id);
            if (rawContactId == null)
                throw new InvalidOperationException($"No raw contact found for contact ID {contact.Id}");

            var ops = new List<ContentProviderOperation>();

            // Delete all existing data rows for this raw contact
            ops.Add(ContentProviderOperation
                .NewDelete(DataColumns.ContentUri)!
                .WithSelection(
                    DataColumns.InterfaceConsts.RawContactId + " = ?",
                    new[] { rawContactId.ToString() })
                .Build()!);

            // Re-insert all data rows
            AddStructuredNameInsertDirect(ops, contact, rawContactId.Value);

            if (!string.IsNullOrWhiteSpace(contact.Nickname))
            {
                ops.Add(ContentProviderOperation
                    .NewInsert(DataColumns.ContentUri)!
                    .WithValue(DataColumns.InterfaceConsts.RawContactId, rawContactId.Value)
                    .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Nickname.ContentItemType)
                    .WithValue(CommonColumns.Nickname.Name, contact.Nickname)
                    .Build()!);
            }

            foreach (var phone in contact.Phones)
                AddPhoneInsertDirect(ops, phone, rawContactId.Value);

            foreach (var email in contact.Emails)
                AddEmailInsertDirect(ops, email, rawContactId.Value);

            foreach (var address in contact.Addresses)
                AddAddressInsertDirect(ops, address, rawContactId.Value);

            if (contact.Organization != null)
                AddOrganizationInsertDirect(ops, contact.Organization, rawContactId.Value);

            if (!string.IsNullOrWhiteSpace(contact.Note))
                AddNoteInsertDirect(ops, contact.Note, rawContactId.Value);

            foreach (var date in contact.Dates)
                AddDateInsertDirect(ops, date, rawContactId.Value);

            foreach (var rel in contact.Relationships)
                AddRelationshipInsertDirect(ops, rel, rawContactId.Value);

            foreach (var website in contact.Websites)
                AddWebsiteInsertDirect(ops, website, rawContactId.Value);

            if (contact.Photo != null && contact.Photo.Length > 0)
                AddPhotoInsertDirect(ops, contact.Photo, rawContactId.Value);

            Resolver.ApplyBatch(ContactsContract.Authority, ops);
        }, ct);
    }

    public Task Delete(string contactId, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var uri = ContentUris.WithAppendedId(ContactsContract.Contacts.ContentUri, long.Parse(contactId));
            Resolver.Delete(uri!, null, null);
        }, ct);
    }

    // ── Query execution ──────────────────────────────────────────────

    IEnumerable<Contact> ExecuteQuery(ContactQueryDescriptor descriptor)
    {
        if (descriptor.Filters.Count == 0)
            return ReadContacts(null, null);

        HashSet<string>? contactIds = null;

        foreach (var filter in descriptor.Filters)
        {
            HashSet<string>? ids = null;

            switch (filter.PropertyName)
            {
                case "GivenName":
                case "FamilyName":
                case "MiddleName":
                case "NamePrefix":
                case "NameSuffix":
                case "DisplayName":
                case "Nickname":
                    ids = QueryByName(filter);
                    break;

                case "Phones":
                    ids = QueryByPhone(filter);
                    break;

                case "Emails":
                    ids = QueryByEmail(filter);
                    break;

                default:
                    // Unsupported filter — load all and let in-memory predicate handle
                    return ReadContacts(null, null);
            }

            if (ids != null)
                contactIds = contactIds == null ? ids : new HashSet<string>(contactIds.Intersect(ids));
        }

        if (contactIds == null || contactIds.Count == 0)
            return Array.Empty<Contact>();

        var idList = string.Join(",", contactIds);
        return ReadContacts(
            ContactsContract.Contacts.InterfaceConsts.Id + " IN (" + string.Join(",", contactIds.Select(_ => "?")) + ")",
            contactIds.ToArray()
        );
    }

    HashSet<string> QueryByName(ContactQueryFilter filter)
    {
        var (op, val) = GetLikeArgs(filter.Operation, filter.Value);
        string column;

        switch (filter.PropertyName)
        {
            case "GivenName":
                column = CommonColumns.StructuredName.GivenName;
                break;
            case "FamilyName":
                column = CommonColumns.StructuredName.FamilyName;
                break;
            case "MiddleName":
                column = CommonColumns.StructuredName.MiddleName;
                break;
            case "NamePrefix":
                column = CommonColumns.StructuredName.Prefix;
                break;
            case "NameSuffix":
                column = CommonColumns.StructuredName.Suffix;
                break;
            case "DisplayName":
                column = CommonColumns.StructuredName.DisplayName;
                break;
            case "Nickname":
                return QueryDataTable(
                    CommonColumns.Nickname.ContentItemType,
                    CommonColumns.Nickname.Name,
                    op,
                    val
                );
            default:
                return new HashSet<string>();
        }

        return QueryDataTable(
            CommonColumns.StructuredName.ContentItemType,
            column,
            op,
            val
        );
    }

    HashSet<string> QueryDataTable(string mimeType, string column, string op, string value)
    {
        var ids = new HashSet<string>();
        var selection = DataColumns.InterfaceConsts.Mimetype + " = ? AND " + column + " " + op + " ?";
        var selectionArgs = new[] { mimeType, value };

        using var cursor = Resolver.Query(
            DataColumns.ContentUri,
            new[] { DataColumns.InterfaceConsts.ContactId },
            selection,
            selectionArgs,
            null
        );

        if (cursor != null)
        {
            while (cursor.MoveToNext())
            {
                var id = cursor.GetString(0);
                if (id != null)
                    ids.Add(id);
            }
        }

        return ids;
    }

    HashSet<string> QueryByPhone(ContactQueryFilter filter)
    {
        var (op, val) = GetLikeArgs(filter.Operation, filter.Value);
        return QueryDataTable(
            CommonColumns.Phone.ContentItemType,
            CommonColumns.Phone.Number,
            op,
            val
        );
    }

    HashSet<string> QueryByEmail(ContactQueryFilter filter)
    {
        var (op, val) = GetLikeArgs(filter.Operation, filter.Value);
        return QueryDataTable(
            CommonColumns.Email.ContentItemType,
            CommonColumns.Email.InterfaceConsts.Data1,
            op,
            val
        );
    }

    static (string Op, string Value) GetLikeArgs(ContactFilterOperation operation, string value)
    {
        return operation switch
        {
            ContactFilterOperation.Contains => ("LIKE", "%" + value + "%"),
            ContactFilterOperation.StartsWith => ("LIKE", value + "%"),
            ContactFilterOperation.EndsWith => ("LIKE", "%" + value),
            ContactFilterOperation.Equals => ("=", value),
            _ => ("LIKE", "%" + value + "%")
        };
    }

    // ── Reading contacts ─────────────────────────────────────────────

    List<Contact> ReadContacts(string? selection, string[]? selectionArgs)
    {
        var contacts = new Dictionary<string, Contact>();

        // Step 1: Load contact stubs with display names
        using (var cursor = Resolver.Query(
            ContactsContract.Contacts.ContentUri,
            new[]
            {
                ContactsContract.Contacts.InterfaceConsts.Id,
                ContactsContract.Contacts.InterfaceConsts.DisplayName
            },
            selection,
            selectionArgs,
            null))
        {
            if (cursor != null)
            {
                var idIdx = cursor.GetColumnIndex(ContactsContract.Contacts.InterfaceConsts.Id);
                var nameIdx = cursor.GetColumnIndex(ContactsContract.Contacts.InterfaceConsts.DisplayName);

                while (cursor.MoveToNext())
                {
                    var id = cursor.GetString(idIdx);
                    if (id == null) continue;

                    contacts[id] = new Contact
                    {
                        Id = id,
                        DisplayName = cursor.GetString(nameIdx) ?? string.Empty
                    };
                }
            }
        }

        if (contacts.Count == 0)
            return new List<Contact>();

        // Step 2: Load all data rows for these contacts
        var contactIdList = contacts.Keys.ToList();
        var placeholders = string.Join(",", contactIdList.Select(_ => "?"));
        var dataSelection = DataColumns.InterfaceConsts.ContactId + " IN (" + placeholders + ")";
        var dataArgs = contactIdList.ToArray();

        using (var cursor = Resolver.Query(
            DataColumns.ContentUri,
            new[]
            {
                DataColumns.InterfaceConsts.ContactId,
                DataColumns.InterfaceConsts.Mimetype,
                DataColumns.InterfaceConsts.Data1,
                DataColumns.InterfaceConsts.Data2,
                DataColumns.InterfaceConsts.Data3,
                DataColumns.InterfaceConsts.Data4,
                DataColumns.InterfaceConsts.Data5,
                DataColumns.InterfaceConsts.Data6,
                DataColumns.InterfaceConsts.Data7,
                DataColumns.InterfaceConsts.Data8,
                DataColumns.InterfaceConsts.Data9,
                DataColumns.InterfaceConsts.Data10,
                DataColumns.InterfaceConsts.Data15
            },
            dataSelection,
            dataArgs,
            null))
        {
            if (cursor != null)
            {
                var contactIdIdx = cursor.GetColumnIndex(DataColumns.InterfaceConsts.ContactId);
                var mimeIdx = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Mimetype);
                var d1 = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Data1);
                var d2 = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Data2);
                var d3 = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Data3);
                var d4 = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Data4);
                var d5 = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Data5);
                var d6 = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Data6);
                var d7 = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Data7);
                var d8 = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Data8);
                var d9 = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Data9);
                var d10 = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Data10);
                var d15 = cursor.GetColumnIndex(DataColumns.InterfaceConsts.Data15);

                while (cursor.MoveToNext())
                {
                    var cid = cursor.GetString(contactIdIdx);
                    if (cid == null || !contacts.TryGetValue(cid, out var contact))
                        continue;

                    var mime = cursor.GetString(mimeIdx);
                    switch (mime)
                    {
                        case CommonColumns.StructuredName.ContentItemType:
                            ReadStructuredName(cursor, contact, d1, d2, d3, d4, d5, d6);
                            break;

                        case CommonColumns.Nickname.ContentItemType:
                            contact.Nickname = cursor.GetString(d1);
                            break;

                        case CommonColumns.Phone.ContentItemType:
                            ReadPhone(cursor, contact, d1, d2, d3);
                            break;

                        case CommonColumns.Email.ContentItemType:
                            ReadEmail(cursor, contact, d1, d2, d3);
                            break;

                        case CommonColumns.StructuredPostal.ContentItemType:
                            ReadAddress(cursor, contact, d2, d3, d4, d7, d8, d9, d10);
                            break;

                        case CommonColumns.Organization.ContentItemType:
                            ReadOrganization(cursor, contact, d1, d4, d5);
                            break;

                        case CommonColumns.Note.ContentItemType:
                            contact.Note = cursor.GetString(d1);
                            break;

                        case CommonColumns.Event.ContentItemType:
                            ReadDate(cursor, contact, d1, d2, d3);
                            break;

                        case CommonColumns.Relation.ContentItemType:
                            ReadRelationship(cursor, contact, d1, d2, d3);
                            break;

                        case CommonColumns.Website.ContentItemType:
                            ReadWebsite(cursor, contact, d1, d3);
                            break;

                        case CommonColumns.Photo.ContentItemType:
                            ReadPhotoBlob(cursor, contact, d15);
                            break;
                    }
                }
            }
        }

        // Step 3: Load photos & thumbnails via photo streams
        foreach (var kvp in contacts)
        {
            var contactUri = ContentUris.WithAppendedId(ContactsContract.Contacts.ContentUri, long.Parse(kvp.Key));
            if (contactUri == null) continue;

            // Thumbnail
            if (kvp.Value.Thumbnail == null)
            {
                using var thumbStream = ContactsContract.Contacts.OpenContactPhotoInputStream(Resolver, contactUri, false);
                if (thumbStream != null)
                {
                    using var ms = new MemoryStream();
                    thumbStream.CopyTo(ms);
                    kvp.Value.Thumbnail = ms.ToArray();
                }
            }

            // Full-size photo
            if (kvp.Value.Photo == null)
            {
                using var photoStream = ContactsContract.Contacts.OpenContactPhotoInputStream(Resolver, contactUri, true);
                if (photoStream != null)
                {
                    using var ms = new MemoryStream();
                    photoStream.CopyTo(ms);
                    kvp.Value.Photo = ms.ToArray();
                }
            }
        }

        return contacts.Values.ToList();
    }

    // ── Data row readers ─────────────────────────────────────────────

    static void ReadStructuredName(ICursor cursor, Contact contact, int d1, int d2, int d3, int d4, int d5, int d6)
    {
        contact.DisplayName = cursor.GetString(d1) ?? contact.DisplayName;
        contact.GivenName = cursor.GetString(d2);
        contact.FamilyName = cursor.GetString(d3);
        contact.NamePrefix = cursor.GetString(d4);
        contact.MiddleName = cursor.GetString(d5);
        contact.NameSuffix = cursor.GetString(d6);
    }

    static void ReadPhone(ICursor cursor, Contact contact, int d1, int d2, int d3)
    {
        var number = cursor.GetString(d1);
        if (string.IsNullOrWhiteSpace(number)) return;

        var type = cursor.GetInt(d2);
        contact.Phones.Add(new ContactPhone
        {
            Number = number,
            Type = FromAndroidPhoneType(type),
            Label = cursor.GetString(d3)
        });
    }

    static void ReadEmail(ICursor cursor, Contact contact, int d1, int d2, int d3)
    {
        var address = cursor.GetString(d1);
        if (string.IsNullOrWhiteSpace(address)) return;

        var type = cursor.GetInt(d2);
        contact.Emails.Add(new ContactEmail
        {
            Address = address,
            Type = FromAndroidEmailType(type),
            Label = cursor.GetString(d3)
        });
    }

    static void ReadAddress(ICursor cursor, Contact contact, int d2, int d3, int d4, int d7, int d8, int d9, int d10)
    {
        contact.Addresses.Add(new ContactAddress
        {
            Street = cursor.GetString(d4),
            City = cursor.GetString(d7),
            State = cursor.GetString(d8),
            PostalCode = cursor.GetString(d9),
            Country = cursor.GetString(d10),
            Type = FromAndroidAddressType(cursor.GetInt(d2)),
            Label = cursor.GetString(d3)
        });
    }

    static void ReadOrganization(ICursor cursor, Contact contact, int d1, int d4, int d5)
    {
        contact.Organization = new ContactOrganization
        {
            Company = cursor.GetString(d1),
            Title = cursor.GetString(d4),
            Department = cursor.GetString(d5)
        };
    }

    static void ReadDate(ICursor cursor, Contact contact, int d1, int d2, int d3)
    {
        var dateStr = cursor.GetString(d1);
        if (string.IsNullOrWhiteSpace(dateStr)) return;

        if (!TryParseContactDate(dateStr, out var date)) return;

        var type = cursor.GetInt(d2);
        contact.Dates.Add(new ContactDate
        {
            Date = date,
            Type = FromAndroidEventType(type),
            Label = cursor.GetString(d3)
        });
    }

    static bool TryParseContactDate(string dateStr, out DateOnly date)
    {
        date = default;

        // Android stores dates as yyyy-MM-dd or --MM-dd (no year)
        if (dateStr.StartsWith("--"))
            dateStr = "0001" + dateStr.Substring(1);

        if (DateOnly.TryParse(dateStr, out date))
            return true;

        // Try common formats
        if (DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out date))
            return true;

        return false;
    }

    static void ReadRelationship(ICursor cursor, Contact contact, int d1, int d2, int d3)
    {
        var name = cursor.GetString(d1);
        if (string.IsNullOrWhiteSpace(name)) return;

        var type = cursor.GetInt(d2);
        contact.Relationships.Add(new ContactRelationship
        {
            Name = name,
            Type = FromAndroidRelationType(type),
            Label = cursor.GetString(d3)
        });
    }

    static void ReadWebsite(ICursor cursor, Contact contact, int d1, int d3)
    {
        var url = cursor.GetString(d1);
        if (string.IsNullOrWhiteSpace(url)) return;

        contact.Websites.Add(new ContactWebsite
        {
            Url = url,
            Label = cursor.GetString(d3)
        });
    }

    static void ReadPhotoBlob(ICursor cursor, Contact contact, int d15)
    {
        var blob = cursor.GetBlob(d15);
        if (blob != null && blob.Length > 0)
            contact.Thumbnail = blob;
    }

    // ── Insert helpers (back-reference) ──────────────────────────────

    static void AddStructuredNameInsert(List<ContentProviderOperation> ops, Contact contact, int backRef)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValueBackReference(DataColumns.InterfaceConsts.RawContactId, backRef)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.StructuredName.ContentItemType)
            .WithValue(CommonColumns.StructuredName.DisplayName, contact.DisplayName)
            .WithValue(CommonColumns.StructuredName.GivenName, contact.GivenName)
            .WithValue(CommonColumns.StructuredName.FamilyName, contact.FamilyName)
            .WithValue(CommonColumns.StructuredName.MiddleName, contact.MiddleName)
            .WithValue(CommonColumns.StructuredName.Prefix, contact.NamePrefix)
            .WithValue(CommonColumns.StructuredName.Suffix, contact.NameSuffix)
            .Build()!);
    }

    static void AddPhoneInsert(List<ContentProviderOperation> ops, ContactPhone phone, int backRef)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValueBackReference(DataColumns.InterfaceConsts.RawContactId, backRef)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Phone.ContentItemType)
            .WithValue(CommonColumns.Phone.Number, phone.Number)
            .WithValue(CommonColumns.Phone.InterfaceConsts.Type, ToAndroidPhoneType(phone.Type))
            .WithValue(CommonColumns.Phone.InterfaceConsts.Label, phone.Label)
            .Build()!);
    }

    static void AddEmailInsert(List<ContentProviderOperation> ops, ContactEmail email, int backRef)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValueBackReference(DataColumns.InterfaceConsts.RawContactId, backRef)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Email.ContentItemType)
            .WithValue(CommonColumns.Email.InterfaceConsts.Data1, email.Address)
            .WithValue(CommonColumns.Email.InterfaceConsts.Type, ToAndroidEmailType(email.Type))
            .WithValue(CommonColumns.Email.InterfaceConsts.Label, email.Label)
            .Build()!);
    }

    static void AddAddressInsert(List<ContentProviderOperation> ops, ContactAddress address, int backRef)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValueBackReference(DataColumns.InterfaceConsts.RawContactId, backRef)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.StructuredPostal.ContentItemType)
            .WithValue(CommonColumns.StructuredPostal.Street, address.Street)
            .WithValue(CommonColumns.StructuredPostal.City, address.City)
            .WithValue(CommonColumns.StructuredPostal.Region, address.State)
            .WithValue(CommonColumns.StructuredPostal.Postcode, address.PostalCode)
            .WithValue(CommonColumns.StructuredPostal.Country, address.Country)
            .WithValue(CommonColumns.StructuredPostal.InterfaceConsts.Type, ToAndroidAddressType(address.Type))
            .WithValue(CommonColumns.StructuredPostal.InterfaceConsts.Label, address.Label)
            .Build()!);
    }

    static void AddOrganizationInsert(List<ContentProviderOperation> ops, ContactOrganization org, int backRef)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValueBackReference(DataColumns.InterfaceConsts.RawContactId, backRef)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Organization.ContentItemType)
            .WithValue(CommonColumns.Organization.Company, org.Company)
            .WithValue(CommonColumns.Organization.Title, org.Title)
            .WithValue(CommonColumns.Organization.Department, org.Department)
            .Build()!);
    }

    static void AddNoteInsert(List<ContentProviderOperation> ops, string note, int backRef)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValueBackReference(DataColumns.InterfaceConsts.RawContactId, backRef)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Note.ContentItemType)
            .WithValue(CommonColumns.Note.InterfaceConsts.Data1, note)
            .Build()!);
    }

    static void AddDateInsert(List<ContentProviderOperation> ops, ContactDate date, int backRef)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValueBackReference(DataColumns.InterfaceConsts.RawContactId, backRef)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Event.ContentItemType)
            .WithValue(CommonColumns.Event.StartDate, date.Date.ToString("yyyy-MM-dd"))
            .WithValue(CommonColumns.Event.InterfaceConsts.Type, ToAndroidEventType(date.Type))
            .WithValue(CommonColumns.Event.InterfaceConsts.Label, date.Label)
            .Build()!);
    }

    static void AddRelationshipInsert(List<ContentProviderOperation> ops, ContactRelationship rel, int backRef)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValueBackReference(DataColumns.InterfaceConsts.RawContactId, backRef)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Relation.ContentItemType)
            .WithValue(CommonColumns.Relation.InterfaceConsts.Data1, rel.Name)
            .WithValue(CommonColumns.Relation.InterfaceConsts.Type, ToAndroidRelationType(rel.Type))
            .WithValue(CommonColumns.Relation.InterfaceConsts.Label, rel.Label)
            .Build()!);
    }

    static void AddWebsiteInsert(List<ContentProviderOperation> ops, ContactWebsite website, int backRef)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValueBackReference(DataColumns.InterfaceConsts.RawContactId, backRef)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Website.ContentItemType)
            .WithValue(CommonColumns.Website.InterfaceConsts.Data1, website.Url)
            .WithValue(CommonColumns.Website.InterfaceConsts.Label, website.Label)
            .Build()!);
    }

    static void AddPhotoInsert(List<ContentProviderOperation> ops, byte[] photo, int backRef)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValueBackReference(DataColumns.InterfaceConsts.RawContactId, backRef)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Photo.ContentItemType)
            .WithValue(CommonColumns.Photo.InterfaceConsts.Data15, photo)
            .Build()!);
    }

    // ── Insert helpers (direct raw contact ID for updates) ───────────

    static void AddStructuredNameInsertDirect(List<ContentProviderOperation> ops, Contact contact, long rawContactId)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValue(DataColumns.InterfaceConsts.RawContactId, rawContactId)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.StructuredName.ContentItemType)
            .WithValue(CommonColumns.StructuredName.DisplayName, contact.DisplayName)
            .WithValue(CommonColumns.StructuredName.GivenName, contact.GivenName)
            .WithValue(CommonColumns.StructuredName.FamilyName, contact.FamilyName)
            .WithValue(CommonColumns.StructuredName.MiddleName, contact.MiddleName)
            .WithValue(CommonColumns.StructuredName.Prefix, contact.NamePrefix)
            .WithValue(CommonColumns.StructuredName.Suffix, contact.NameSuffix)
            .Build()!);
    }

    static void AddPhoneInsertDirect(List<ContentProviderOperation> ops, ContactPhone phone, long rawContactId)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValue(DataColumns.InterfaceConsts.RawContactId, rawContactId)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Phone.ContentItemType)
            .WithValue(CommonColumns.Phone.Number, phone.Number)
            .WithValue(CommonColumns.Phone.InterfaceConsts.Type, ToAndroidPhoneType(phone.Type))
            .WithValue(CommonColumns.Phone.InterfaceConsts.Label, phone.Label)
            .Build()!);
    }

    static void AddEmailInsertDirect(List<ContentProviderOperation> ops, ContactEmail email, long rawContactId)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValue(DataColumns.InterfaceConsts.RawContactId, rawContactId)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Email.ContentItemType)
            .WithValue(CommonColumns.Email.InterfaceConsts.Data1, email.Address)
            .WithValue(CommonColumns.Email.InterfaceConsts.Type, ToAndroidEmailType(email.Type))
            .WithValue(CommonColumns.Email.InterfaceConsts.Label, email.Label)
            .Build()!);
    }

    static void AddAddressInsertDirect(List<ContentProviderOperation> ops, ContactAddress address, long rawContactId)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValue(DataColumns.InterfaceConsts.RawContactId, rawContactId)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.StructuredPostal.ContentItemType)
            .WithValue(CommonColumns.StructuredPostal.Street, address.Street)
            .WithValue(CommonColumns.StructuredPostal.City, address.City)
            .WithValue(CommonColumns.StructuredPostal.Region, address.State)
            .WithValue(CommonColumns.StructuredPostal.Postcode, address.PostalCode)
            .WithValue(CommonColumns.StructuredPostal.Country, address.Country)
            .WithValue(CommonColumns.StructuredPostal.InterfaceConsts.Type, ToAndroidAddressType(address.Type))
            .WithValue(CommonColumns.StructuredPostal.InterfaceConsts.Label, address.Label)
            .Build()!);
    }

    static void AddOrganizationInsertDirect(List<ContentProviderOperation> ops, ContactOrganization org, long rawContactId)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValue(DataColumns.InterfaceConsts.RawContactId, rawContactId)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Organization.ContentItemType)
            .WithValue(CommonColumns.Organization.Company, org.Company)
            .WithValue(CommonColumns.Organization.Title, org.Title)
            .WithValue(CommonColumns.Organization.Department, org.Department)
            .Build()!);
    }

    static void AddNoteInsertDirect(List<ContentProviderOperation> ops, string note, long rawContactId)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValue(DataColumns.InterfaceConsts.RawContactId, rawContactId)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Note.ContentItemType)
            .WithValue(CommonColumns.Note.InterfaceConsts.Data1, note)
            .Build()!);
    }

    static void AddDateInsertDirect(List<ContentProviderOperation> ops, ContactDate date, long rawContactId)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValue(DataColumns.InterfaceConsts.RawContactId, rawContactId)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Event.ContentItemType)
            .WithValue(CommonColumns.Event.StartDate, date.Date.ToString("yyyy-MM-dd"))
            .WithValue(CommonColumns.Event.InterfaceConsts.Type, ToAndroidEventType(date.Type))
            .WithValue(CommonColumns.Event.InterfaceConsts.Label, date.Label)
            .Build()!);
    }

    static void AddRelationshipInsertDirect(List<ContentProviderOperation> ops, ContactRelationship rel, long rawContactId)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValue(DataColumns.InterfaceConsts.RawContactId, rawContactId)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Relation.ContentItemType)
            .WithValue(CommonColumns.Relation.InterfaceConsts.Data1, rel.Name)
            .WithValue(CommonColumns.Relation.InterfaceConsts.Type, ToAndroidRelationType(rel.Type))
            .WithValue(CommonColumns.Relation.InterfaceConsts.Label, rel.Label)
            .Build()!);
    }

    static void AddWebsiteInsertDirect(List<ContentProviderOperation> ops, ContactWebsite website, long rawContactId)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValue(DataColumns.InterfaceConsts.RawContactId, rawContactId)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Website.ContentItemType)
            .WithValue(CommonColumns.Website.InterfaceConsts.Data1, website.Url)
            .WithValue(CommonColumns.Website.InterfaceConsts.Label, website.Label)
            .Build()!);
    }

    static void AddPhotoInsertDirect(List<ContentProviderOperation> ops, byte[] photo, long rawContactId)
    {
        ops.Add(ContentProviderOperation
            .NewInsert(DataColumns.ContentUri)!
            .WithValue(DataColumns.InterfaceConsts.RawContactId, rawContactId)
            .WithValue(DataColumns.InterfaceConsts.Mimetype, CommonColumns.Photo.ContentItemType)
            .WithValue(CommonColumns.Photo.InterfaceConsts.Data15, photo)
            .Build()!);
    }

    // ── Utility ──────────────────────────────────────────────────────

    static long? GetRawContactId(string contactId)
    {
        using var cursor = Resolver.Query(
            RawContactsColumns.ContentUri,
            new[] { RawContactsColumns.InterfaceConsts.Id },
            RawContactsColumns.InterfaceConsts.ContactId + " = ?",
            new[] { contactId },
            null
        );

        if (cursor != null && cursor.MoveToFirst())
            return cursor.GetLong(0);

        return null;
    }

    // ── Enum mapping: Phone ──────────────────────────────────────────

    static int ToAndroidPhoneType(PhoneType type) => type switch
    {
        PhoneType.Home => (int)Android.Provider.PhoneDataKind.Home,
        PhoneType.Mobile => (int)Android.Provider.PhoneDataKind.Mobile,
        PhoneType.Work => (int)Android.Provider.PhoneDataKind.Work,
        PhoneType.FaxWork => (int)Android.Provider.PhoneDataKind.FaxWork,
        PhoneType.FaxHome => (int)Android.Provider.PhoneDataKind.FaxHome,
        PhoneType.Pager => (int)Android.Provider.PhoneDataKind.Pager,
        PhoneType.Custom => (int)Android.Provider.PhoneDataKind.Custom,
        _ => (int)Android.Provider.PhoneDataKind.Other
    };

    static PhoneType FromAndroidPhoneType(int type) => type switch
    {
        (int)Android.Provider.PhoneDataKind.Home => PhoneType.Home,
        (int)Android.Provider.PhoneDataKind.Mobile => PhoneType.Mobile,
        (int)Android.Provider.PhoneDataKind.Work => PhoneType.Work,
        (int)Android.Provider.PhoneDataKind.FaxWork => PhoneType.FaxWork,
        (int)Android.Provider.PhoneDataKind.FaxHome => PhoneType.FaxHome,
        (int)Android.Provider.PhoneDataKind.Pager => PhoneType.Pager,
        (int)Android.Provider.PhoneDataKind.Custom => PhoneType.Custom,
        _ => PhoneType.Other
    };

    // ── Enum mapping: Email ──────────────────────────────────────────

    static int ToAndroidEmailType(EmailType type) => type switch
    {
        EmailType.Home => (int)Android.Provider.EmailDataKind.Home,
        EmailType.Work => (int)Android.Provider.EmailDataKind.Work,
        EmailType.Custom => (int)Android.Provider.EmailDataKind.Custom,
        _ => (int)Android.Provider.EmailDataKind.Other
    };

    static EmailType FromAndroidEmailType(int type) => type switch
    {
        (int)Android.Provider.EmailDataKind.Home => EmailType.Home,
        (int)Android.Provider.EmailDataKind.Work => EmailType.Work,
        (int)Android.Provider.EmailDataKind.Custom => EmailType.Custom,
        _ => EmailType.Other
    };

    // ── Enum mapping: Address ────────────────────────────────────────

    static int ToAndroidAddressType(AddressType type) => type switch
    {
        AddressType.Home => (int)Android.Provider.AddressDataKind.Home,
        AddressType.Work => (int)Android.Provider.AddressDataKind.Work,
        AddressType.Custom => (int)Android.Provider.AddressDataKind.Custom,
        _ => (int)Android.Provider.AddressDataKind.Other
    };

    static AddressType FromAndroidAddressType(int type) => type switch
    {
        (int)Android.Provider.AddressDataKind.Home => AddressType.Home,
        (int)Android.Provider.AddressDataKind.Work => AddressType.Work,
        (int)Android.Provider.AddressDataKind.Custom => AddressType.Custom,
        _ => AddressType.Other
    };

    // ── Enum mapping: Event/Date ─────────────────────────────────────

    static int ToAndroidEventType(ContactDateType type) => type switch
    {
        ContactDateType.Birthday => (int)Android.Provider.EventDataKind.Birthday,
        ContactDateType.Anniversary => (int)Android.Provider.EventDataKind.Anniversary,
        ContactDateType.Custom => (int)Android.Provider.EventDataKind.Custom,
        _ => (int)Android.Provider.EventDataKind.Other
    };

    static ContactDateType FromAndroidEventType(int type) => type switch
    {
        (int)Android.Provider.EventDataKind.Birthday => ContactDateType.Birthday,
        (int)Android.Provider.EventDataKind.Anniversary => ContactDateType.Anniversary,
        (int)Android.Provider.EventDataKind.Custom => ContactDateType.Custom,
        _ => ContactDateType.Other
    };

    // ── Enum mapping: Relationship ───────────────────────────────────

    static int ToAndroidRelationType(RelationshipType type) => type switch
    {
        RelationshipType.Father => (int)Android.Provider.RelationDataKind.Father,
        RelationshipType.Mother => (int)Android.Provider.RelationDataKind.Mother,
        RelationshipType.Parent => (int)Android.Provider.RelationDataKind.Parent,
        RelationshipType.Brother => (int)Android.Provider.RelationDataKind.Brother,
        RelationshipType.Sister => (int)Android.Provider.RelationDataKind.Sister,
        RelationshipType.Child => (int)Android.Provider.RelationDataKind.Child,
        RelationshipType.Friend => (int)Android.Provider.RelationDataKind.Friend,
        RelationshipType.Spouse => (int)Android.Provider.RelationDataKind.Spouse,
        RelationshipType.Partner => (int)Android.Provider.RelationDataKind.Partner,
        RelationshipType.Assistant => (int)Android.Provider.RelationDataKind.Assistant,
        RelationshipType.Manager => (int)Android.Provider.RelationDataKind.Manager,
        RelationshipType.Custom => (int)Android.Provider.RelationDataKind.Custom,
        _ => (int)Android.Provider.RelationDataKind.Custom
    };

    static RelationshipType FromAndroidRelationType(int type) => type switch
    {
        (int)Android.Provider.RelationDataKind.Father => RelationshipType.Father,
        (int)Android.Provider.RelationDataKind.Mother => RelationshipType.Mother,
        (int)Android.Provider.RelationDataKind.Parent => RelationshipType.Parent,
        (int)Android.Provider.RelationDataKind.Brother => RelationshipType.Brother,
        (int)Android.Provider.RelationDataKind.Sister => RelationshipType.Sister,
        (int)Android.Provider.RelationDataKind.Child => RelationshipType.Child,
        (int)Android.Provider.RelationDataKind.Friend => RelationshipType.Friend,
        (int)Android.Provider.RelationDataKind.Spouse => RelationshipType.Spouse,
        (int)Android.Provider.RelationDataKind.Partner => RelationshipType.Partner,
        (int)Android.Provider.RelationDataKind.Assistant => RelationshipType.Assistant,
        (int)Android.Provider.RelationDataKind.Manager => RelationshipType.Manager,
        (int)Android.Provider.RelationDataKind.Custom => RelationshipType.Custom,
        _ => RelationshipType.Other
    };
}
