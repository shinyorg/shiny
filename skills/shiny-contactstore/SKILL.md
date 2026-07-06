---
name: shiny-contactstore
description: Generate code using Shiny.Contacts for cross-platform device contact access with CRUD, LINQ queries, and Shiny.Core permissions
auto_invoke: true
triggers:
  - contact store
  - contacts
  - IContactStore
  - ContactStore
  - AddContactStore
  - Shiny.Contacts
  - device contacts
  - read contacts
  - write contacts
  - contact query
  - contact LINQ
  - contacts AI tools
  - Shiny.Contacts.Extensions.AI
  - AddContactsAITools
  - ContactAITools
  - ContactAICapabilities
---

# Shiny.Contacts Skill

You are an expert in Shiny.Contacts, a cross-platform library for accessing device contacts on Android and iOS.

## When to Use This Skill

Invoke this skill when the user wants to:
- Access device contacts (read, create, update, delete)
- Query contacts with LINQ
- Request contact permissions using Shiny's AccessState model
- Register the contact store in DI
- Work with contact models (phones, emails, addresses, etc.)

## Library Overview

**GitHub**: https://github.com/shinyorg/shiny
**NuGet**: `Shiny.Contacts`
**Namespace**: `Shiny.Contacts`

Shiny.Contacts provides:
- Full CRUD operations on device contacts
- LINQ query support with native translation (Android content provider queries, iOS CNContact predicates)
- Permission handling via Shiny.Core's `AccessState` model
- Dependency injection integration
- AOT and trimmer compatible

## Setup

### 1. Install NuGet Package
```bash
dotnet add package Shiny.Contacts
```

### 2. Register in MauiProgram.cs
The app must call `.UseShiny()` (from Shiny.Hosting.Maui) so platform services like permissions are wired up.
```csharp
using Shiny;

builder.UseShiny();
builder.Services.AddContactStore();
```

### 3. Platform Permissions

**Android** — Add to `AndroidManifest.xml`:
```xml
<uses-permission android:name="android.permission.READ_CONTACTS" />
<uses-permission android:name="android.permission.WRITE_CONTACTS" />
```

**iOS** — Add to `Info.plist`:
```xml
<key>NSContactsUsageDescription</key>
<string>This app needs access to your contacts.</string>
```

## Permissions

Permissions use Shiny.Core's `Shiny.AccessState` model. `IContactStore` exposes two members:

```csharp
// Request access (triggers OS prompt if needed)
var access = await contactStore.RequestAccess();
if (access != AccessState.Available)
{
    // Handle denied / restricted
    return;
}

// Check current state without prompting
var current = contactStore.GetCurrentAccess();
```

### Android Access Results
- `AccessState.Available` — both read and write access granted
- `AccessState.Restricted` — only read or only write granted (not both)
- `AccessState.Denied` — neither read nor write granted

### iOS Access Results
- `AccessState.Available` — contacts access authorized
- `AccessState.Denied` — contacts access denied
- `AccessState.Restricted` — contacts access restricted or limited
- `AccessState.Unknown` — not yet determined

## API Reference

### IContactStore Interface

```csharp
public interface IContactStore
{
    AccessState GetCurrentAccess();
    Task<AccessState> RequestAccess(CancellationToken ct = default);
    Task<IReadOnlyList<Contact>> GetAll(CancellationToken ct = default);
    Task<Contact?> GetById(string contactId, CancellationToken ct = default);
    IQueryable<Contact> Query();
    Task<string> Create(Contact contact, CancellationToken ct = default);
    Task Update(Contact contact, CancellationToken ct = default);
    Task Delete(string contactId, CancellationToken ct = default);
}
```

### Extension Methods

```csharp
// Query extensions
Task<IReadOnlyList<char>> contactStore.GetFamilyNameFirstLetters(CancellationToken ct = default);
```

### Query with LINQ

The library translates LINQ predicates to native queries where possible, with in-memory fallback.

```csharp
// Filter by name
var results = contactStore.Query()
    .Where(c => c.GivenName.Contains("John"))
    .ToList();

// Filter by phone number
var results = contactStore.Query()
    .Where(c => c.Phones.Any(p => p.Number.Contains("555")))
    .ToList();

// Filter by email
var results = contactStore.Query()
    .Where(c => c.Emails.Any(e => e.Address.Contains("@example.com")))
    .ToList();

// Combine filters
var results = contactStore.Query()
    .Where(c => c.GivenName.StartsWith("J") && c.FamilyName.Contains("Smith"))
    .ToList();

// Paging
var page = contactStore.Query()
    .Where(c => c.FamilyName.StartsWith("A"))
    .Skip(10)
    .Take(20)
    .ToList();
```

**Supported operations:** `Contains`, `StartsWith`, `EndsWith`, `Equals`

**Filterable properties:** `GivenName`, `FamilyName`, `MiddleName`, `NamePrefix`, `NameSuffix`, `Nickname`, `DisplayName`, `Note`

**Filterable collections:** `Phones` (by `Number`), `Emails` (by `Address`)

### Create a Contact

```csharp
var contact = new Contact
{
    GivenName = "John",
    FamilyName = "Doe",
    Note = "Met at conference"
};
contact.Phones.Add(new ContactPhone("555-1234", PhoneType.Mobile));
contact.Emails.Add(new ContactEmail("john@example.com", EmailType.Work));

string id = await contactStore.Create(contact);
```

### Update a Contact

```csharp
var contact = await contactStore.GetById(id);
contact.GivenName = "Jane";
await contactStore.Update(contact);
```

### Delete a Contact

```csharp
await contactStore.Delete(contactId);
```

## Models

### Contact

| Property       | Type                        |
|----------------|-----------------------------|
| Id             | `string?`                   |
| NamePrefix     | `string?`                   |
| GivenName      | `string?`                   |
| MiddleName     | `string?`                   |
| FamilyName     | `string?`                   |
| NameSuffix     | `string?`                   |
| Nickname       | `string?`                   |
| DisplayName    | `string`                    |
| Note           | `string?`                   |
| Organization   | `ContactOrganization?`      |
| Photo          | `byte[]?` (see note below)  |
| Thumbnail      | `byte[]?`                   |
| Phones         | `List<ContactPhone>`        |
| Emails         | `List<ContactEmail>`        |
| Addresses      | `List<ContactAddress>`      |
| Dates          | `List<ContactDate>`         |
| Relationships  | `List<ContactRelationship>` |
| Websites       | `List<ContactWebsite>`      |

### Enums

**PhoneType:** Home, Mobile, Work, FaxWork, FaxHome, Pager, Other, Custom

**EmailType:** Home, Work, Other, Custom

**AddressType:** Home, Work, Other, Custom

**ContactDateType:** Birthday, Anniversary, Other, Custom

**RelationshipType:** Father, Mother, Parent, Brother, Sister, Child, Friend, Spouse, Partner, Assistant, Manager, Other, Custom

## Photo vs Thumbnail (bulk reads)

`GetAll()` and `Query()` populate **`Thumbnail`** only; **`Photo` (the full-resolution image) is `null`** on these bulk reads. Decoding every contact's full photo into a `byte[]` at once spikes memory and can get the app OOM/jetsam-killed on a real device with many photo contacts. To get the full `Photo`, fetch the single contact with **`GetById(id)`** (which populates both `Thumbnail` and `Photo`). Bind list rows to `Thumbnail` and load `Photo` on a detail screen.

## iOS Notes & Relations Entitlement

Reading `Note` and `Relationships` on iOS requires the `com.apple.developer.contacts.notes` entitlement. The library auto-detects this at runtime. If absent, `Note` returns `null` and `Relationships` is empty.

## Best Practices

1. **Always request access first** — use `await contactStore.RequestAccess()` and check for `AccessState.Available` before any CRUD operation
2. **Use LINQ queries for filtering** — prefer `Query().Where(...)` over `GetAll()` when filtering, as it uses native queries
3. **Check for Restricted on Android** — `AccessState.Restricted` means partial access (read-only or write-only)
4. **Handle iOS entitlements gracefully** — Notes and Relations silently return empty without the entitlement
5. **Bind lists to `Thumbnail`, not `Photo`** — bulk reads (`GetAll`/`Query`) only load `Thumbnail`; get the full `Photo` from `GetById` on a detail screen
6. **Use primary constructors** — inject `IContactStore` via primary constructor

## AI Tool Integration (Shiny.Contacts.Extensions.AI)

The optional `Shiny.Contacts.Extensions.AI` package exposes `IContactStore` as `Microsoft.Extensions.AI` tool functions (`AIFunction`s) for LLM agents. You opt-in exactly which operations the model can see — a read/write allow-list you control on behalf of the agent (**not** an OS permission prompt; the platform contact permission must already be granted). Read-only by default; write is opt-in. AOT-compatible (hand-built schemas, `JsonNode` results — no reflection).

```csharp
using Shiny.Contacts;
using Shiny.Contacts.Extensions.AI;

builder.Services.AddContactStore();                          // registers IContactStore
builder.Services.AddContactsAITools(tools => tools
    .AddContacts(ContactAICapabilities.ReadWrite)            // Read is the default; ReadWrite adds create/update/delete
);

// resolve the bundle and pass the tools to any IChatClient
var tools = sp.GetRequiredService<ContactAITools>().Tools;
var response = await chatClient.GetResponseAsync(
    messages,
    new ChatOptions { Tools = [.. tools] }
);
```

Key types:
- `AddContactsAITools(Action<IContactAIToolBuilder>)` — DI extension; throws if nothing is added.
- `IContactAIToolBuilder` — `AddContacts(ContactAICapabilities)`.
- `ContactAICapabilities` `[Flags]` — `None`, `Read` (default), `Write`, `ReadWrite`.
- `ContactAITools` — resolve from DI; `.Tools` is `IReadOnlyList<AITool>`.

Generated tools (only for opted-in capabilities): `search_contacts` (free-text over name/phone/email), `get_contact` (by id), `create_contact`, `update_contact`, `delete_contact`.

> The AI tools assume permissions are already granted — they do **not** trigger the platform permission UI (needs a foreground activity). Call `IContactStore.RequestAccess(...)` from the app before invoking the agent. `delete_contact` is irreversible — instruct the model to confirm with the user first.
