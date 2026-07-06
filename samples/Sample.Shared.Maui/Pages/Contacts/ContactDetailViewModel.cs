using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Contacts;
using Contact = Shiny.Contacts.Contact;

namespace Sample.Shared.Maui.Pages.Contacts;

[ShellMap<ContactDetailPage>]
public partial class ContactDetailViewModel(
    IContactStore contactStore,
    INavigator navigator,
    IDialogs dialogs
) : ObservableObject
{
    [ShellProperty]
    [ObservableProperty]
    public partial string? ContactId { get; set; }

    [ObservableProperty]
    Contact? contact;

    [ObservableProperty]
    bool isLoading;

    partial void OnContactIdChanged(string? value)
    {
        if (value != null)
            LoadContactCommand.Execute(null);
    }

    [RelayCommand]
    async Task LoadContact()
    {
        if (ContactId == null) return;

        try
        {
            IsLoading = true;
            Contact = await contactStore.GetById(ContactId);
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    async Task EditContact()
    {
        if (ContactId == null) return;
        await navigator.NavigateTo<ContactEditViewModel>(vm => vm.ContactId = ContactId);
    }

    [RelayCommand]
    async Task DeleteContact()
    {
        if (ContactId == null || Contact == null) return;

        var confirm = await dialogs.Confirm(
            "Delete Contact",
            $"Delete {Contact.DisplayName}?",
            "Delete", "Cancel");

        if (!confirm) return;

        try
        {
            await contactStore.Delete(ContactId);
            await navigator.GoBack();
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
    }
}
