using ClientContactManager.Data;
using ClientContactManager.Models;
using ClientContactManager.Services;
using Microsoft.EntityFrameworkCore;

namespace ClientContactManager.Tests;

public class ContactServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ── CreateContactAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateContact_PersistsContact()
    {
        // Arrange
        var db = CreateDb();
        var svc = new ContactService(db);
        var vm = new ContactFormViewModel { Name = "Jane", Surname = "Doe", Email = "jane@example.com" };

        // Act
        var (success, contactId, error) = await svc.CreateContactAsync(vm);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        var saved = await db.Contacts.FindAsync(contactId);
        Assert.NotNull(saved);
        Assert.Equal("jane@example.com", saved.Email);
    }

    [Fact]
    public async Task CreateContact_ReturnsError_WhenEmailAlreadyInUse()
    {
        // Arrange
        var db = CreateDb();
        db.Contacts.Add(new Contact { Name = "Existing", Surname = "User", Email = "taken@example.com" });
        await db.SaveChangesAsync();
        var svc = new ContactService(db);
        var vm = new ContactFormViewModel { Name = "New", Surname = "User", Email = "taken@example.com" };

        // Act
        var (success, contactId, error) = await svc.CreateContactAsync(vm);

        // Assert
        Assert.False(success);
        Assert.Equal("Email is already in use.", error);
    }

    // ── UpdateContactAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateContact_AllowsSameEmailForSameContact()
    {
        // Arrange
        var db = CreateDb();
        var contact = new Contact { Name = "Jane", Surname = "Doe", Email = "jane@example.com" };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        var svc = new ContactService(db);
        var vm = new ContactFormViewModel { Name = "Janet", Surname = "Doe", Email = "jane@example.com" };

        // Act
        var (success, error) = await svc.UpdateContactAsync(contact.Id, vm);

        // Assert
        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public async Task UpdateContact_ReturnsError_WhenEmailTakenByOtherContact()
    {
        // Arrange
        var db = CreateDb();
        db.Contacts.Add(new Contact { Name = "Other", Surname = "Person", Email = "other@example.com" });
        var contact = new Contact { Name = "Jane", Surname = "Doe", Email = "jane@example.com" };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        var svc = new ContactService(db);
        var vm = new ContactFormViewModel { Name = "Jane", Surname = "Doe", Email = "other@example.com" };

        // Act
        var (success, error) = await svc.UpdateContactAsync(contact.Id, vm);

        // Assert
        Assert.False(success);
        Assert.Equal("Email is already in use.", error);
    }

    // ── LinkClientAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task LinkClient_CreatesJunctionRow()
    {
        // Arrange
        var db = CreateDb();
        var client  = new Client  { Name = "Acme", ClientCode = "ACM001" };
        var contact = new Contact { Name = "Jane", Surname = "Doe", Email = "jane@example.com" };
        db.Clients.Add(client);
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        var svc = new ContactService(db);

        // Act
        var (success, vm, error) = await svc.LinkClientAsync(contact.Id, client.Id);

        // Assert
        Assert.True(success);
        Assert.True(await db.ClientContacts.AnyAsync(cc => cc.ClientId == client.Id && cc.ContactId == contact.Id));
    }

    [Fact]
    public async Task LinkClient_ReturnsError_WhenAlreadyLinked()
    {
        // Arrange
        var db = CreateDb();
        var client  = new Client  { Name = "Acme", ClientCode = "ACM001" };
        var contact = new Contact { Name = "Jane", Surname = "Doe", Email = "jane@example.com" };
        db.Clients.Add(client);
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        db.ClientContacts.Add(new ClientContact { ClientId = client.Id, ContactId = contact.Id });
        await db.SaveChangesAsync();
        var svc = new ContactService(db);

        // Act
        var (success, vm, error) = await svc.LinkClientAsync(contact.Id, client.Id);

        // Assert
        Assert.False(success);
        Assert.Equal("Client is already linked.", error);
    }

    // ── UnlinkClientAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UnlinkClient_RemovesJunctionRow()
    {
        // Arrange
        var db = CreateDb();
        var client  = new Client  { Name = "Acme", ClientCode = "ACM001" };
        var contact = new Contact { Name = "Jane", Surname = "Doe", Email = "jane@example.com" };
        db.Clients.Add(client);
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        db.ClientContacts.Add(new ClientContact { ClientId = client.Id, ContactId = contact.Id });
        await db.SaveChangesAsync();
        var svc = new ContactService(db);

        // Act
        var (success, vm, error) = await svc.UnlinkClientAsync(contact.Id, client.Id);

        // Assert
        Assert.True(success);
        Assert.False(await db.ClientContacts.AnyAsync(cc => cc.ClientId == client.Id && cc.ContactId == contact.Id));
    }
}
