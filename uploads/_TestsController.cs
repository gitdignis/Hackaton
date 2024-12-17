using myDashboard.Managers;
using myDashboard.Models.Contexts;
using myDashboard.Models.Data;
using myDashboard.Models.Databases;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data;
//using System.Linq.Dynamic;

namespace myDashboard.Controllers
{
    public class _TestsController : _Controller
    {
        private readonly InstancesManager managerInstances = new InstancesManager();

        // GET: _Tests
        //[AuthorizeRoles("Administrator")]
        public ActionResult Leaflet()
        {
            return View();
        }
        //[AuthorizeRoles("Administrator")]
		public ActionResult Outlook()
        {
            return View();
        }
        //[AuthorizeRoles("Administrator")]
        public ActionResult TeamworkFont()
        {
            return View("TeamworkFont");
        }
        //[AuthorizeRoles("Administrator")]
        public ActionResult CloseLT()
		{
			return View();
		}
        //[AuthorizeRoles("Administrator")]
        public ActionResult TagIT()
        {
            return View("excel");
        }
        public ActionResult fancytree()
        {
            return View("fancytree");
        }

        public ActionResult MyTreeInstances_LoadData(string opportunity_id) => _TreeInstances_LoadData(UserContext.ContextType.My, opportunity_id);

        protected ActionResult _TreeInstances_LoadData(
            UserContext.ContextType context,
            string opportunity_id
        )
        {
            int totalRecords = 0;

            IQueryable<instance> results = managerInstances.QueryData(
                // filters
                null,
                opportunity_id,
                null,
                null,
                null,

                // sort
                null,

                // user context
                UserContext.Impersonate(context, User.Identity.Name),

                // returned results
                out totalRecords
            );


            List<TreeNodeView> view = (
                from r in results
                group r by new {
                    r.Site__Id_,
                    r.Address_Full
                } into s
                select new TreeNodeView
                {
                    title = s.Key.Address_Full,
                    type = "book",
                    data = new TreeNodeDataView
                    {
                        Site_id = s.Key.Site__Id_
                    },
                    folder = true
                }
            ).ToList();

            foreach (TreeNodeView n in  view)
            {
                n.children = (
                    from r
                    in results
                    where r.Site__Id_ == n.data.Site_id
                    select new TreeNodeView
                    {
                        title = r.Instance_Reference,
                        type = "computer",
                        data = new TreeNodeDataView
                        {
                            Site_id = r.Site__Id_
                        },
                        folder = false
                    }
                ).ToList();
            };

            var j =  JsonObject(view);

            return j;
        }


    }
}