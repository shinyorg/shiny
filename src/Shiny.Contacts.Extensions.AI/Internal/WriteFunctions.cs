using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Shiny.Contacts.Extensions.AI.Internal;

/// <summary>Creates a new device contact.</summary>
sealed class CreateContactFunction : ContactAIFunctionBase
{
    public CreateContactFunction(IContactStore store)
        : base(store, "create_contact",
            "Create a new device contact. Provide at least a name or organization. Returns the new contact's id.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["givenName"] = AiSchema.String("First / given name."),
                ["familyName"] = AiSchema.String("Last / family name."),
                ["phone"] = AiSchema.String("Primary phone number."),
                ["email"] = AiSchema.String("Primary email address."),
                ["organization"] = AiSchema.String("Company / organization name."),
                ["note"] = AiSchema.String("Free-text note.")
            }));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var contact = new Contact
        {
            GivenName = GetString(arguments, "givenName"),
            FamilyName = GetString(arguments, "familyName"),
            Note = GetString(arguments, "note")
        };

        var org = GetString(arguments, "organization");
        if (!string.IsNullOrWhiteSpace(org))
            contact.Organization = new ContactOrganization(org);

        var phone = GetString(arguments, "phone");
        if (!string.IsNullOrWhiteSpace(phone))
            contact.Phones.Add(new ContactPhone(phone));

        var email = GetString(arguments, "email");
        if (!string.IsNullOrWhiteSpace(email))
            contact.Emails.Add(new ContactEmail(email));

        if (string.IsNullOrWhiteSpace(contact.GivenName) &&
            string.IsNullOrWhiteSpace(contact.FamilyName) &&
            string.IsNullOrWhiteSpace(org))
            return new JsonObject { ["error"] = "Provide at least a givenName, familyName, or organization." };

        var id = await this.Store.Create(contact, cancellationToken).ConfigureAwait(false);
        return new JsonObject { ["success"] = true, ["id"] = id, ["displayName"] = contact.DisplayName };
    }
}

/// <summary>Updates fields on an existing device contact.</summary>
sealed class UpdateContactFunction : ContactAIFunctionBase
{
    public UpdateContactFunction(IContactStore store)
        : base(store, "update_contact",
            "Update an existing contact (found by id). Only the fields you supply are changed. Adding a phone/email appends to the existing lists.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["contactId"] = AiSchema.String("The id of the contact to update."),
                ["givenName"] = AiSchema.String("New first / given name."),
                ["familyName"] = AiSchema.String("New last / family name."),
                ["addPhone"] = AiSchema.String("A phone number to add to the contact."),
                ["addEmail"] = AiSchema.String("An email address to add to the contact."),
                ["organization"] = AiSchema.String("New company / organization name."),
                ["note"] = AiSchema.String("New free-text note (replaces any existing note).")
            },
            "contactId"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var id = GetString(arguments, "contactId");
        if (string.IsNullOrWhiteSpace(id))
            return new JsonObject { ["error"] = "'contactId' is required." };

        var contact = await this.Store.GetById(id, cancellationToken).ConfigureAwait(false);
        if (contact is null)
            return new JsonObject { ["error"] = $"No contact found with id '{id}'." };

        var givenName = GetString(arguments, "givenName");
        if (givenName != null)
            contact.GivenName = givenName;

        var familyName = GetString(arguments, "familyName");
        if (familyName != null)
            contact.FamilyName = familyName;

        var note = GetString(arguments, "note");
        if (note != null)
            contact.Note = note;

        var org = GetString(arguments, "organization");
        if (!string.IsNullOrWhiteSpace(org))
            contact.Organization = new ContactOrganization(org);

        var addPhone = GetString(arguments, "addPhone");
        if (!string.IsNullOrWhiteSpace(addPhone))
            contact.Phones.Add(new ContactPhone(addPhone));

        var addEmail = GetString(arguments, "addEmail");
        if (!string.IsNullOrWhiteSpace(addEmail))
            contact.Emails.Add(new ContactEmail(addEmail));

        await this.Store.Update(contact, cancellationToken).ConfigureAwait(false);
        return new JsonObject { ["success"] = true, ["id"] = id, ["displayName"] = contact.DisplayName };
    }
}

/// <summary>Deletes a device contact by id.</summary>
sealed class DeleteContactFunction : ContactAIFunctionBase
{
    public DeleteContactFunction(IContactStore store)
        : base(store, "delete_contact",
            "Delete a contact from the device by its id. This is irreversible - confirm with the user before calling.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["contactId"] = AiSchema.String("The id of the contact to delete.")
            },
            "contactId"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var id = GetString(arguments, "contactId");
        if (string.IsNullOrWhiteSpace(id))
            return new JsonObject { ["error"] = "'contactId' is required." };

        await this.Store.Delete(id, cancellationToken).ConfigureAwait(false);
        return new JsonObject { ["success"] = true, ["id"] = id };
    }
}
