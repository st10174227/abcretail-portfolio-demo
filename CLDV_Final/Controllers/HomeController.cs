using ABCRetail.Demo;
using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ABCRetail.Controllers;
public sealed class HomeController(RetailStore store) : Controller
{
    [HttpGet] public IActionResult Index() => View(new Dashboard(store.Data.Customers.Count,store.Data.Products.Count,store.Data.Orders.Count,
        store.Data.Files.Count(x=>!x.IsImage),store.Data.Events.Count(x=>x.ProcessedAt is null),store.Data.Orders.Sum(x=>x.Total)));
    [HttpGet] public IActionResult Privacy() => View();
    [HttpGet] public IActionResult Error() => View(new ErrorViewModel { RequestId=Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
