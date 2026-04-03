using ClientContactManager.Models;
using ClientContactManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClientContactManager.Controllers;

public class ContactsController : Controller
{
    private readonly IContactService _contactService;

    public ContactsController(IContactService contactService)
    {
        _contactService = contactService;
    }

    public async Task<IActionResult> Index()
    {
        var contacts = await _contactService.GetAllContactsAsync();
        return View(contacts);
    }

    public IActionResult Create()
    {
        return View("CreateEdit", new ContactFormViewModel());
    }

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

    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _contactService.GetContactFormViewModelAsync(id);
        if (vm == null) return NotFound();
        return View("CreateEdit", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [FromForm] ContactFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Please correct the errors below." });

        var (success, error) = await _contactService.UpdateContactAsync(id, vm);
        if (!success)
            return Json(new { success = false, message = error });

        return Json(new { success = true, message = "Contact saved successfully." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkClient([FromForm] int contactId, [FromForm] int clientId)
    {
        var (success, client, error) = await _contactService.LinkClientAsync(contactId, clientId);
        if (!success)
            return Json(new { success = false, message = error });

        return Json(new { success = true, client });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkClient([FromForm] int contactId, [FromForm] int clientId)
    {
        var (success, client, error) = await _contactService.UnlinkClientAsync(contactId, clientId);
        if (!success)
            return Json(new { success = false, message = error });

        return Json(new { success = true, client });
    }
}
