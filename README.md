# Client Contact Manager

A full-stack web application for managing clients and their contacts in a many-to-many relationship. Built with C# ASP.NET Core MVC, Entity Framework Core, MySQL, and vanilla JavaScript.

---

## Tech Stack

| Layer    | Technology                                    |
|----------|-----------------------------------------------|
| Backend  | C# / ASP.NET Core MVC (.NET 8)                |
| ORM      | Entity Framework Core 8                       |
| Database | MySQL 8                                       |
| Frontend | Razor Views, Bootstrap 5, vanilla JavaScript  |

---

## Getting Started

### Option A — Docker (recommended, no local MySQL needed)

**Prerequisites:** [Docker Desktop](https://www.docker.com/products/docker-desktop/)

```bash
git clone https://github.com/Vidurl16/ClientContactManager.git
cd ClientContactManager
docker compose up --build
```

Open `http://localhost:8080`. The database is created and migrated automatically — nothing else to install or configure.

---

### Option B — Local .NET + MySQL

**Prerequisites:**

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| MySQL | 8.0+ | https://dev.mysql.com/downloads/mysql/ |

**1 — Create the database and user in MySQL**

```sql
CREATE DATABASE client_contact_manager;
CREATE USER 'ccm_user'@'localhost' IDENTIFIED BY 'ccm_password123';
GRANT ALL PRIVILEGES ON client_contact_manager.* TO 'ccm_user'@'localhost';
FLUSH PRIVILEGES;
```

**2 — Confirm the connection string** in `ClientContactManager/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;database=client_contact_manager;user=ccm_user;password=ccm_password123;"
}
```

**3 — Run**

```bash
cd ClientContactManager
dotnet run
```

Open `https://localhost:<port>` in your browser. **Migrations apply automatically on startup.**

---

## Running the Tests

```bash
dotnet test ClientContactManager.Tests/
```

16 unit tests covering `ClientService` and `ContactService` — code generation, email uniqueness, link/unlink logic.

---

## Architecture

The app uses **MVC with a Service layer** — controllers are thin HTTP handlers; all business logic lives in injectable services.

```
Browser Request
      │
      ▼
  Controller          — routes HTTP, validates ModelState, returns JSON or View
      │
      ▼
  Service             — business logic (IClientService / IContactService)
      │
      ▼
  AppDbContext        — EF Core translates LINQ to SQL
      │
      ▼
  MySQL Database
```

### Folder Structure

```
Controllers/    HTTP actions — no business logic
Services/       Interfaces + implementations — all business rules
Models/         EF Core entities + ViewModels
Data/           AppDbContext — relationships, indexes, cascade config
Views/          Razor templates (shared partials in Views/Shared/)
wwwroot/js/     ajax.js, validation.js, clients.js, contacts.js, tabs.js
Migrations/     EF Core auto-generated migration files
```

---

## Database Schema

### Clients
| Column     | Type         | Constraints            |
|------------|--------------|------------------------|
| Id         | int          | PK, auto-increment     |
| Name       | varchar(200) | NOT NULL               |
| ClientCode | varchar(6)   | NOT NULL, UNIQUE       |

### Contacts
| Column  | Type         | Constraints            |
|---------|--------------|------------------------|
| Id      | int          | PK, auto-increment     |
| Name    | varchar(100) | NOT NULL               |
| Surname | varchar(100) | NOT NULL               |
| Email   | varchar(150) | NOT NULL, UNIQUE       |

### ClientContacts (junction table)
| Column    | Type | Constraints                                     |
|-----------|------|-------------------------------------------------|
| ClientId  | int  | Composite PK, FK → Clients.Id (CASCADE DELETE)  |
| ContactId | int  | Composite PK, FK → Contacts.Id (CASCADE DELETE) |

Indexes on both FK columns. Cascade delete on both sides.

---

## OOP Principles

### Encapsulation — `Models/Client.cs`
```csharp
public string ClientCode { get; init; } = string.Empty;
```
`init` makes `ClientCode` write-once — set at creation, immutable thereafter. Raw DB entities are never exposed to views; they're always projected into ViewModels first.

---

### Abstraction — `Services/IClientService.cs`
```csharp
public interface IClientService
{
    Task<(bool success, int clientId, string? error)> CreateClientAsync(ClientFormViewModel vm);
    Task<string> GenerateClientCodeAsync(string name);
    // ...
}
```
Controllers depend entirely on this interface. They have no knowledge of EF Core, MySQL, or how client codes are generated.

---

### Inheritance — `Controllers/ClientsController.cs`
```csharp
public class ClientsController : Controller
{
    public async Task<IActionResult> Index()
    {
        var clients = await _clientService.GetAllClientsAsync();
        return View(clients); // View(), Json(), NotFound() all inherited
    }
}
```
Both controllers inherit ASP.NET Core's `Controller` base class, gaining routing helpers, `ModelState`, `HttpContext`, and response factory methods.

---

### Polymorphism — `Program.cs` + `Controllers/ClientsController.cs`
```csharp
// Program.cs — register the concrete type against the interface
builder.Services.AddScoped<IClientService, ClientService>();

// Controller — depends only on the interface
public ClientsController(IClientService clientService)
{
    _clientService = clientService; // runtime: ClientService injected here
}
```
The controller holds an `IClientService` reference. Swapping in a different implementation (e.g. a mock or caching layer) requires only a one-line change in `Program.cs` — the controller is unchanged.

---

## SOLID Principles

### S — Single Responsibility
`ClientsController` only handles HTTP. `ClientService` only contains business logic. Each has exactly one reason to change.

```csharp
// Controller — shapes the response, nothing more
[HttpPost] public async Task<IActionResult> UnlinkContact([FromForm] int clientId, [FromForm] int contactId)
{
    var (success, contact, error) = await _clientService.UnlinkContactAsync(clientId, contactId);
    return success ? Json(new { success = true, contact }) : Json(new { success = false, message = error });
}

// Service — owns the business rule
public async Task<(bool success, ContactSummaryViewModel? contact, string? error)> UnlinkContactAsync(int clientId, int contactId)
{
    var link = await _db.ClientContacts.Include(cc => cc.Contact)
        .FirstOrDefaultAsync(cc => cc.ClientId == clientId && cc.ContactId == contactId);
    if (link == null) return (false, null, "Link not found.");
    _db.ClientContacts.Remove(link);
    await _db.SaveChangesAsync();
    return (true, new ContactSummaryViewModel { ... }, null);
}
```

---

### O — Open/Closed
The service interfaces form a stable contract. New behaviour (caching, auditing, a test double) is added by implementing the interface — no existing controller or service is modified.

```csharp
// New implementation — open for extension
public class CachedClientService : IClientService { ... }

// Swap in Program.cs — closed for modification
builder.Services.AddScoped<IClientService, CachedClientService>();
```

---

### L — Liskov Substitution
`ClientService` implements every method on `IClientService` correctly — no `NotImplementedException`, no narrowed return types, no violated postconditions. Any code holding an `IClientService` reference receives a `ClientService` and all calls behave as the interface promises.

---

### I — Interface Segregation
`IClientService` and `IContactService` are separate, focused interfaces. Neither contains methods unrelated to its entity. `ContactsController` depends only on `IContactService` — it never sees client-specific operations.

```csharp
// IClientService.cs — client operations only
Task<string> GenerateClientCodeAsync(string name);
Task<(bool success, ContactSummaryViewModel? contact, string? error)> LinkContactAsync(int clientId, int contactId);

// IContactService.cs — contact operations only
Task<(bool success, ClientSummaryViewModel? client, string? error)> LinkClientAsync(int contactId, int clientId);
```

---

### D — Dependency Inversion
High-level modules (controllers) depend on abstractions (interfaces), not concretions. The DI container wires the real implementations at startup.

```csharp
// Program.cs — concretions registered once
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IContactService, ContactService>();

// ClientsController.cs — depends on abstraction only
public ClientsController(IClientService clientService) => _clientService = clientService;
```

---

## AJAX & Client Interaction

All link/unlink operations and form saves happen via `fetch()` — no page reloads.

**Unlink flow:**
1. User clicks **Unlink** → `clients.js` intercepts via event delegation
2. `CCM.ajax.post('/Clients/UnlinkContact', formData)` — POST with CSRF token
3. `ClientsController.UnlinkContact` → `ClientService.UnlinkContactAsync` → EF Core deletes the `ClientContacts` row
4. Controller returns `Json(new { success = true, contact })`
5. JavaScript removes the `<tr>` from the DOM, restores the contact to the dropdown

```javascript
// clients.js — delegated unlink handler
tbody.addEventListener('click', async function (e) {
    const btn = e.target.closest('.unlink-btn');
    if (!btn) return;

    const fd = new FormData();
    fd.append('clientId',  cfg.clientId);
    fd.append('contactId', btn.dataset.contactId);
    fd.append('__RequestVerificationToken', v.getToken());

    const data = await ax.post(cfg.unlinkUrl, fd);
    if (data.success) {
        tbody.querySelector(`tr[data-contact-id="${btn.dataset.contactId}"]`)?.remove();
        addToDropdown(data.contact.id, `${data.contact.surname} ${data.contact.name}`);
        toggleEmptyState();
    }
});
```

---

## Client Code Generation

Each client receives an auto-generated unique code on creation:

**Rule:** First 3 characters of the name (uppercased, padded with A/B/C if shorter), then a 3-digit sequence number that increments until a unique DB value is found.

```csharp
// ClientService.cs
public async Task<string> GenerateClientCodeAsync(string name)
{
    var upper = name.ToUpper();
    var prefix = new char[3];
    int padChar = 0;
    for (int i = 0; i < 3; i++)
        prefix[i] = i < upper.Length ? upper[i] : (char)('A' + padChar++);

    var prefixStr = new string(prefix);
    for (int num = 1; num <= 999; num++)
    {
        var code = $"{prefixStr}{num:D3}";
        if (!await _db.Clients.AnyAsync(c => c.ClientCode == code))
            return code;
    }
    throw new InvalidOperationException($"No unique code available for prefix '{prefixStr}'.");
}
```

| Input Name   | Code     |
|--------------|----------|
| Acme Corp    | `ACM001` |
| Jo           | `JOA001` |
| A            | `AAA001` |
| Acme Corp ×2 | `ACM002` |

---

## Validation

**Server-side** (cannot be bypassed):

```csharp
// ContactService.cs — email uniqueness enforced in the service layer
var emailTaken = await _db.Contacts.AnyAsync(c => c.Email == vm.Email);
if (emailTaken) return (false, 0, "Email is already in use.");
```

`ModelState.IsValid` is also checked in every POST action before the service is called, catching `[Required]`, `[MaxLength]`, and `[EmailAddress]` annotation violations.

**Client-side** (instant feedback via `CCM.validation`):

```javascript
// validation.js
function validateEmail(input) {
    const val = input.value.trim();
    if (!val) { _setValidity(input, false); return false; }
    const ok = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val);
    _setValidity(input, ok);
    return ok;
}

// contacts.js — checked on submit and on every input event
const emailValid = v.validateEmail(emailInput);
if (!nameValid || !surnameValid || !emailValid) return;
```

Email uniqueness is also enforced at the database level via a unique index on `Contacts.Email`.
