using Shiny.Contacts;
using Contact = Shiny.Contacts.Contact;
using ContactPhone = Shiny.Contacts.ContactPhone;
using ContactEmail = Shiny.Contacts.ContactEmail;

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// Real Contacts / ContactsContract CRUD and queries. Every contact the suite creates has a family name starting with
/// <see cref="Prefix"/>, so TeardownAsync can sweep anything an earlier, crashed run left behind.
/// </summary>
public class ContactsTests(IContactStore store) : DeviceTestSuite
{
    public const string Prefix = "ShinyE2E";

    // smallest valid PNG (1x1, opaque) - the stores re-encode photos, so only presence is asserted
    static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
    );

    public override string Name => "Contacts";


    public override async Task SetupAsync(TestContext context)
    {
        var access = await store.RequestAccess(context.CancellationToken);
        if (access != AccessState.Available)
            throw new SkipException($"Contacts access is {access} - the runner pre-grants it on simulators/emulators");

        await this.SweepAsync(context);
    }


    public override Task TeardownAsync(TestContext context) => this.SweepAsync(context);


    protected override void Define()
    {
        this.Test("Access_IsAvailable", _ =>
        {
            Assert.Equal(AccessState.Available, store.GetCurrentAccess());
            return Task.CompletedTask;
        });

        this.Test("Contact_CreateReadDelete_AllFields", async ctx =>
        {
            var source = NewFullContact(NewMarker());
            var id = await store.Create(source, ctx.CancellationToken);
            Assert.False(String.IsNullOrWhiteSpace(id));
            try
            {
                var read = await store.GetById(id, ctx.CancellationToken);
                Assert.NotNull(read);
                Assert.Equal(id, read.Id);

                Assert.Equal(source.NamePrefix, read.NamePrefix);
                Assert.Equal(source.GivenName, read.GivenName);
                Assert.Equal(source.MiddleName, read.MiddleName);
                Assert.Equal(source.FamilyName, read.FamilyName);
                Assert.Equal(source.NameSuffix, read.NameSuffix);
                Assert.Equal(source.Nickname, read.Nickname);
                Assert.Contains(source.GivenName!, read.DisplayName);

                Assert.NotNull(read.Organization);
                Assert.Equal("Shiny Labs", read.Organization.Company);
                Assert.Equal("Tester", read.Organization.Title);
                Assert.Equal("QA", read.Organization.Department);

                AssertPhones(source.Phones, read.Phones);
                Assert.Equal(
                    source.Emails.Select(x => (x.Address, x.Type)).Order(),
                    read.Emails.Select(x => (x.Address, x.Type)).Order()
                );

                Assert.Equal(2, read.Addresses.Count);
                var home = Assert.Single(read.Addresses, x => x.Type == AddressType.Home);
                Assert.Equal("1 Shiny Way", home.Street);
                Assert.Equal("Toronto", home.City);
                Assert.Equal("ON", home.State);
                Assert.Equal("M5V 1A1", home.PostalCode);
                Assert.Equal("Canada", home.Country);
                Assert.Equal("Seattle", Assert.Single(read.Addresses, x => x.Type == AddressType.Work).City);

                Assert.Contains(read.Websites, x => x.Url.TrimEnd('/') == "https://shinylib.net");

                var birthday = Assert.Single(read.Dates, x => x.Type == ContactDateType.Birthday);
                Assert.Equal(new DateOnly(1990, 4, 15), birthday.Date);
                Assert.Contains(read.Dates, x => x.Type == ContactDateType.Anniversary && x.Date == new DateOnly(2015, 6, 20));

                // Apple only returns notes + relations with the com.apple.developer.contacts.notes entitlement
                if (ctx.IsAndroid)
                {
                    Assert.Equal(source.Note, read.Note);
                    Assert.Contains(read.Relationships, x => x.Name == "Pat Tester" && x.Type == RelationshipType.Manager);
                }
                else
                {
                    ctx.Log($"Note='{read.Note}' Relationships={read.Relationships.Count} (needs the notes entitlement on Apple)");
                }
            }
            finally
            {
                await store.Delete(id, ctx.CancellationToken);
            }
            Assert.Null(await store.GetById(id, ctx.CancellationToken));
        });

        this.Test("Contact_Update", async ctx =>
        {
            var marker = NewMarker();
            var id = await store.Create(new Contact
            {
                GivenName = "Before",
                FamilyName = marker,
                Phones = [new ContactPhone("+15555550100", PhoneType.Mobile)],
                Emails =
                [
                    new ContactEmail("keep@shinylib.net", EmailType.Work),
                    new ContactEmail("drop@shinylib.net", EmailType.Home)
                ]
            }, ctx.CancellationToken);
            try
            {
                var contact = await store.GetById(id, ctx.CancellationToken);
                Assert.NotNull(contact);

                contact.GivenName = "After";
                contact.FamilyName = marker + "x";
                contact.Phones.Add(new ContactPhone("+15555550101", PhoneType.Work));
                contact.Emails.RemoveAll(x => x.Address == "drop@shinylib.net");
                contact.Organization = new ContactOrganization("Updated Co");
                await store.Update(contact, ctx.CancellationToken);

                var updated = await store.GetById(id, ctx.CancellationToken);
                Assert.NotNull(updated);
                Assert.Equal("After", updated.GivenName);
                Assert.Equal(marker + "x", updated.FamilyName);
                AssertPhones(contact.Phones, updated.Phones);
                Assert.Equal("keep@shinylib.net", Assert.Single(updated.Emails).Address);
                Assert.Equal("Updated Co", updated.Organization?.Company);
            }
            finally
            {
                await store.Delete(id, ctx.CancellationToken);
            }
        });

        this.Test("Contact_Photo", async ctx =>
        {
            var id = await store.Create(new Contact
            {
                GivenName = "Photo",
                FamilyName = NewMarker(),
                Photo = Png
            }, ctx.CancellationToken);
            try
            {
                var single = await store.GetById(id, ctx.CancellationToken);
                Assert.NotNull(single);
                Assert.NotNull(single.Photo);
                Assert.NotEmpty(single.Photo);
                ctx.Log($"Photo {single.Photo.Length} bytes, thumbnail {single.Thumbnail?.Length ?? 0} bytes");

                // bulk reads deliberately leave the full photo out (memory) - only the thumbnail comes back
                var bulk = (await store.GetAll(ctx.CancellationToken)).Single(x => x.Id == id);
                Assert.Null(bulk.Photo);
            }
            finally
            {
                await store.Delete(id, ctx.CancellationToken);
            }
        });

        this.Test("Query_FiltersSortAndPage", async ctx =>
        {
            var marker = NewMarker();
            var domain = $"{marker.ToLowerInvariant()}.example";
            List<string> ids = [];
            try
            {
                ids.Add(await store.Create(new Contact
                {
                    GivenName = "Alpha",
                    FamilyName = marker,
                    Emails = [new ContactEmail($"alpha@{domain}")],
                    Phones = [new ContactPhone("+15555550111")],
                    Organization = new ContactOrganization("Acme")
                }, ctx.CancellationToken));
                ids.Add(await store.Create(new Contact
                {
                    GivenName = "Bravo",
                    FamilyName = marker,
                    Emails = [new ContactEmail("bravo@elsewhere.example")],
                    Organization = new ContactOrganization("Zenith")
                }, ctx.CancellationToken));
                ids.Add(await store.Create(new Contact
                {
                    GivenName = "Charlie",
                    FamilyName = marker,
                    Organization = new ContactOrganization("Acme")
                }, ctx.CancellationToken));

                var byFamily = await store
                    .Query()
                    .Where(ContactField.FamilyName, marker, ContactFilterOperation.Equals)
                    .OrderBy(ContactSortField.GivenName, descending: true)
                    .ToListAsync(ctx.CancellationToken);
                Assert.Equal(["Charlie", "Bravo", "Alpha"], byFamily.Select(x => x.GivenName));

                var exact = await store
                    .Query()
                    .Where(ContactField.FamilyName, marker, ContactFilterOperation.StartsWith)
                    .Where(ContactField.GivenName, "bravo", ContactFilterOperation.Equals)
                    .ToListAsync(ctx.CancellationToken);
                Assert.Equal(ids[1], Assert.Single(exact).Id);

                var byEmail = await store
                    .Query()
                    .Where(ContactField.Email, domain, ContactFilterOperation.EndsWith)
                    .ToListAsync(ctx.CancellationToken);
                Assert.Equal(ids[0], Assert.Single(byEmail).Id);

                var byPhone = await store
                    .Query()
                    .Where(ContactField.FamilyName, marker)
                    .Where(ContactField.Phone, "5550111")
                    .ToListAsync(ctx.CancellationToken);
                Assert.Equal(ids[0], Assert.Single(byPhone).Id);

                var acme = await store
                    .Query()
                    .Where(ContactField.FamilyName, marker)
                    .Where(ContactField.Company, "acme", ContactFilterOperation.Equals)
                    .OrderBy(ContactSortField.Company)
                    .ThenBy(ContactSortField.GivenName)
                    .ToListAsync(ctx.CancellationToken);
                Assert.Equal(["Alpha", "Charlie"], acme.Select(x => x.GivenName));

                var anyMatch = await store
                    .Query()
                    .Match(ContactFilterMatch.Any)
                    .Where(ContactField.Email, $"alpha@{domain}", ContactFilterOperation.Equals)
                    .Where(ContactField.GivenName, "Charlie", ContactFilterOperation.Equals)
                    .Where(x => x.FamilyName == marker)
                    .ToListAsync(ctx.CancellationToken);
                Assert.Equal(new[] { ids[0], ids[2] }.Order(), anyMatch.Select(x => x.Id!).Order());

                var search = await store.Query().Search(marker).CountAsync(ctx.CancellationToken);
                Assert.Equal(3, search);

                var page = await store
                    .Query()
                    .Where(ContactField.FamilyName, marker)
                    .OrderBy(ContactSortField.GivenName)
                    .Skip(1)
                    .Take(1)
                    .FirstOrDefaultAsync(ctx.CancellationToken);
                Assert.Equal("Bravo", page?.GivenName);
            }
            finally
            {
                foreach (var id in ids)
                    await store.Delete(id, ctx.CancellationToken);
            }

            Assert.Equal(0, await store.Query().Where(ContactField.FamilyName, marker).CountAsync(ctx.CancellationToken));
        });

        this.Test("GetAll_IncludesCreated", async ctx =>
        {
            var marker = NewMarker();
            var id = await store.Create(new Contact { GivenName = "Listed", FamilyName = marker }, ctx.CancellationToken);
            try
            {
                var all = await store.GetAll(ctx.CancellationToken);
                ctx.Log($"{all.Count} contact(s) on the device");
                var listed = Assert.Single(all, x => x.Id == id);
                Assert.Equal(marker, listed.FamilyName);
            }
            finally
            {
                await store.Delete(id, ctx.CancellationToken);
            }

            var after = await store.GetAll(ctx.CancellationToken);
            Assert.DoesNotContain(after, x => x.Id == id);
        });
    }


    async Task SweepAsync(TestContext context)
    {
        var leftovers = await store
            .Query()
            .Where(ContactField.FamilyName, Prefix, ContactFilterOperation.StartsWith)
            .ToListAsync(context.CancellationToken);

        foreach (var contact in leftovers.Where(x => x.Id != null))
        {
            try
            {
                await store.Delete(contact.Id!, context.CancellationToken);
            }
            catch (Exception ex)
            {
                context.Log($"Sweep could not delete {contact.DisplayName}: {ex.Message}");
            }
        }

        if (leftovers.Count > 0)
            context.Log($"Swept {leftovers.Count} contact(s)");
    }


    static Contact NewFullContact(string marker) => new()
    {
        NamePrefix = "Dr.",
        GivenName = "Shiny",
        MiddleName = "Device",
        FamilyName = marker,
        NameSuffix = "Jr.",
        Nickname = "Shines",
        Organization = new ContactOrganization("Shiny Labs", "Tester", "QA"),
        Phones =
        [
            new ContactPhone("+15555550123", PhoneType.Mobile),
            new ContactPhone("+15555550124", PhoneType.Work)
        ],
        Emails =
        [
            new ContactEmail("home@shinylib.net", EmailType.Home),
            new ContactEmail("work@shinylib.net", EmailType.Work)
        ],
        Addresses =
        [
            new ContactAddress("1 Shiny Way", "Toronto", "ON", "M5V 1A1", "Canada", AddressType.Home),
            new ContactAddress("2 Work St", "Seattle", "WA", "98101", "USA", AddressType.Work)
        ],
        Websites = [new ContactWebsite("https://shinylib.net")],
        Dates =
        [
            new ContactDate(new DateOnly(1990, 4, 15), ContactDateType.Birthday),
            new ContactDate(new DateOnly(2015, 6, 20), ContactDateType.Anniversary)
        ],
        Relationships = [new ContactRelationship("Pat Tester", RelationshipType.Manager)],
        Note = "Created by Shiny.DeviceTests"
    };


    // stores may reformat numbers ("+1 555-555-0123") - compare digits and type
    static void AssertPhones(IEnumerable<ContactPhone> expected, IEnumerable<ContactPhone> actual)
        => Assert.Equal(
            expected.Select(x => (Digits(x.Number), x.Type)).Order(),
            actual.Select(x => (Digits(x.Number), x.Type)).Order()
        );

    static string Digits(string value) => new(value.Where(Char.IsDigit).ToArray());

    static string NewMarker() => $"{Prefix}{Guid.NewGuid():N}"[..(Prefix.Length + 8)];
}
