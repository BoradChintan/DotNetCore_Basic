using Microsoft.AspNetCore.Mvc;

namespace DotNETBasic.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult AccessDenied()
        {
            return View("_AccessDenied");
        }
    }
}
