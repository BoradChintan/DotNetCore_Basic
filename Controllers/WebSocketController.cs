using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Text;
using DotNETBasic.Services;

namespace DotNETBasic.Controllers
{
    public class WebSocketController : Controller
    {
         
        public IActionResult Index()
        {
            return View();
        }
    }
}
