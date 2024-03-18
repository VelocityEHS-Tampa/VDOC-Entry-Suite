﻿using SendGrid.Helpers.Mail;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using vdoc.chemtel.net.Models;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Mail;

namespace vdoc.chemtel.net.Controllers
{
    public class HomeController : Controller
    {
        string constring = Properties.Settings.Default.Connection; //Connection string to the database
        string Userconstring = Properties.Settings.Default.UserConnection; //Connection string to the database

        #region Login Functions
        public ActionResult Login(FormCollection fc)
        {

            string username = fc["username"].ToString();
            string password = fc["password"].ToString();
            //Throw error if missing field.
            if (username == "" || password == "")
            {
                ViewBag.ErrorMessage = "Invalid Username/Password combination";
                return View();
            }

            //Start validation process
            string LoginValidated = ValidatePass(username, password);

            if (LoginValidated == "Yes")
            {
                Session.Add("SessionStartTime", DateTime.Now);
                GetAcctInfo(username);
                if (password == "Password1" || Int32.Parse(Session["DaysBetween"].ToString()) >= 90)
                {
                    if (username == "ctaylor") //correct users username in order to access the Operator folder.
                    {
                        username = "cataylor";
                    }
                    Session.Add("Username", username);
                    return View("ResetPassword");
                }
                else
                {
                    ViewBag.ErrorMessage = "Successful Login!";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid Username/Password combination";
                return View();
            }
        }
        public ActionResult ResetPassword()
        {
            return View();
        }
        public ActionResult PasswordChangeResults(FormCollection fc)
        {
            string username = Session["username"].ToString();
            string password = fc["NewPassword"].ToString();
            string SqlCmd = "UPDATE CRMWebU SET Password = @p, LastPassChange = @lpc WHERE Username = @un";

            string EncPass = PassEncrypt.EncodePassword(password);

            using (SqlConnection con = new SqlConnection(Userconstring))
            {
                using (SqlCommand cmd = new SqlCommand(SqlCmd, con))
                {
                    con.Open();
                    cmd.Parameters.AddWithValue("@p", EncPass);
                    cmd.Parameters.AddWithValue("@un", username);
                    cmd.Parameters.AddWithValue("@lpc", DateTime.Now.ToShortDateString());
                    cmd.ExecuteNonQuery();
                }
            }
            GetAcctInfo(username);
            return RedirectToAction("Index");
        }
        public ActionResult Logout()
        {
            Session["Username"] = null;
            Session["FullName"] = null;
            Session["AccountType"] = null;
            return RedirectToAction("Index");
        }
        private string ValidatePass(string username, string password)
        {
            string Validated = "No";
            string DBPass = "";
            string SqlCmd = "SELECT Password FROM CRMWebU WHERE username = @username";

            using (SqlConnection con = new SqlConnection(Userconstring))
            {
                using (SqlCommand cmd = new SqlCommand(SqlCmd, con))
                {
                    con.Open();
                    cmd.Parameters.AddWithValue("@username", username);
                    DBPass = cmd.ExecuteScalar().ToString();
                }
            }

            string EncPass = PassEncrypt.EncodePassword(password);

            if (EncPass == DBPass)
            {
                Validated = "Yes";
            }

            return Validated;
        }
        private void GetAcctInfo(string username)
        {
            string SqlCmd = "Select * FROM CRMWebU WHERE Username = @username";
            using (SqlConnection con = new SqlConnection(Userconstring))
            {
                using (SqlCommand cmd = new SqlCommand(SqlCmd, con))
                {
                    con.Open();
                    cmd.Parameters.AddWithValue("@username", username);
                    SqlDataReader sdr = cmd.ExecuteReader();
                    while (sdr.Read())
                    {
                        Session.Add("Username", sdr["Username"].ToString());
                        Session.Add("FullName", sdr["Name"].ToString());
                        Session.Add("AccountType", sdr["UserType"].ToString());
                        Session.Add("LastPassChange", sdr["LastPassChange"].ToString());
                        //Get how many days it has been since the last password change.
                        TimeSpan ts = DateTime.Now - DateTime.Parse(sdr["LastPassChange"].ToString());
                        Session.Add("DaysBetween", Math.Round(ts.TotalDays));
                    }
                }
            }
        }
        #endregion


        public ActionResult Index()
        {
            if (Session["Username"] == null || Session["Username"].ToString() == "")
            {
            } else
            {
                //Get Companies within Users folder
                string rootpath = @"\\chem-fs1.ers.local\Document_DB\Operators\" + Session["username"].ToString() + @"\"; //Sets the path to the operators folder
                List<string> Companies = new List<string>();
                foreach (string d in Directory.GetDirectories(rootpath))
                {
                    Companies.Add(d.Split('\\')[6]); //Splitting the UNC path to get the company name at the end.
                }
                ViewBag.Companies = Companies;
            
                //Get SDS Languages
                List<string> lang = new List<string>();
                lang = GetLanguages(); //Gets the list of languages for thelanguage combobox
                ViewBag.Languages = lang;
            }
            return View();
        }

        private void GetFolders()
        {
            string rootpath = @"\\chem-fs1.ers.local\Document_DB\Operators\" + Session["username"].ToString() + @"\"; //Sets the path to the operators folder
            string[] files = Directory.GetDirectories(rootpath);
            List<string> cbcompanies = new List<string>();
            if (files.Length > 0)
            {
                foreach (string file in files)
                {
                    string[] ar = file.Split('\\');
                    cbcompanies.Add(ar[ar.Length - 1]);
                }
            }
        }

        private List<string> GetLanguages()
        {
            List<string> lang = new List<string>();
            lang = Search.GetLanguages(constring);
            return lang;
        }

        [HttpGet]
        public JsonResult GetFiles(string SelectedCompany, int FileIndex) //Gets the selected company's files to be processed
        {
            Session.Add("CompanySelected", SelectedCompany);
            string rootpath = @"\\chem-fs1.ers.local\Document_DB\Operators\" + Session["username"].ToString() + @"\"; //Sets the path to the operators folder
            string FileList = "";
            if (Directory.GetFiles(rootpath + "\\" + SelectedCompany).Length != 0)
            {
                FileList = Directory.GetFiles(rootpath + "\\" + SelectedCompany)[FileIndex];
                FileList = Path.GetFileName(FileList);
            }
            else
            {
                FileList = "";
            }
            return Json(new { FileList = FileList }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult CheckForDuplicates(string Filename, string CompanySelected)
        {
            string reviewpath = @"\\chem-fs1.ers.local\Document_DB\Operators\Review\" + CompanySelected + @"\" + Filename; //Sets the destination folder
            string HasDuplicate = "0";
            try
            {
                if (System.IO.File.Exists(reviewpath))
                {
                    HasDuplicate = "1"; // If there is a duplicate file name, warn user.
                }
            } catch(Exception ex) {
                string pathh = @"\\chem-fs1.ers.local\Log Files\vdoc.log";
                StreamWriter log;
                if (System.IO.File.Exists(pathh))
                    log = System.IO.File.AppendText(pathh);
                else
                    log = System.IO.File.CreateText(pathh);
                string mod = "CheckForDuplicates";
                string pfile = "HomeController.cs";
                string user = System.Environment.UserName;
                log.WriteLine("Date: " + DateTime.Now.ToShortDateString() + "\n" + "Time: " + DateTime.Now.ToShortTimeString() + "\n" + "User: " + user + "\n" + "Error Message: " + ex.Message + "\n" + "File: " + pfile + "\n" + "Method: " + mod + "\n\n\n");
                log.Close();
            }
            return Json(new { HasDuplicate = HasDuplicate }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SubmitNewFile(FormCollection fc)
        {
            try
            {
                string reviewpath = @"\\chem-fs1.ers.local\Document_DB\Operators\Review\" + Session["CompanySelected"].ToString() + @"\"; //Sets the destination folder
                string rootpath = @"\\chem-fs1.ers.local\Document_DB\Operators\" + Session["username"].ToString() + @"\"; //Sets the path to the operators folder
                MSDSReview m = new MSDSReview();
                m = GetData(fc); //Gets the data from the form
                if (!Directory.Exists(reviewpath)) { //Determines if the current company has a subfolder in the Review folder
                    Directory.CreateDirectory(reviewpath); //If not, it's created
                }
                string src = rootpath + Session["CompanySelected"].ToString() + @"\" + fc["OldFileName"].ToString(); //Sets the source path
                string dest = reviewpath + fc["Filename"].ToString(); //Sets the destination path
                System.IO.File.Copy(src, dest, true);
                System.IO.File.Delete(src);
                AddRecord(constring, m);
            }
            catch (Exception ex)
            {
                string pathh = @"\\chem-fs1.ers.local\Log Files\vdoc.log";
                StreamWriter log;
                if (System.IO.File.Exists(pathh))
                    log = System.IO.File.AppendText(pathh);
                else
                    log = System.IO.File.CreateText(pathh);
                string mod = "btnok_Click";
                string pfile = "frmvesdataentry.cs";
                string user = System.Environment.UserName;
                log.WriteLine("Date: " + DateTime.Now.ToShortDateString() + "\n" + "Time: " + DateTime.Now.ToShortTimeString() + "\n" + "User: " + user + "\n" + "Error Message: " + ex.Message + "\n" + "File: " + pfile + "\n" + "Method: " + mod + "\n\n\n");
                log.Close();
                return RedirectToAction("Error", "Home", new { ErrorMessage = ex.Message });
            }
            return RedirectToAction("Index", new {FileSubmitted = "Success" });
        }
        [HttpGet]
        public JsonResult FormatFilename(string Manufacturer, string ProductName, string ProductNumber, string Language, string Date)
        {
            string FortmattedFileName = "";
            if (Manufacturer != "")
                FortmattedFileName = Manufacturer;
            if (ProductName != "")
            {
                if (ProductName.Contains(','))
                {
                    string[] ar = ProductName.Split(',');
                    if (ar.Length > 0)
                    {
                        FortmattedFileName += "_" + ar[0];
                    }
                    else
                    {
                        FortmattedFileName += "_" + ProductName;
                    }
                }
                else
                {
                    FortmattedFileName += "_" + ProductName;
                }
            }
            if (ProductNumber != "")
            {
                if (ProductNumber.Contains(' '))
                {

                    string[] ar = ProductNumber.Split();
                    if (ar.Length > 1)
                    {
                        FortmattedFileName += "_" + ar[0] + "-" + ar[1];
                    }
                    else
                    {
                        FortmattedFileName += "_" + ProductNumber;
                    }
                }
                else
                {
                    FortmattedFileName += "_" + ProductNumber;
                }
            }
            //if (txtdate.Text != "  /  /")
            //{
            //    string[] ar = txtdate.Text.Split('/');
            //    string date =ar[0]+"-"+ar[1]+"-"+ar[2];
            FortmattedFileName += "_" + Date + "_" + Language + ".pdf";
            //}
            return Json(new { NewFileName = FortmattedFileName }, JsonRequestBehavior.AllowGet);
        }
        //Gets the data from the form
        private MSDSReview GetData(FormCollection fc)
        {
            MSDSReview m = new MSDSReview();
            m.Filename = fc["Filename"].ToString();
            m.Company = fc["CompanyName"].ToString();
            m.Manufacturer = fc["Manufacturer"].ToString();
            m.Product_Name = fc["ProductName"].ToString();
            m.Language = fc["Language"].ToString();
            m.Product_Number = fc["ProductNumber"].ToString();
            m.Date = fc["Date"].ToString();
            m.MIS = fc["CompanyMIS"].ToString();
            m.Location = fc["Location"].ToString();
            m.Department = fc["Department"].ToString();
            m.Username = Session["username"].ToString();
            m.Date_Entered = DateTime.Now;
            return m;
        }

        public void AddRecord(string constring, MSDSReview m)
        {
            string strsql = "INSERT INTO msdsreview (Filename, Company, ProductName, Manufacturer, ProductNumber, Date, MIS, Location, Dept, username, DateEntered, LastModified, Language) VALUES (@filename, @company, @productname, @manufacturer, @productnumber, @date, @mis, @location, @dept, @username, @dateentered, @lastmodified, @language)";
            using (SqlConnection cn = new SqlConnection(constring))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(strsql, cn))
                {
                    cmd.Parameters.AddWithValue("@filename", m.Filename);
                    cmd.Parameters.AddWithValue("@company", m.Company);
                    cmd.Parameters.AddWithValue("@productname", m.Product_Name);
                    cmd.Parameters.AddWithValue("@manufacturer", m.Manufacturer);
                    cmd.Parameters.AddWithValue("@productnumber", m.Product_Number);
                    cmd.Parameters.AddWithValue("@date", m.Date);
                    cmd.Parameters.AddWithValue("@mis", m.MIS);
                    cmd.Parameters.AddWithValue("@location", m.Location);
                    cmd.Parameters.AddWithValue("@dept", m.Department);
                    cmd.Parameters.AddWithValue("@username", m.Username);
                    cmd.Parameters.AddWithValue("@dateentered", m.Date_Entered);
                    cmd.Parameters.AddWithValue("@lastmodified", m.Last_Modified);
                    cmd.Parameters.AddWithValue("@language", m.Language);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}