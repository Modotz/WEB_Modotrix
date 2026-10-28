using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

public class DashboardController : Controller
{
    private readonly AccessService _access;

    public DashboardController(AccessService access)
    {
        _access = access;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _access.GetAsync());
    }
}
