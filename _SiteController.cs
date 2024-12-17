using System.Web.Mvc;

namespace myDashboard.Controllers
{
    public class _SiteController : Controller
    {
        public ActionResult Index()
        {
            return null;
        }

        //[ChildActionOnly]
        public ActionResult LoadHTML(string page)
        {
            return new FilePathResult(page, "text/html");
        }
    }
}