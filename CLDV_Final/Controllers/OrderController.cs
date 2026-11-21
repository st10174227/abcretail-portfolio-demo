using ABCRetail.Demo;
using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers;
public sealed class OrderController(RetailStore store) : Controller
{
    private OrderForm Choices(OrderForm form) { form.Customers=store.Data.Customers; form.Products=store.Data.Products; return form; }
    [HttpGet] public IActionResult Index() => View(store.Data.Orders);
    [HttpGet] public IActionResult AddOrder() => View("Form",Choices(new()));
    [HttpPost] public IActionResult AddOrder(OrderForm form)
    {
        form.Id="";
        if(ModelState.IsValid)try { store.SaveOrder(form,null); TempData["Success"]="Demo order recorded. No payment or delivery is created."; return RedirectToAction(nameof(Index)); }
            catch(RetailException ex) { ModelState.AddModelError("",ex.Message); }
        return View("Form",Choices(form));
    }
    [HttpGet] public IActionResult EditOrder(string id)
    {
        var entity=store.Order(id); return entity is null ? NotFound() : View("Form",Choices(new OrderForm { Id=entity.Id,Version=entity.Version,CustomerId=entity.CustomerId,ProductId=entity.ProductId,Quantity=entity.Quantity,OrderDate=entity.OrderDate }));
    }
    [HttpPost] public IActionResult EditOrder(OrderForm form)
    {
        var entity=store.Order(form.Id); if(entity is null)return NotFound();
        if(ModelState.IsValid)try { store.SaveOrder(form,entity); TempData["Success"]="Order updated."; return RedirectToAction(nameof(Index)); }
            catch(RetailException ex) { ModelState.AddModelError("",ex.Message); }
        return View("Form",Choices(form));
    }
    [HttpGet] public IActionResult DeleteOrder(string id) { var entity=store.Order(id); return entity is null ? NotFound() : View("Delete",entity); }
    [HttpPost,ActionName("DeleteOrder")] public IActionResult DeleteConfirmed(string id,int version)
    {
        var entity=store.Order(id); if(entity is null)return NotFound();
        try { store.DeleteOrder(entity,version); TempData["Success"]="Order removed."; return RedirectToAction(nameof(Index)); }
        catch(RetailException ex) { ModelState.AddModelError("",ex.Message); return View("Delete",entity); }
    }
}
