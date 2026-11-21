using ABCRetail.Demo;
using ABCRetail.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers;
public sealed class ProductController(RetailStore store) : Controller
{
    [HttpGet] public IActionResult Index() => View(store.Data.Products);
    [HttpGet] public IActionResult AddProduct() => View("Form",new ProductForm());
    [HttpPost] public async Task<IActionResult> AddProduct(ProductForm form,IFormFile? imageFile)
    {
        form.Id="";
        if(ModelState.IsValid) try { var image=await store.ReadFile(imageFile,true); store.SaveProduct(form,null,image); TempData["Success"]="Product added to your demo."; return RedirectToAction(nameof(Index)); }
            catch(RetailException ex) { ModelState.AddModelError("",ex.Message); }
        return View("Form",form);
    }
    [HttpGet] public IActionResult EditProduct(string id)
    {
        var entity=store.Product(id); return entity is null ? NotFound() : View("Form",new ProductForm { Id=entity.Id,Version=entity.Version,Name=entity.Name,Description=entity.Description,Price=entity.Price,ExistingImageId=entity.ImageId });
    }
    [HttpPost] public async Task<IActionResult> EditProduct(ProductForm form,IFormFile? imageFile)
    {
        var entity=store.Product(form.Id); if(entity is null)return NotFound(); form.ExistingImageId=entity.ImageId;
        if(ModelState.IsValid) try { var image=await store.ReadFile(imageFile,true); store.SaveProduct(form,entity,image); TempData["Success"]="Product updated."; return RedirectToAction(nameof(Index)); }
            catch(RetailException ex) { ModelState.AddModelError("",ex.Message); }
        return View("Form",form);
    }
    [HttpGet] public IActionResult DeleteProduct(string id) { var entity=store.Product(id); return entity is null ? NotFound() : View("Delete",entity); }
    [HttpPost,ActionName("DeleteProduct")] public IActionResult DeleteConfirmed(string id,int version)
    {
        var entity=store.Product(id); if(entity is null)return NotFound();
        try { store.DeleteProduct(entity,version); TempData["Success"]="Product removed."; return RedirectToAction(nameof(Index)); }
        catch(RetailException ex) { ModelState.AddModelError("",ex.Message); return View("Delete",entity); }
    }
}
