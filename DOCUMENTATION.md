# Client Contact Manager — Project Documentation

## 1. Project Overview

**Client Contact Manager** is a web application that lets a business manage its clients and their associated contacts. Users can:

- Create and edit **clients** (companies or individuals), each automatically assigned a unique client code
- Create and edit **contacts** (people with a name, surname, and email address)
- **Link and unlink** clients and contacts in a many-to-many relationship — one client can have many contacts, and one contact can belong to many clients
- Perform all linking/unlinking actions **without page reloads** via AJAX

### Tech Stack

| Layer         | Technology                              |
|---------------|-----------------------------------------|
| Backend       | C# / ASP.NET Core MVC (.NET 8)          |
| ORM           | Entity Framework Core 8                 |
| Database      | MySQL 8                                 |
| Frontend      | Razor Views, Bootstrap 5, vanilla JS    |
| JS modules    | `ajax.js`, `validation.js`, `clients.js`, `contacts.js`, `tabs.js` |

### How to Run Locally

1. Ensure MySQL 8 is running
2. Set the correct connection string in `appsettings.json` under `"DefaultConnection"`
3. Open a terminal in the repo root:

```bash
cd ClientContactManager
dotnet ef database update   # applies EF Core migrations → creates tables
dotnet run                  # starts the dev server
```

4. Open `https://localhost:<port>` in your browser (port shown in terminal output)

---

## 2. Architecture

### MVC + Service Layer

The app uses the **Model-View-Controller** pattern with an extra **Service layer**:

- **Controllers** (`Controllers/`) — receive HTTP requests, check model validity, call services, return JSON or Razor Views. They contain **no business logic**.
- **Services** (`Services/`) — contain all business rules (code generation, email uniqueness, link management). Controllers talk to services only via interfaces.
- **Views** (`Views/`) — Razor `.cshtml` templates that render HTML. Shared partials live in `Views/Shared/`.
- **Models** (`Models/`) — EF Core entity classes (`Client`, `Contact`, `ClientContact`) plus ViewModels used to pass data between controllers and views.
- **Data** (`Data/AppDbContext.cs`) — EF Core `DbContext`; configures table relationships, keys, and indexes.

### Folder Structure

```
ClientContactManager/
├── Controllers/
│   ├── ClientsController.cs      HTTP actions for Clients (Index, Create, Edit, LinkContact, UnlinkContact)
│   ├── ContactsController.cs     HTTP actions for Contacts (Index, Create, Edit, LinkClient, UnlinkClient)
│   └── HomeController.cs         Default landing page redirect
├── Data/
│   └── AppDbContext.cs           EF Core DbContext — DB sets + relationship config
├── Migrations/                   Auto-generated EF Core migration files
├── Models/
│   ├── Client.cs                 Entity: Id, Name, ClientCode, ClientContacts nav
│   ├── Contact.cs                Entity: Id, Name, Surname, Email, ClientContacts nav
│   ├── ClientContact.cs          Junction entity: ClientId + ContactId (composite PK)
│   ├── ClientFormViewModel.cs    ViewModel for the client create/edit form
│   ├── ContactFormViewModel.cs   ViewModel for the contact create/edit form
│   ├── ClientIndexItemViewModel.cs  ViewModel for the clients list row
│   ├── ContactIndexItemViewModel.cs ViewModel for the contacts list row
│   ├── ClientSummaryViewModel.cs    Lightweight client summary (used in contact form)
│   └── ContactSummaryViewModel.cs   Lightweight contact summary (used in client form)
├── Services/
│   ├── IClientService.cs         Interface — client business operations
│   ├── ClientService.cs          Implementation of IClientService
│   ├── IContactService.cs        Interface — contact business operations
│   └── ContactService.cs         Implementation of IContactService
├── Views/
│   ├── Clients/
│   │   ├── Index.cshtml          Clients list page
│   │   └── CreateEdit.cshtml     Shared create/edit form (tabbed: General + Contacts)
│   ├── Contacts/
│   │   ├── Index.cshtml          Contacts list page
│   │   └── CreateEdit.cshtml     Shared create/edit form (tabbed: General + Clients)
│   └── Shared/
│       ├── _Layout.cshtml        Master layout (nav, footer)
│       ├── _Table.cshtml         Reusable table partial
│       ├── _FormTabs.cshtml      Reusable tabbed form partial
│       └── _ValidationSummary.cshtml  Alert container partial
├── wwwroot/
│   ├── css/app.css               Custom application styles
│   └── js/
│       ├── ajax.js               CCM.ajax — shared fetch() POST helper
│       ├── validation.js         CCM.validation — form validation helpers
│       ├── clients.js            Client form logic (save, link, unlink)
│       ├── contacts.js           Contact form logic (save, link, unlink)
│       └── tabs.js               Tab switching logic
├── appsettings.json              DB connection string + logging config
└── Program.cs                    App bootstrap, DI registration, middleware pipeline
```

### Request Flow

```
Browser Request
      │
      ▼
  Controller          (ClientsController / ContactsController)
  - Validates ModelState
  - Calls service method
  - Returns View() or Json()
      │
      ▼
  Service             (ClientService / ContactService)
  - Business logic (code generation, uniqueness checks, link management)
  - Uses AppDbContext via constructor injection
      │
      ▼
  AppDbContext        (Entity Framework Core)
  - Translates LINQ queries to SQL
  - Manages change tracking and SaveChangesAsync()
      │
      ▼
  MySQL Database
  - Tables: Clients, Contacts, ClientContacts
```

---

## 3. Database Schema

### Clients

| Column     | Type         | Constraints              |
|------------|--------------|--------------------------|
| Id         | int          | PRIMARY KEY, auto-inc    |
| Name       | varchar(200) | NOT NULL                 |
| ClientCode | varchar(6)   | NOT NULL, UNIQUE INDEX   |

### Contacts

| Column  | Type         | Constraints              |
|---------|--------------|--------------------------|
| Id      | int          | PRIMARY KEY, auto-inc    |
| Name    | varchar(100) | NOT NULL                 |
| Surname | varchar(100) | NOT NULL                 |
| Email   | varchar(150) | NOT NULL, UNIQUE INDEX   |

### ClientContacts (junction table)

| Column    | Type | Constraints                                       |
|-----------|------|---------------------------------------------------|
| ClientId  | int  | Composite PK, FK → Clients.Id (CASCADE DELETE)    |
| ContactId | int  | Composite PK, FK → Contacts.Id (CASCADE DELETE)   |

Indexes: individual indexes on `ClientId` and `ContactId` for query performance.

> [SCREENSHOT PLACEHOLDER: MySQL schema diagram]

---

## 4. OOP Principles — Where in the code

### Encapsulation

**File:** `ClientContactManager/Models/Client.cs`

```csharp
public class Client
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(6)]
    public string ClientCode { get; init; } = string.Empty;   // init-only after creation

    public ICollection<ClientContact> ClientContacts { get; set; } = new List<ClientContact>();
}
```

**Why:** `ClientCode` uses `init` — it can only be set during object creation, not mutated later. The internal navigation collection (`ClientContacts`) is typed as the interface `ICollection<T>`, not the concrete `List<T>`, hiding the implementation. Internal data (DB state) is never exposed raw — it's always projected into a ViewModel before reaching the view.

---

### Abstraction

**File:** `ClientContactManager/Services/IClientService.cs`

```csharp
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
```

**Why:** The interface defines *what* operations exist without exposing *how* they work. `ClientsController` only knows about `IClientService` — it has no idea EF Core or MySQL exist. This is abstraction: hiding complexity behind a named contract.

---

### Inheritance

**File:** `ClientContactManager/Controllers/ClientsController.cs`

```csharp
public class ClientsController : Controller
{
    // inherits View(), Json(), NotFound(), RedirectToAction(), ModelState, etc.
    public async Task<IActionResult> Index()
    {
        var clients = await _clientService.GetAllClientsAsync();
        return View(clients);   // View() is inherited from Controller base class
    }
}
```

**Why:** `ClientsController` inherits from ASP.NET Core's built-in `Controller` base class, gaining all the helper methods (`View()`, `Json()`, `NotFound()`, `RedirectToAction()`, access to `ModelState` and `HttpContext`) without reimplementing them. The same applies to `ContactsController`.

---

### Polymorphism

**File:** `ClientContactManager/Program.cs` + `Controllers/ClientsController.cs`

```csharp
// Program.cs — registration
builder.Services.AddScoped<IClientService, ClientService>();

// ClientsController.cs — usage via interface
public class ClientsController : Controller
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;   // runtime: actually a ClientService instance
    }
}
```

**Why:** The controller holds a reference typed as `IClientService`. At runtime the DI container injects a `ClientService` object. If a second implementation (e.g., `MockClientService` for testing) were registered instead, the controller would work identically — it behaves differently based on which concrete type is injected. This is runtime (subtype) polymorphism.

---

## 5. SOLID Principles — Where in the code

### S — Single Responsibility

**`ClientsController.cs`** — only routes HTTP requests and shapes responses:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UnlinkContact([FromForm] int clientId, [FromForm] int contactId)
{
    var (success, contact, error) = await _clientService.UnlinkContactAsync(clientId, contactId);
    if (!success)
        return Json(new { success = false, message = error });

    return Json(new { success = true, contact });
}
```

**`ClientService.cs`** — only contains business logic (no HTTP concerns):

```csharp
public async Task<(bool success, ContactSummaryViewModel? contact, string? error)> UnlinkContactAsync(int clientId, int contactId)
{
    var link = await _db.ClientContacts
        .Include(cc => cc.Contact)
        .FirstOrDefaultAsync(cc => cc.ClientId == clientId && cc.ContactId == contactId);

    if (link == null) return (false, null, "Link not found.");

    _db.ClientContacts.Remove(link);
    await _db.SaveChangesAsync();
    return (true, new ContactSummaryViewModel { ... }, null);
}
```

Each class has one reason to change: the controller changes if HTTP behaviour changes; the service changes if business rules change.

---

### O — Open/Closed

**Files:** `IClientService.cs`, `IContactService.cs`

The interfaces define a stable contract. New behaviour (e.g., a caching layer, an audit-logging service, a test double) can be added by creating a new class that implements the interface — **without modifying** `ClientsController` or any existing code.

```csharp
// A new implementation can be added without touching the controller:
public class CachedClientService : IClientService { ... }

// Program.cs — swap in the new implementation:
builder.Services.AddScoped<IClientService, CachedClientService>();
```

---

### L — Liskov Substitution

**File:** `ClientContactManager/Services/ClientService.cs`

`ClientService` fully satisfies every method declared in `IClientService`. Any code that holds an `IClientService` reference can receive a `ClientService` instance and all calls will behave correctly — no method throws `NotImplementedException`, no preconditions are strengthened, no return types are narrowed. The interface contract is honoured completely.

```csharp
public class ClientService : IClientService
{
    // Every method in IClientService is implemented correctly:
    public async Task<List<ClientIndexItemViewModel>> GetAllClientsAsync() { ... }
    public async Task<ClientFormViewModel?> GetClientFormViewModelAsync(int id) { ... }
    public async Task<(bool success, int clientId, string? error)> CreateClientAsync(ClientFormViewModel vm) { ... }
    // ... all 7 methods
}
```

---

### I — Interface Segregation

**Files:** `IClientService.cs` vs `IContactService.cs`

Each interface is focused on one entity. `IContactService` has no client methods; `IClientService` has no contact methods. Neither interface forces implementors to provide methods they don't need.

```csharp
// IClientService.cs — client operations only
public interface IClientService
{
    Task<string> GenerateClientCodeAsync(string name);
    Task<(bool success, ContactSummaryViewModel? contact, string? error)> LinkContactAsync(int clientId, int contactId);
    // ...
}

// IContactService.cs — contact operations only
public interface IContactService
{
    Task<(bool success, ClientSummaryViewModel? client, string? error)> LinkClientAsync(int contactId, int clientId);
    // ...
}
```

---

### D — Dependency Inversion

**File:** `ClientContactManager/Program.cs`

High-level modules (controllers) depend on abstractions (interfaces), not concretions. The DI container wires everything up:

```csharp
// Program.cs — register abstractions → concretions
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 45))));

builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IContactService, ContactService>();
```

**File:** `ClientContactManager/Controllers/ClientsController.cs`

```csharp
public class ClientsController : Controller
{
    private readonly IClientService _clientService;   // ← depends on abstraction

    public ClientsController(IClientService clientService)   // ← injected by DI
    {
        _clientService = clientService;
    }
}
```

The controller never calls `new ClientService()`. The dependency flows in through the constructor — high-level policy is decoupled from low-level detail.

---

## 6. AJAX — How it works in this project

### What is AJAX?

AJAX (Asynchronous JavaScript and XML) means the browser sends an HTTP request **in the background**, without navigating to a new page. When the server responds, JavaScript reads the data and updates only the relevant part of the page. The user never sees a full reload.

### Full Flow: Unlink Contact

```
1. User clicks "Unlink" button next to a contact in the Contacts tab
        │
        ▼
2. clients.js intercepts the click (event delegation on #contacts-tbody)
   Builds a FormData with clientId, contactId, CSRF token
        │
        ▼
3. CCM.ajax.post('/Clients/UnlinkContact', formData)
   — fetch() sends a POST request in the background
        │
        ▼
4. ClientsController.UnlinkContact(clientId, contactId)
   — calls _clientService.UnlinkContactAsync()
        │
        ▼
5. ClientService queries DB, removes ClientContacts row, SaveChangesAsync()
        │
        ▼
6. Controller returns: Json(new { success = true, contact })
        │
        ▼
7. clients.js receives JSON:
   - Removes the <tr> for that contact from the linked-contacts table
   - Adds the contact back to the "available contacts" dropdown
   - Calls toggleEmptyState() to show/hide the empty-state message
```

### Code Snippet — `clients.js` (unlink handler)

```javascript
const tbody = document.getElementById('contacts-tbody');
if (tbody) {
  tbody.addEventListener('click', async function (e) {
    const btn = e.target.closest('.unlink-btn');
    if (!btn) return;

    const contactId = btn.dataset.contactId;
    btn.disabled = true;

    const fd = new FormData();
    fd.append('clientId',  cfg.clientId);
    fd.append('contactId', contactId);
    fd.append('__RequestVerificationToken', v.getToken());

    try {
      const data = await ax.post(cfg.unlinkUrl, fd);

      if (data.success) {
        tbody.querySelector(`tr[data-contact-id="${contactId}"]`)?.remove();
        const c = data.contact;
        addToDropdown(c.id, `${c.surname} ${c.name}`);
        toggleEmptyState();
      } else {
        v.showAlert('danger', data.message || 'Could not unlink contact.');
        btn.disabled = false;
      }
    } catch {
      v.showAlert('danger', 'A network error occurred.');
      btn.disabled = false;
    }
  });
}
```

### Code Snippet — `ClientsController.cs` (UnlinkContact action)

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UnlinkContact([FromForm] int clientId, [FromForm] int contactId)
{
    var (success, contact, error) = await _clientService.UnlinkContactAsync(clientId, contactId);
    if (!success)
        return Json(new { success = false, message = error });

    return Json(new { success = true, contact });
}
```

> [SCREENSHOT PLACEHOLDER: Unlink button before and after]

---

## 7. Client Code Generation

### Business Rule

When a client is created, the system automatically generates a unique **client code** using this algorithm:

1. Take the client's name, convert to uppercase
2. Take the first **3 characters** (pad with A, B, C... if the name is shorter than 3 characters)
3. Append a **3-digit sequence number** starting at `001`, incrementing until a unique code is found in the database

### Code Snippet — `ClientService.GenerateClientCodeAsync`

```csharp
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
```

### Examples

| Input Name        | Prefix | Sequence | Generated Code |
|-------------------|--------|----------|----------------|
| Acme Corp         | ACM    | 001      | `ACM001`       |
| Jo (short name)   | JOA    | 001      | `JOA001`       |
| A (single char)   | AAA    | 001      | `AAA001`       |
| Acme Corp (2nd)   | ACM    | 002      | `ACM002`       |
| Acme Corp (3rd)   | ACM    | 003      | `ACM003`       |

> [SCREENSHOT PLACEHOLDER: Client list showing generated codes]

---

## 8. Validation

### Server-side Validation

Server-side validation runs inside the service layer and cannot be bypassed by the client.

**Where:** `ContactService.CreateContactAsync` and `UpdateContactAsync`

```csharp
public async Task<(bool success, int contactId, string? error)> CreateContactAsync(ContactFormViewModel vm)
{
    var emailTaken = await _db.Contacts.AnyAsync(c => c.Email == vm.Email);
    if (emailTaken) return (false, 0, "Email is already in use.");

    var contact = new Contact { Name = vm.Name, Surname = vm.Surname, Email = vm.Email };
    _db.Contacts.Add(contact);
    await _db.SaveChangesAsync();
    return (true, contact.Id, null);
}
```

**Where (controller-level):** `ContactsController.Create` checks `ModelState.IsValid` before calling the service, catching data annotation violations (Required, MaxLength, EmailAddress):

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create([FromForm] ContactFormViewModel vm)
{
    if (!ModelState.IsValid)
        return Json(new { success = false, message = "Please correct the errors below." });

    var (success, contactId, error) = await _contactService.CreateContactAsync(vm);
    if (!success)
        return Json(new { success = false, message = error });

    return Json(new { success = true, redirectUrl = Url.Action("Edit", new { id = contactId }) });
}
```

### Client-side Validation

Client-side validation provides instant feedback without a network round-trip.

**Where:** `validation.js` (shared module) + `contacts.js`

```javascript
// validation.js — validateEmail
function validateEmail(input) {
    const val = input.value.trim();
    const feedback = document.getElementById('email-feedback');

    if (!val) {
        _setValidity(input, false);
        if (feedback) feedback.textContent = 'Email is required.';
        return false;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(val)) {
        _setValidity(input, false);
        if (feedback) feedback.textContent = 'Email is not valid.';
        return false;
    }

    _setValidity(input, true);
    return true;
}
```

```javascript
// contacts.js — called on submit and on input events
form.addEventListener('submit', async function (e) {
    e.preventDefault();

    const nameValid    = v.validateRequired(nameInput,    'Name is required.');
    const surnameValid = v.validateRequired(surnameInput, 'Surname is required.');
    const emailValid   = v.validateEmail(emailInput);

    if (!nameValid || !surnameValid || !emailValid) return;  // stop here if invalid
    // ... proceed with AJAX save
});

// Real-time feedback as user types
emailInput.addEventListener('input', function () { v.validateEmail(this); });
```

> [SCREENSHOT PLACEHOLDER: Validation error on form]

---

## 9. Screenshots

> [SCREENSHOT: Home/Clients list]

> [SCREENSHOT: Create client form — General tab]

> [SCREENSHOT: Client form — Contacts tab with linked contacts]

> [SCREENSHOT: Contacts list]

> [SCREENSHOT: Create contact form — General tab]

> [SCREENSHOT: Contact form — Clients tab]

> [SCREENSHOT: Validation error]

> [SCREENSHOT: Empty state]

---

## 10. Presentation Notes

### Project Overview

**What to say:**
- "This app manages clients and their contacts in a many-to-many relationship — a client can have multiple contacts, and a contact can belong to multiple clients."
- "I built it in C# with ASP.NET Core MVC, using Entity Framework Core to talk to a MySQL database, and vanilla JavaScript for interactive features."
- "The two main entities are Clients — which represent businesses or individuals — and Contacts, which represent people."

**Anticipated questions:**
- *Why ASP.NET Core MVC and not a SPA framework?* → "MVC with server-rendered views is well-suited for CRUD apps like this. It keeps things simple and avoids the overhead of a separate frontend build pipeline."
- *How do you run the app?* → "Clone the repo, set the connection string in `appsettings.json`, run `dotnet ef database update` to create the tables, then `dotnet run`."

---

### Architecture / MVC + Service Layer

**What to say:**
- "I split the app into Controllers, Services, and Models. Controllers are thin — they just route requests and return responses. All business logic lives in the service layer."
- "The service layer talks to the database via EF Core, and the controller talks to the service via an interface — so nothing is tightly coupled."
- "This separation means I can test the service without spinning up a web server, or swap the implementation without touching the controller."

**Anticipated questions:**
- *Why a service layer on top of MVC?* → "Without it, business logic leaks into controllers. A service layer gives each class a single responsibility and makes the code easier to test and maintain."
- *What are the ViewModels for?* → "They shape the data for each specific view — the form only receives what it needs, and the entity models stay clean."

---

### Database & Relationships

**What to say:**
- "There are three tables: Clients, Contacts, and ClientContacts. ClientContacts is a junction table that implements the many-to-many relationship with a composite primary key."
- "EF Core configures the relationships, indexes, and cascade delete in `AppDbContext.OnModelCreating`."
- "Both ClientCode and Email have unique indexes enforced at the database level."

**Anticipated questions:**
- *Why a junction table instead of a direct relationship?* → "A many-to-many relationship requires a junction table — one client row can't hold multiple contact foreign keys, and vice versa."
- *What happens if a client is deleted?* → "Cascade delete is configured, so all related ClientContacts rows are automatically removed."

---

### OOP Principles

**What to say:**
- "**Encapsulation:** `ClientCode` is `init`-only — it's set once on creation and can't be changed. Internal DB state is always projected into ViewModels before reaching the view."
- "**Abstraction:** Controllers only know about the `IClientService` interface — they have no idea EF Core or MySQL exists."
- "**Inheritance:** All controllers inherit from ASP.NET Core's `Controller` base class, gaining `View()`, `Json()`, `ModelState`, and more."
- "**Polymorphism:** The DI container injects `ClientService` into a variable typed as `IClientService`. At runtime the concrete type is resolved — swapping implementations requires only a one-line change in `Program.cs`."

**Anticipated questions:**
- *Where is polymorphism in a CRUD app?* → "It's in the dependency injection — the controller uses the interface type, and the DI container decides which concrete class to inject at runtime."

---

### SOLID Principles

**What to say:**
- "**S:** `ClientsController` only handles HTTP. `ClientService` only handles business logic. Each has exactly one reason to change."
- "**O:** The interfaces are a stable contract. Adding a caching layer means creating a new class — no existing code needs to change."
- "**L:** `ClientService` implements every method on `IClientService` correctly. You can substitute it anywhere the interface is used."
- "**I:** `IClientService` and `IContactService` are separate — neither interface has methods that don't belong to it."
- "**D:** Controllers depend on interfaces, not concrete classes. The DI container in `Program.cs` wires up the real implementations."

**Anticipated questions:**
- *How does DI help with SOLID?* → "DI enforces the Dependency Inversion Principle — high-level modules declare what they need (via constructor parameters typed as interfaces), and the container provides it."

---

### AJAX / Unlink Flow

**What to say:**
- "When you click Unlink, no page reload happens. JavaScript intercepts the click, sends a POST request in the background using `fetch()`, and when the server responds with JSON, the row is removed from the table and the contact is moved back to the dropdown — all in under a second."
- "The CSRF anti-forgery token is included in every POST to prevent cross-site request forgery."
- "The server always returns `{ success: true/false, ... }` so the JS can handle errors gracefully."

**Anticipated questions:**
- *Why not just submit a form and reload the page?* → "Full reloads lose scroll position and feel slow. AJAX gives a much better UX, especially on a form with tabs."
- *What if the network fails?* → "The `catch` block in every AJAX call shows an error alert — the UI never silently fails."

---

### Client Code Generation

**What to say:**
- "Every client gets an auto-generated code: the first 3 characters of their name (uppercased), plus a 3-digit sequence number. For example, 'Acme Corp' → `ACM001`."
- "The code is generated in `ClientService.GenerateClientCodeAsync` — it checks the database to guarantee uniqueness before returning."
- "Short names are padded: 'Jo' → `JOA001`. The code is `init`-only on the model, so it can never be changed after creation."

**Anticipated questions:**
- *What if the prefix runs out of sequence numbers?* → "The method throws an `InvalidOperationException` after 999 attempts — in practice this would never happen in normal usage."
- *Can a user change the client code?* → "No. `ClientCode` uses C#'s `init` accessor — once set in the constructor, it's immutable."

---

### Validation

**What to say:**
- "Validation happens on two levels. Client-side: as you type, the JavaScript module highlights invalid fields immediately. Server-side: the controller checks `ModelState` and the service checks business rules like email uniqueness — these run even if someone bypasses the JS."
- "Email uniqueness is enforced in the service layer with a database query, and also at the database level with a unique index."

**Anticipated questions:**
- *Why both client-side and server-side validation?* → "Client-side is for UX — instant feedback. Server-side is for security — you can never trust the browser. Both are needed."
- *What data annotation attributes do you use?* → "`[Required]`, `[MaxLength]`, and `[EmailAddress]` on the model properties. EF Core uses `[MaxLength]` to set the column length in the database."
