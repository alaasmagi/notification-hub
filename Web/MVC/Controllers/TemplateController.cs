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
public class TemplateController : Controller
{
    private readonly IClientRepository _clientRepository;
    private readonly INotificationTypeRepository _notificationTypeRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly TemplateDtoMapper _mapper = new();
    private readonly IBaseUow _uow;

    public TemplateController(
        IClientRepository clientRepository,
        INotificationTypeRepository notificationTypeRepository,
        ITemplateRepository templateRepository,
        IBaseUow uow)
    {
        _clientRepository = clientRepository;
        _notificationTypeRepository = notificationTypeRepository;
        _templateRepository = templateRepository;
        _uow = uow;
    }

    public async Task<IActionResult> Index()
    {
        var templates = await _templateRepository.GetAllForAdminAsync();
        var types = (await _notificationTypeRepository.GetAllForAdminAsync()).ToDictionary(x => x.Id);
        var clients = (await _clientRepository.GetAllForAdminAsync()).ToDictionary(x => x.Id, x => x.Source);
        var rows = templates.Select(template => new TemplateIndexRow
        {
            Template = _mapper.Map(template)!,
            ClientSource = clients.GetValueOrDefault(types[template.NotificationTypeId].ClientId, "Unknown"),
            NotificationTypeKey = types[template.NotificationTypeId].Key
        }).ToList();

        return View(rows);
    }

    public async Task<IActionResult> Create()
    {
        await LoadNotificationTypesAsync();
        return View(new TemplateDto { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TemplateDto template)
    {
        if (!ModelState.IsValid)
        {
            await LoadNotificationTypesAsync();
            return View(template);
        }

        var response = await _templateRepository.CreateAsync(_mapper.Map(template)!);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            await LoadNotificationTypesAsync();
            return View(template);
        }

        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var template = _mapper.Map(await _templateRepository.GetForAdminAsync(id));
        if (template is null)
        {
            return NotFound();
        }

        await LoadNotificationTypesAsync();
        return View(template);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TemplateDto template)
    {
        if (id != template.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadNotificationTypesAsync();
            return View(template);
        }

        var response = await _templateRepository.UpdateAsync(id, _mapper.Map(template)!, null, Guid.Empty);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            await LoadNotificationTypesAsync();
            return View(template);
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
            var response = await _templateRepository.RemoveAsync(id, null, Guid.Empty);
            if (response.Successful)
            {
                await _uow.SaveChangesAsync();
            }

            TempData[response.Successful ? "SuccessMessage" : "ErrorMessage"] = response.Successful
                ? "Template deleted."
                : response.Error!.Message;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Template could not be deleted.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadNotificationTypesAsync()
    {
        var clients = (await _clientRepository.GetAllForAdminAsync())
            .ToDictionary(x => x.Id, x => x.Source);

        ViewBag.NotificationTypes = (await _notificationTypeRepository.GetAllForAdminAsync())
            .Select(x => new NotificationTypeOption
            {
                Id = x.Id,
                ClientSource = clients.GetValueOrDefault(x.ClientId, "Unknown"),
                Key = x.Key
            }).ToList();
    }
}
