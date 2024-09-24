using DotNETBasic.Models;
using DotNETBasic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.PowerPlatform.Dataverse.Client;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Security.Claims;

namespace DotNETBasic.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataverseService _service;


        public HomeController(ILogger<HomeController> logger , DataverseService dataverseService)
        {
            _logger = logger;
            _service = dataverseService;

        }

       
        public IActionResult Index()
        {
            var claims = User.Claims.ToList();
            string uniqueEmail = claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;


            List<string> roles = _service.GetRoles(uniqueEmail);
             
            HttpContext.Session.SetString("UserRoles", JsonConvert.SerializeObject(roles));

            return View();
        }


        [CustomAuthorize("Admin")]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
