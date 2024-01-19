using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace vdoc.chemtel.net.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string DEFolder = @"\\chem-fs1.ers.local\Document_DB\Operators\mpepitone";
            List<string> Companies = new List<string>();
            foreach (string d in System.IO.Directory.GetDirectories(DEFolder))
            {
                Companies.Add(d.Split('\\')[6]); //Splitting the UNC path to get the company name at the end.
            }
            ViewBag.Companies = Companies;
            return View();
        }
    }
}