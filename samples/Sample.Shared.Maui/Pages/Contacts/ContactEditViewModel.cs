using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using Shiny;
using Shiny.Contacts;
using Contact = Shiny.Contacts.Contact;
using ContactPhone = Shiny.Contacts.ContactPhone;
using ContactEmail = Shiny.Contacts.ContactEmail;

namespace Sample.Shared.Maui.Pages.Contacts;

[ShellMap<ContactEditPage>]
public partial class ContactEditViewModel(
    INavigator navigator,
    IDialogs dialogs,
    IContactStore contactStore,
    IMediaPicker mediaPicker
) : ObservableObject
{
    [ObservableProperty]
    string? contactId;

    [ObservableProperty]
    bool isNew = true;

    [ObservableProperty]
    bool isBusy;

    // Name fields
    [ObservableProperty] string givenName = string.Empty;
    [ObservableProperty] string familyName = string.Empty;
    [ObservableProperty] string middleName = string.Empty;
    [ObservableProperty] string namePrefix = string.Empty;
    [ObservableProperty] string nameSuffix = string.Empty;
    [ObservableProperty] string nickname = string.Empty;

    // Organization
    [ObservableProperty] string company = string.Empty;
    [ObservableProperty] string jobTitle = string.Empty;
    [ObservableProperty] string department = string.Empty;

    // Primary contact info
    [ObservableProperty] string phoneNumber = string.Empty;
    [ObservableProperty] string emailAddress = string.Empty;

    // Address
    [ObservableProperty] string street = string.Empty;
    [ObservableProperty] string city = string.Empty;
    [ObservableProperty] string state = string.Empty;
    [ObservableProperty] string postalCode = string.Empty;
    [ObservableProperty] string country = string.Empty;

    // Other
    [ObservableProperty] string note = string.Empty;
    [ObservableProperty] string website = string.Empty;

    // Photo
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhotoImageSource))]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    byte[]? photoBytes;

    public ImageSource? PhotoImageSource => PhotoBytes is { Length: > 0 }
        ? ImageSource.FromStream(() => new MemoryStream(PhotoBytes))
        : null;

    public bool HasPhoto => PhotoBytes is { Length: > 0 };

    public string PageTitle => IsNew ? "Add Contact" : "Edit Contact";

    partial void OnContactIdChanged(string? value)
    {
        IsNew = string.IsNullOrEmpty(value);
        if (!IsNew)
            LoadContactCommand.Execute(null);
    }

    [RelayCommand]
    async Task LoadContact()
    {
        if (ContactId == null) return;

        try
        {
            IsBusy = true;
            var contact = await contactStore.GetById(ContactId);
            if (contact == null)
            {
                await dialogs.Alert("Error", "Contact not found", "OK");
                await navigator.GoBack();
                return;
            }

            GivenName = contact.GivenName ?? string.Empty;
            FamilyName = contact.FamilyName ?? string.Empty;
            MiddleName = contact.MiddleName ?? string.Empty;
            NamePrefix = contact.NamePrefix ?? string.Empty;
            NameSuffix = contact.NameSuffix ?? string.Empty;
            Nickname = contact.Nickname ?? string.Empty;
            Company = contact.Organization?.Company ?? string.Empty;
            JobTitle = contact.Organization?.Title ?? string.Empty;
            Department = contact.Organization?.Department ?? string.Empty;
            PhoneNumber = contact.Phones.FirstOrDefault()?.Number ?? string.Empty;
            EmailAddress = contact.Emails.FirstOrDefault()?.Address ?? string.Empty;
            Note = contact.Note ?? string.Empty;
            Website = contact.Websites.FirstOrDefault()?.Url ?? string.Empty;
            PhotoBytes = contact.Photo;

            var addr = contact.Addresses.FirstOrDefault();
            if (addr != null)
            {
                Street = addr.Street ?? string.Empty;
                City = addr.City ?? string.Empty;
                State = addr.State ?? string.Empty;
                PostalCode = addr.PostalCode ?? string.Empty;
                Country = addr.Country ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task Save()
    {
        if (string.IsNullOrWhiteSpace(GivenName) && string.IsNullOrWhiteSpace(FamilyName))
        {
            await dialogs.Alert("Validation", "Please enter at least a first or last name.", "OK");
            return;
        }

        try
        {
            IsBusy = true;

            var contact = new Contact
            {
                Id = ContactId,
                GivenName = NullIfEmpty(GivenName),
                FamilyName = NullIfEmpty(FamilyName),
                MiddleName = NullIfEmpty(MiddleName),
                NamePrefix = NullIfEmpty(NamePrefix),
                NameSuffix = NullIfEmpty(NameSuffix),
                Nickname = NullIfEmpty(Nickname),
                Note = NullIfEmpty(Note),
                Photo = PhotoBytes
            };

            if (!string.IsNullOrWhiteSpace(Company) || !string.IsNullOrWhiteSpace(JobTitle))
            {
                contact.Organization = new ContactOrganization(
                    NullIfEmpty(Company), NullIfEmpty(JobTitle), NullIfEmpty(Department));
            }

            if (!string.IsNullOrWhiteSpace(PhoneNumber))
                contact.Phones.Add(new ContactPhone(PhoneNumber, PhoneType.Mobile));

            if (!string.IsNullOrWhiteSpace(EmailAddress))
                contact.Emails.Add(new ContactEmail(EmailAddress, EmailType.Home));

            if (!string.IsNullOrWhiteSpace(Street) || !string.IsNullOrWhiteSpace(City))
            {
                contact.Addresses.Add(new ContactAddress(
                    NullIfEmpty(Street), NullIfEmpty(City), NullIfEmpty(State),
                    NullIfEmpty(PostalCode), NullIfEmpty(Country)));
            }

            if (!string.IsNullOrWhiteSpace(Website))
                contact.Websites.Add(new ContactWebsite(Website));

            if (IsNew)
                await contactStore.Create(contact);
            else
                await contactStore.Update(contact);

            await navigator.GoBack();
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    Task Cancel() => navigator.GoBack();

    [RelayCommand]
    async Task TakePhoto()
    {
        try
        {
            if (!mediaPicker.IsCaptureSupported)
            {
                await dialogs.Alert("Not Supported", "Camera is not available on this device.", "OK");
                return;
            }
            var photo = await mediaPicker.CapturePhotoAsync();
            if (photo != null)
                PhotoBytes = await ReadFileResultAsync(photo);
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
    }

    [RelayCommand]
    async Task PickPhoto()
    {
        try
        {
            var photo = (await mediaPicker.PickPhotosAsync())?.FirstOrDefault();
            if (photo != null)
                PhotoBytes = await ReadFileResultAsync(photo);
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
    }

    [RelayCommand]
    void RemovePhoto() => PhotoBytes = null;

    static async Task<byte[]> ReadFileResultAsync(FileResult fileResult)
    {
        await using var stream = await fileResult.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
