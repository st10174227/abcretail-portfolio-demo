using ABCRetail.Demo;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers;
public sealed class DemoController(RetailStore store) : Controller
{
    [HttpGet] public IActionResult Index() => View(store.Data.Events);
    [HttpPost] public IActionResult ProcessNext() { store.ProcessNext(); return RedirectToAction(nameof(Index)); }
    [HttpPost] public IActionResult Reset()
    {
        store.Data.Reset(); TempData.Clear(); TempData["Success"]="Your demo has been reset to fresh sample data."; return RedirectToAction("Index","Home");
    }
}
