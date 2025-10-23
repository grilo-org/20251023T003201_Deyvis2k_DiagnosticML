using Microsoft.AspNetCore.Mvc;
using CSProject.Models;

namespace CSProject.Controllers;

public class AccountController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    public IActionResult Logout()
    {
        return View();
    }
}
