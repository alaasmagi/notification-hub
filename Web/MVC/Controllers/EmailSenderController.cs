using Base.Contracts.DataAccess;
using Contracts.DataAccess;
using DTO.Web;
using DTO.Web.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.MVC.Models;

namespace Web.MVC.Controllers;

[Authorize]
public class EmailSenderController : Controller
{
    private readonly IClientRepository _clientRepository;
    private readonly IEmailSenderRepository _emailSenderRepository;
    private readonly EmailSenderDtoMapper _mapper = new();
    private readonly IBaseUow _uow;

    public EmailSenderController(
        IClientRepository clientRepository,
        IEmailSenderRepository emailSenderRepository,
        IBaseUow uow)
    {
        _clientRepository = clientRepository;
        _emailSenderRepository = emailSenderRepository;
        _uow = uow;
    }

    public async Task<IActionResult> Index()
    {
        var emailSenders = await _emailSenderRepository.GetAllForAdminAsync();
        var clients = (await _clientRepository.GetAllForAdminAsync())
            .ToDictionary(x => x.Id, x => x.Source);

        var rows = emailSenders
            .Select(emailSender => new EmailSenderIndexRow
            {
                EmailSender = _mapper.Map(emailSender)!,
                ClientSource = clients.GetValueOrDefault(emailSender.ClientId, "Unknown")
            })
            .ToList();

        return View(rows);
    }

    public async Task<IActionResult> Create()
    {
        await LoadClientsAsync();
        return View(new EmailSenderDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmailSenderDto emailSender)
    {
        if (!ModelState.IsValid)
        {
            await LoadClientsAsync();
            return View(emailSender);
        }

        var response = await _emailSenderRepository.CreateAsync(_mapper.Map(emailSender)!);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            await LoadClientsAsync();
            return View(emailSender);
        }

        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var emailSender = _mapper.Map(await _emailSenderRepository.GetForAdminAsync(id));
        if (emailSender is null)
        {
            return NotFound();
        }

        await LoadClientsAsync();
        return View(emailSender);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EmailSenderDto emailSender)
    {
        if (id != emailSender.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadClientsAsync();
            return View(emailSender);
        }

        var response = await _emailSenderRepository.UpdateAsync(id, _mapper.Map(emailSender)!, null, Guid.Empty);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            await LoadClientsAsync();
            return View(emailSender);
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
            var response = await _emailSenderRepository.RemoveAsync(id, null, Guid.Empty);
            if (response.Successful)
            {
                await _uow.SaveChangesAsync();
            }

            TempData[response.Successful ? "SuccessMessage" : "ErrorMessage"] = response.Successful
                ? "Email sender deleted."
                : response.Error!.Message;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Email sender could not be deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadClientsAsync()
    {
        var clients = (await _clientRepository.GetAllForAdminAsync())
            .OrderBy(x => x.Source)
            .Select(x => new ClientOption
            {
                Id = x.Id,
                Source = x.Source,
                DisplayName = x.DisplayName
            })
            .ToList();

        ViewBag.Clients = clients;
    }
}
