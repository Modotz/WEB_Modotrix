using ERP.Web.Models;
using ERP.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers;

public class MenusController : Controller
{
    private readonly ApiClient _api;

    public MenusController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> IndexOld(string? search = null)
    {
        ViewBag.Search = search;

        var menus = await _api.GetMenuAsync(search);

        return View(menus);
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search = null,
        int page = 1,
        int pageSize = 1000)
    {
        var result =
            await _api.GetMenuAsync(
                search,
                page,
                pageSize
            );

        ViewBag.Search = search;

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        var menu = await _api.GetMenuAsync(id);

        if (menu is null)
            return NotFound();

        return Json(menu);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            ModelState.AddModelError(
                nameof(model.Title),
                "Judul wajib diisi."
            );
        }

        if (!ModelState.IsValid)
        {
            foreach (var item in ModelState)
            {
                foreach (var error in item.Value.Errors)
                {
                    Console.WriteLine(
                        $"MODELSTATE ERROR | Field: {item.Key} | Error: {error.ErrorMessage}"
                    );

                    if (error.Exception != null)
                    {
                        Console.WriteLine(
                            $"EXCEPTION: {error.Exception.Message}"
                        );
                    }
                }
            }

            TempData["Error"] =
                "Data menu belum lengkap.";

            return RedirectToAction(nameof(Index));
        }

        var result = await _api.CreateMenuAsync(model);

        if (!result.Success)
        {
            TempData["Error"] =
                result.Error ?? "Gagal menambahkan menu.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] =
            "Menu berhasil ditambahkan.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOrder(
        [FromBody] List<MenuViewModel> request)
    {
        try
        {
            if (request == null || request.Count == 0)
            {
                return BadRequest(
                    "Data urutan menu kosong."
                );
            }

            var response =
                await _api.SaveMenuOrderAsync(request);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(
                    (int)response.StatusCode,
                    responseBody
                );
            }

            return Ok(new
            {
                message =
                    "Urutan menu berhasil disimpan."
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "ERROR SaveOrder MVC:"
            );

            Console.WriteLine(
                ex.ToString()
            );

            return StatusCode(500, new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        MenuViewModel model)
    {
        ModelState.Remove(
            nameof(model.Title)
        );

        if (!ModelState.IsValid)
        {
            TempData["Error"] =
                "Data menu belum lengkap.";

            return RedirectToAction(
                nameof(Index)
            );
        }

        var result =
            await _api.UpdateMenuAsync(model);

        TempData[
            result.Success
                ? "Success"
                : "Error"
        ] =
            result.Success
                ? "Menu berhasil diubah."
                : result.Error
                    ?? "Gagal mengubah menu.";

        return RedirectToAction(
            nameof(Index)
        );
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        long id)
    {
        var result =
            await _api.DeleteMenuAsync(id);

        TempData[
            result.Success
                ? "Success"
                : "Error"
        ] =
            result.Success
                ? "Menu berhasil dihapus."
                : result.Error
                    ?? "Gagal menghapus menu.";

        return RedirectToAction(
            nameof(Index)
        );
    }
}