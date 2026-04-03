using ClientContactManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientContactManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ClientContact> ClientContacts => Set<ClientContact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientContact>(entity =>
        {
            entity.HasKey(cc => new { cc.ClientId, cc.ContactId });

            entity.HasIndex(cc => cc.ClientId);
            entity.HasIndex(cc => cc.ContactId);

            entity.HasOne(cc => cc.Client)
                .WithMany(c => c.ClientContacts)
                .HasForeignKey(cc => cc.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cc => cc.Contact)
                .WithMany(c => c.ClientContacts)
                .HasForeignKey(cc => cc.ContactId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasIndex(c => c.ClientCode).IsUnique();
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasIndex(c => c.Email).IsUnique();
        });
    }
}
