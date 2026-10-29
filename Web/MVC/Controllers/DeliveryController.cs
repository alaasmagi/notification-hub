using Contracts.DataAccess;
using DTO.Web.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.MVC.Controllers;

[Authorize]
public class DeliveryController : Controller
{
    private readonly DeliveryDtoMapper _mapper = new();
    private readonly IDeliveryRepository _emailRepository;

    public DeliveryController(IDeliveryRepository emailRepository)
    {
        _emailRepository = emailRepository;
    }

    public async Task<IActionResult> Index()
    {
        var emails = await _emailRepository.GetAllForAdminAsync();
        return View(_mapper.Map(emails));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var email = await _emailRepository.GetForAdminAsync(id);

        if (email is null)
        {
            return NotFound();
        }

        return View(_mapper.Map(email));
    }
}
