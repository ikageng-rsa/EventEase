using EventEase.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EventEase.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly SignInManager<IdentityUser> _signInManager;

        public HomeController(SignInManager<IdentityUser> signInManager, ILogger<HomeController> logger)
        {
            _signInManager = signInManager;
            _logger = logger;

        }

        [HttpGet("")]
        public IActionResult Index()
        {

            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [HttpGet("privacy")]
        public IActionResult Privacy()
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
