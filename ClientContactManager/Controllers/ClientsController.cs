using ClientContactManager.Models;
using ClientContactManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClientContactManager.Controllers;

public class ClientsController : Controller
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    public async Task<IActionResult> Index()
    {
        var clients = await _clientService.GetAllClientsAsync();
        return View(clients);
    }

    public IActionResult Create()
    {
        return View("CreateEdit", new ClientFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] ClientFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Please correct the errors below." });

        var (success, clientId, error) = await _clientService.CreateClientAsync(vm);
        if (!success)
            return Json(new { success = false, message = error });

        return Json(new { success = true, redirectUrl = Url.Action("Edit", new { id = clientId }) });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _clientService.GetClientFormViewModelAsync(id);
        if (vm == null) return NotFound();
        return View("CreateEdit", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [FromForm] ClientFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Please correct the errors below." });

        var (success, error) = await _clientService.UpdateClientAsync(id, vm);
        if (!success)
            return Json(new { success = false, message = error });

        return Json(new { success = true, message = "Client saved successfully." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkContact([FromForm] int clientId, [FromForm] int contactId)
    {
        var (success, contact, error) = await _clientService.LinkContactAsync(clientId, contactId);
        if (!success)
            return Json(new { success = false, message = error });

        return Json(new { success = true, contact });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkContact([FromForm] int clientId, [FromForm] int contactId)
    {
        var (success, contact, error) = await _clientService.UnlinkContactAsync(clientId, contactId);
        if (!success)
            return Json(new { success = false, message = error });

        return Json(new { success = true, contact });
    }
}
