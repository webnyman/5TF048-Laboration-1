using Microsoft.AspNetCore.Mvc;

namespace PracticeLogger.Models
{
    public class PracticeTypeCatalog : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
