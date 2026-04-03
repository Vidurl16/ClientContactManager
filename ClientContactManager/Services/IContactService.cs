using ClientContactManager.Models;

namespace ClientContactManager.Services;

public interface IContactService
{
    Task<List<ContactIndexItemViewModel>> GetAllContactsAsync();
    Task<ContactFormViewModel?> GetContactFormViewModelAsync(int id);
    Task<(bool success, int contactId, string? error)> CreateContactAsync(ContactFormViewModel vm);
    Task<(bool success, string? error)> UpdateContactAsync(int id, ContactFormViewModel vm);
    Task<(bool success, ClientSummaryViewModel? client, string? error)> LinkClientAsync(int contactId, int clientId);
    Task<(bool success, ClientSummaryViewModel? client, string? error)> UnlinkClientAsync(int contactId, int clientId);
}
