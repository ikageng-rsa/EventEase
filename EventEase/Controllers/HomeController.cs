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

        public HomeController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {

            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

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
