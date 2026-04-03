using ClientContactManager.Models;

namespace ClientContactManager.Services;

public interface IClientService
{
    Task<List<ClientIndexItemViewModel>> GetAllClientsAsync();
    Task<ClientFormViewModel?> GetClientFormViewModelAsync(int id);
    Task<(bool success, int clientId, string? error)> CreateClientAsync(ClientFormViewModel vm);
    Task<(bool success, string? error)> UpdateClientAsync(int id, ClientFormViewModel vm);
    Task<(bool success, ContactSummaryViewModel? contact, string? error)> LinkContactAsync(int clientId, int contactId);
    Task<(bool success, ContactSummaryViewModel? contact, string? error)> UnlinkContactAsync(int clientId, int contactId);
    Task<string> GenerateClientCodeAsync(string name);
}
