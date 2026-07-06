using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Contacts;
using Contact = Shiny.Contacts.Contact;

namespace Sample.Shared.Maui.Pages.Contacts;

[ShellMap<ContactListPage>("contacts")]
public partial class ContactListViewModel(
    IContactStore contactStore,
    INavigator navigator,
    IDialogs dialogs
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    string searchText = String.Empty;

    [ObservableProperty]
    bool isRefreshing;

    public List<Contact> Contacts
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    async Task LoadContacts()
    {
        try
        {
            var access = await contactStore.RequestAccess();
            if (access != AccessState.Available)
            {
                await dialogs.Alert("FAIL", $"Contact access not granted ({access})", "OK");
                return;
            }
            IsRefreshing = true;

            var search = SearchText.Trim();
            
            if (string.IsNullOrWhiteSpace(search))
            {
                Contacts = (await contactStore.GetAll()).ToList();
            }
            else
            {
                Contacts = contactStore
                    .Query()
                    .Where(c =>
                        c.GivenName!.Contains(search) ||
                        c.FamilyName!.Contains(search) ||
                        c.Phones.Any(p => p.Number.Contains(search)) ||
                        c.Emails.Any(e => e.Address.Contains(search))
                    )
                    .OrderBy(x => x.FamilyName)
                    .ThenBy(x => x.GivenName)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    Task Search() => LoadContacts();

    [RelayCommand]
    Task AddContact() => navigator.NavigateTo<ContactEditViewModel>();

    [RelayCommand]
    async Task ViewContact(Contact contact)
    {
        if (contact?.Id == null) return;
        
        await navigator.NavigateTo<ContactDetailViewModel>(vm => vm.ContactId = contact.Id);
    }

    [RelayCommand]
    async Task DeleteContact(Contact contact)
    {
        if (contact?.Id == null) return;

        var confirm = await dialogs.Confirm(
            "Delete Contact",
            $"Delete {contact.DisplayName}?",
            "Delete", "Cancel");

        if (!confirm) return;

        try
        {
            await contactStore.Delete(contact.Id);
            await this.LoadContacts();
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        // Auto-search as user types (debounced by UI)
        SearchCommand.Execute(null);
    }

    public void OnAppearing() => _ = this.LoadContacts();
    public void OnDisappearing() { }
}
