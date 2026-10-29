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
public class NotificationTypeController : Controller
{
    private readonly IClientRepository _clientRepository;
    private readonly INotificationTypeRepository _notificationTypeRepository;
    private readonly NotificationTypeDtoMapper _mapper = new();
    private readonly IBaseUow _uow;

    public NotificationTypeController(
        IClientRepository clientRepository,
        INotificationTypeRepository notificationTypeRepository,
        IBaseUow uow)
    {
        _clientRepository = clientRepository;
        _notificationTypeRepository = notificationTypeRepository;
        _uow = uow;
    }

    public async Task<IActionResult> Index()
    {
        var notificationTypes = await _notificationTypeRepository.GetAllForAdminAsync();
        var clients = (await _clientRepository.GetAllForAdminAsync())
            .ToDictionary(x => x.Id, x => x.Source);

        var rows = notificationTypes
            .Select(notificationType => new NotificationTypeIndexRow
            {
                NotificationType = _mapper.Map(notificationType)!,
                ClientSource = clients.GetValueOrDefault(notificationType.ClientId, "Unknown")
            })
            .ToList();

        return View(rows);
    }

    public async Task<IActionResult> Create()
    {
        await LoadClientsAsync();
        return View(new NotificationTypeDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NotificationTypeDto notificationType)
    {
        if (!ModelState.IsValid)
        {
            await LoadClientsAsync();
            return View(notificationType);
        }

        var response = await _notificationTypeRepository.CreateAsync(_mapper.Map(notificationType)!);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            await LoadClientsAsync();
            return View(notificationType);
        }

        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var notificationType = _mapper.Map(await _notificationTypeRepository.GetForAdminAsync(id));
        if (notificationType is null)
        {
            return NotFound();
        }

        await LoadClientsAsync();
        return View(notificationType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, NotificationTypeDto notificationType)
    {
        if (id != notificationType.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadClientsAsync();
            return View(notificationType);
        }

        var response = await _notificationTypeRepository.UpdateAsync(id, _mapper.Map(notificationType)!, null, Guid.Empty);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            await LoadClientsAsync();
            return View(notificationType);
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
            var response = await _notificationTypeRepository.RemoveAsync(id, null, Guid.Empty);
            if (response.Successful)
            {
                await _uow.SaveChangesAsync();
            }

            TempData[response.Successful ? "SuccessMessage" : "ErrorMessage"] = response.Successful
                ? "Notification type deleted."
                : response.Error!.Message;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Notification type cannot be deleted while templates reference it.";
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
