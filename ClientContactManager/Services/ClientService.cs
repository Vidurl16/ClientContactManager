using ClientContactManager.Data;
using ClientContactManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientContactManager.Services;

public class ClientService : IClientService
{
    private readonly AppDbContext _db;

    public ClientService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ClientIndexItemViewModel>> GetAllClientsAsync()
    {
        return await _db.Clients
            .OrderBy(c => c.Name)
            .Select(c => new ClientIndexItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                ClientCode = c.ClientCode,
                ContactCount = c.ClientContacts.Count()
            })
            .ToListAsync();
    }

    public async Task<ClientFormViewModel?> GetClientFormViewModelAsync(int id)
    {
        var client = await _db.Clients
            .Include(c => c.ClientContacts)
                .ThenInclude(cc => cc.Contact)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null) return null;

        var linkedContactIds = client.ClientContacts.Select(cc => cc.ContactId).ToHashSet();

        var linkedContacts = client.ClientContacts
            .Select(cc => new ContactSummaryViewModel
            {
                Id = cc.Contact.Id,
                Name = cc.Contact.Name,
                Surname = cc.Contact.Surname,
                Email = cc.Contact.Email
            })
            .OrderBy(c => c.Surname)
            .ThenBy(c => c.Name)
            .ToList();

        var availableContacts = await _db.Contacts
            .Where(c => !linkedContactIds.Contains(c.Id))
            .OrderBy(c => c.Surname)
            .ThenBy(c => c.Name)
            .Select(c => new ContactSummaryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Surname = c.Surname,
                Email = c.Email
            })
            .ToListAsync();

        return new ClientFormViewModel
        {
            Id = client.Id,
            Name = client.Name,
            ClientCode = client.ClientCode,
            LinkedContacts = linkedContacts,
            AvailableContacts = availableContacts
        };
    }

    public async Task<(bool success, int clientId, string? error)> CreateClientAsync(ClientFormViewModel vm)
    {
        var code = await GenerateClientCodeAsync(vm.Name);

        var client = new Client { Name = vm.Name, ClientCode = code };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return (true, client.Id, null);
    }

    public async Task<(bool success, string? error)> UpdateClientAsync(int id, ClientFormViewModel vm)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return (false, "Client not found.");

        client.Name = vm.Name;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool success, ContactSummaryViewModel? contact, string? error)> LinkContactAsync(int clientId, int contactId)
    {
        var alreadyLinked = await _db.ClientContacts
            .AnyAsync(cc => cc.ClientId == clientId && cc.ContactId == contactId);
        if (alreadyLinked) return (false, null, "Contact is already linked.");

        var contact = await _db.Contacts.FindAsync(contactId);
        if (contact == null) return (false, null, "Contact not found.");

        _db.ClientContacts.Add(new ClientContact { ClientId = clientId, ContactId = contactId });
        await _db.SaveChangesAsync();

        return (true, new ContactSummaryViewModel
        {
            Id = contact.Id,
            Name = contact.Name,
            Surname = contact.Surname,
            Email = contact.Email
        }, null);
    }

    public async Task<(bool success, ContactSummaryViewModel? contact, string? error)> UnlinkContactAsync(int clientId, int contactId)
    {
        var link = await _db.ClientContacts
            .Include(cc => cc.Contact)
            .FirstOrDefaultAsync(cc => cc.ClientId == clientId && cc.ContactId == contactId);

        if (link == null) return (false, null, "Link not found.");

        var contactVm = new ContactSummaryViewModel
        {
            Id = link.Contact.Id,
            Name = link.Contact.Name,
            Surname = link.Contact.Surname,
            Email = link.Contact.Email
        };

        _db.ClientContacts.Remove(link);
        await _db.SaveChangesAsync();
        return (true, contactVm, null);
    }

    public async Task<string> GenerateClientCodeAsync(string name)
    {
        var upper = name.ToUpper();
        var prefix = new char[3];
        int padChar = 0;
        for (int i = 0; i < 3; i++)
        {
            if (i < upper.Length)
                prefix[i] = upper[i];
            else
                prefix[i] = (char)('A' + padChar++);
        }
        var prefixStr = new string(prefix);

        for (int num = 1; num <= 999; num++)
        {
            var code = $"{prefixStr}{num:D3}";
            if (!await _db.Clients.AnyAsync(c => c.ClientCode == code))
                return code;
        }
        throw new InvalidOperationException($"Unable to generate a unique client code for prefix '{prefixStr}'.");
    }
}
