using Base.Contracts.DataAccess;
using Contracts.DataAccess;
using DTO.Web;
using DTO.Web.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.MVC.Controllers;

[Authorize]
public class ClientController : Controller
{
    private readonly IClientRepository _clientRepository;
    private readonly ClientDtoMapper _mapper = new();
    private readonly IBaseUow _uow;

    public ClientController(IClientRepository clientRepository, IBaseUow uow)
    {
        _clientRepository = clientRepository;
        _uow = uow;
    }

    public async Task<IActionResult> Index()
    {
        var clients = await _clientRepository.GetAllForAdminAsync();
        return View(_mapper.Map(clients));
    }

    public IActionResult Create()
    {
        return View(new ClientDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientDto client)
    {
        if (!ModelState.IsValid)
        {
            return View(client);
        }

        var response = await _clientRepository.CreateAsync(_mapper.Map(client)!);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            return View(client);
        }

        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var client = _mapper.Map(await _clientRepository.GetForAdminAsync(id));
        if (client is null)
        {
            return NotFound();
        }

        return View(client);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ClientDto client)
    {
        if (id != client.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(client);
        }

        var response = await _clientRepository.UpdateAsync(id, _mapper.Map(client)!, null, Guid.Empty);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            return View(client);
        }

        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var response = await _clientRepository.RemoveAsync(id, null, Guid.Empty);
            if (response.Successful)
            {
                await _uow.SaveChangesAsync();
            }

            TempData[response.Successful ? "SuccessMessage" : "ErrorMessage"] = response.Successful
                ? "Client deleted."
                : response.Error!.Message;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Client cannot be deleted while email senders or notification types reference it.";
        }

        return RedirectToAction(nameof(Index));
    }
}
