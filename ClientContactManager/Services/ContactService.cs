using ClientContactManager.Data;
using ClientContactManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientContactManager.Services;

public class ContactService : IContactService
{
    private readonly AppDbContext _db;

    public ContactService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ContactIndexItemViewModel>> GetAllContactsAsync()
    {
        return await _db.Contacts
            .OrderBy(c => c.Surname)
            .ThenBy(c => c.Name)
            .Select(c => new ContactIndexItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Surname = c.Surname,
                Email = c.Email,
                ClientCount = c.ClientContacts.Count()
            })
            .ToListAsync();
    }

    public async Task<ContactFormViewModel?> GetContactFormViewModelAsync(int id)
    {
        var contact = await _db.Contacts
            .Include(c => c.ClientContacts)
                .ThenInclude(cc => cc.Client)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contact == null) return null;

        var linkedClientIds = contact.ClientContacts.Select(cc => cc.ClientId).ToHashSet();

        var linkedClients = contact.ClientContacts
            .Select(cc => new ClientSummaryViewModel
            {
                Id = cc.Client.Id,
                Name = cc.Client.Name,
                ClientCode = cc.Client.ClientCode
            })
            .OrderBy(c => c.Name)
            .ToList();

        var availableClients = await _db.Clients
            .Where(c => !linkedClientIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => new ClientSummaryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                ClientCode = c.ClientCode
            })
            .ToListAsync();

        return new ContactFormViewModel
        {
            Id = contact.Id,
            Name = contact.Name,
            Surname = contact.Surname,
            Email = contact.Email,
            LinkedClients = linkedClients,
            AvailableClients = availableClients
        };
    }

    public async Task<(bool success, int contactId, string? error)> CreateContactAsync(ContactFormViewModel vm)
    {
        var emailTaken = await _db.Contacts.AnyAsync(c => c.Email == vm.Email);
        if (emailTaken) return (false, 0, "Email is already in use.");

        var contact = new Contact
        {
            Name = vm.Name,
            Surname = vm.Surname,
            Email = vm.Email
        };
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();
        return (true, contact.Id, null);
    }

    public async Task<(bool success, string? error)> UpdateContactAsync(int id, ContactFormViewModel vm)
    {
        var contact = await _db.Contacts.FindAsync(id);
        if (contact == null) return (false, "Contact not found.");

        var emailTaken = await _db.Contacts.AnyAsync(c => c.Email == vm.Email && c.Id != id);
        if (emailTaken) return (false, "Email is already in use.");

        contact.Name = vm.Name;
        contact.Surname = vm.Surname;
        contact.Email = vm.Email;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool success, ClientSummaryViewModel? client, string? error)> LinkClientAsync(int contactId, int clientId)
    {
        var alreadyLinked = await _db.ClientContacts
            .AnyAsync(cc => cc.ContactId == contactId && cc.ClientId == clientId);
        if (alreadyLinked) return (false, null, "Client is already linked.");

        var client = await _db.Clients.FindAsync(clientId);
        if (client == null) return (false, null, "Client not found.");

        _db.ClientContacts.Add(new ClientContact { ClientId = clientId, ContactId = contactId });
        await _db.SaveChangesAsync();

        return (true, new ClientSummaryViewModel
        {
            Id = client.Id,
            Name = client.Name,
            ClientCode = client.ClientCode
        }, null);
    }

    public async Task<(bool success, ClientSummaryViewModel? client, string? error)> UnlinkClientAsync(int contactId, int clientId)
    {
        var link = await _db.ClientContacts
            .Include(cc => cc.Client)
            .FirstOrDefaultAsync(cc => cc.ContactId == contactId && cc.ClientId == clientId);

        if (link == null) return (false, null, "Link not found.");

        var clientVm = new ClientSummaryViewModel
        {
            Id = link.Client.Id,
            Name = link.Client.Name,
            ClientCode = link.Client.ClientCode
        };

        _db.ClientContacts.Remove(link);
        await _db.SaveChangesAsync();
        return (true, clientVm, null);
    }
}
