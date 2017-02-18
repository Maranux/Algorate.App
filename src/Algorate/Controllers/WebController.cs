using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

// This is the controller for users who are not logged in.
// NOTE: _WebLayout is the layout for all pages without authentication.

namespace Algorate.Controllers
{
    public class WebController : Controller
    {
        [ViewLayout("_WebLayout")]
        // GET: /<controller>/
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return this.RedirectToAction("Index", "Home");
            } else
            {
                return View();
            }
        }

        [Authorize]
        [ViewLayout("_WebLayout")]
        // GET: /<controller>/login
        public void Login()
        {
        }
    }
}
