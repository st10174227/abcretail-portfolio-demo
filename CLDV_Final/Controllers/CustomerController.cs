using ABCRetail.Demo;
using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers;
public sealed class CustomerController(RetailStore store) : Controller
{
    [HttpGet] public IActionResult Index() => View(store.Data.Customers);
    [HttpGet] public IActionResult AddCustomer() => View("Form",new CustomerForm());
    [HttpPost] public IActionResult AddCustomer(CustomerForm form)
    {
        form.Id = "";
        if (ModelState.IsValid) try { store.SaveCustomer(form,null); TempData["Success"] = "Customer added to your demo."; return RedirectToAction(nameof(Index)); }
            catch (RetailException ex) { ModelState.AddModelError("",ex.Message); }
        return View("Form",form);
    }
    [HttpGet] public IActionResult EditCustomer(string id)
    {
        var entity = store.Customer(id); return entity is null ? NotFound() : View("Form",new CustomerForm { Id=entity.Id,Version=entity.Version,Name=entity.Name,Email=entity.Email });
    }
    [HttpPost] public IActionResult EditCustomer(CustomerForm form)
    {
        var entity = store.Customer(form.Id); if (entity is null) return NotFound();
        if (ModelState.IsValid) try { store.SaveCustomer(form,entity); TempData["Success"] = "Customer updated."; return RedirectToAction(nameof(Index)); }
            catch (RetailException ex) { ModelState.AddModelError("",ex.Message); }
        return View("Form",form);
    }
    [HttpGet] public IActionResult DeleteCustomer(string id) { var entity=store.Customer(id); return entity is null ? NotFound() : View("Delete",entity); }
    [HttpPost, ActionName("DeleteCustomer")] public IActionResult DeleteConfirmed(string id,int version)
    {
        var entity=store.Customer(id); if(entity is null)return NotFound();
        try { store.DeleteCustomer(entity,version); TempData["Success"]="Customer removed."; return RedirectToAction(nameof(Index)); }
        catch(RetailException ex) { ModelState.AddModelError("",ex.Message); return View("Delete",entity); }
    }
}
