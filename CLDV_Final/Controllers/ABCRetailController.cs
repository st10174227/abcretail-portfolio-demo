using ABCRetail.Demo;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers;
public sealed class ABCRetailController(RetailStore store) : Controller
{
    [HttpGet] public IActionResult ListContract() => View(store.Data.Files.Where(x=>!x.IsImage).ToList());
    [HttpGet] public IActionResult ListImages() => View(store.Data.Files.Where(x=>x.IsImage).ToList());
    [HttpGet] public IActionResult UploadContract() => View();
    [HttpGet] public IActionResult UploadImage() => View();
    [HttpPost] public async Task<IActionResult> UploadContract(IFormFile? contractFile)
    {
        try
        {
            var file=await store.ReadFile(contractFile,false) ?? throw new RetailException("Choose a sample PDF contract.");
            store.AddFile(file); TempData["Success"]="Sample contract uploaded to your temporary workspace."; return RedirectToAction(nameof(ListContract));
        }
        catch(RetailException ex) { ModelState.AddModelError("",ex.Message); return View(); }
    }
    [HttpPost] public async Task<IActionResult> UploadImage(IFormFile? imageFile)
    {
        try
        {
            var file=await store.ReadFile(imageFile,true) ?? throw new RetailException("Choose a sample image.");
            store.AddFile(file); TempData["Success"]="Image uploaded to your temporary workspace."; return RedirectToAction(nameof(ListImages));
        }
        catch(RetailException ex) { ModelState.AddModelError("",ex.Message); return View(); }
    }
    [HttpGet] public IActionResult DownloadContract(string id)
    {
        var file=store.Data.Files.FirstOrDefault(x=>x.Id==id && !x.IsImage); return file is null ? NotFound() : File(file.Bytes,file.ContentType,file.Name);
    }
    [HttpGet] public IActionResult Image(string id)
    {
        var file=store.Data.Files.FirstOrDefault(x=>x.Id==id && x.IsImage); return file is null ? NotFound() : File(file.Bytes,file.ContentType);
    }
    [HttpPost] public IActionResult DeleteContract(string id) => DeleteFile(id,false);
    [HttpPost] public IActionResult DeleteImage(string id) => DeleteFile(id,true);
    private IActionResult DeleteFile(string id,bool image)
    {
        var file=store.Data.Files.FirstOrDefault(x=>x.Id==id && x.IsImage==image); if(file is null)return NotFound();
        store.DeleteFile(file); TempData["Success"]="File removed from your workspace."; return RedirectToAction(image ? nameof(ListImages) : nameof(ListContract));
    }
}
