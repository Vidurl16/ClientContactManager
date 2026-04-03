using ClientContactManager.Data;
using ClientContactManager.Models;
using ClientContactManager.Services;
using Microsoft.EntityFrameworkCore;

namespace ClientContactManager.Tests;

public class ClientServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ── GenerateClientCodeAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GenerateClientCode_UsesFirstThreeCharsUppercased()
    {
        // Arrange
        var db = CreateDb();
        var svc = new ClientService(db);

        // Act
        var code = await svc.GenerateClientCodeAsync("Acme Corp");

        // Assert
        Assert.Equal("ACM001", code);
    }

    [Fact]
    public async Task GenerateClientCode_PadsShortNameWithLetters()
    {
        // Arrange
        var db = CreateDb();
        var svc = new ClientService(db);

        // Act
        var code = await svc.GenerateClientCodeAsync("Jo");

        // Assert
        Assert.Equal("JOA001", code);
    }

    [Fact]
    public async Task GenerateClientCode_PadsSingleCharName()
    {
        // Arrange
        var db = CreateDb();
        var svc = new ClientService(db);

        // Act
        var code = await svc.GenerateClientCodeAsync("A");

        // Assert — second pad char is 'B' (padChar starts at 0 for the first pad position)
        Assert.Equal("AAB001", code);
    }

    [Fact]
    public async Task GenerateClientCode_IncrementsSequenceWhenPrefixTaken()
    {
        // Arrange
        var db = CreateDb();
        db.Clients.Add(new Client { Name = "Acme", ClientCode = "ACM001" });
        await db.SaveChangesAsync();
        var svc = new ClientService(db);

        // Act
        var code = await svc.GenerateClientCodeAsync("Acme Corp");

        // Assert
        Assert.Equal("ACM002", code);
    }

    // ── CreateClientAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateClient_PersistsClientWithGeneratedCode()
    {
        // Arrange
        var db = CreateDb();
        var svc = new ClientService(db);
        var vm = new ClientFormViewModel { Name = "Beta Ltd" };

        // Act
        var (success, clientId, error) = await svc.CreateClientAsync(vm);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        var saved = await db.Clients.FindAsync(clientId);
        Assert.NotNull(saved);
        Assert.Equal("BET001", saved.ClientCode);
    }

    // ── LinkContactAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task LinkContact_CreatesJunctionRow()
    {
        // Arrange
        var db = CreateDb();
        var client  = new Client  { Name = "Acme", ClientCode = "ACM001" };
        var contact = new Contact { Name = "Jane", Surname = "Doe", Email = "jane@example.com" };
        db.Clients.Add(client);
        db.Contacts.Add(contact);
        await db.SaveChangesAsync();
        var svc = new ClientService(db);

        // Act
        var (success, vm, error) = await svc.LinkContactAsync(client.Id, contact.Id);

        // Assert
        Assert.True(success);
        Assert.Null(error);
        Assert.True(await db.ClientContacts.AnyAsync(cc => cc.ClientId == client.Id && cc.ContactId == contact.Id));
    }

    [Fact]
    public async Task LinkContact_ReturnsError_WhenAlreadyLinked()
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
        var svc = new ClientService(db);

        // Act
        var (success, vm, error) = await svc.LinkContactAsync(client.Id, contact.Id);

        // Assert
        Assert.False(success);
        Assert.Equal("Contact is already linked.", error);
    }

    // ── UnlinkContactAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UnlinkContact_RemovesJunctionRow()
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
        var svc = new ClientService(db);

        // Act
        var (success, vm, error) = await svc.UnlinkContactAsync(client.Id, contact.Id);

        // Assert
        Assert.True(success);
        Assert.False(await db.ClientContacts.AnyAsync(cc => cc.ClientId == client.Id && cc.ContactId == contact.Id));
    }

    [Fact]
    public async Task UnlinkContact_ReturnsError_WhenLinkNotFound()
    {
        // Arrange
        var db = CreateDb();
        var svc = new ClientService(db);

        // Act
        var (success, vm, error) = await svc.UnlinkContactAsync(99, 99);

        // Assert
        Assert.False(success);
        Assert.Equal("Link not found.", error);
    }
}
