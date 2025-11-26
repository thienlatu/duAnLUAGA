using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace duAnLUAGA.Areas.Employee.Controllers
{
    [Area("Employee")]
    //[Authorize(Policy = "EmployeeOnly")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
