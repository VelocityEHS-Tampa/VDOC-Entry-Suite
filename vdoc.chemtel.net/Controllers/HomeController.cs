using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using vdoc.chemtel.net.Models;

namespace vdoc.chemtel.net.Controllers
{
    public class HomeController : Controller
    {
        string constring = Properties.Settings.Default.Connection; //Connection string to the database
        string username = System.Environment.UserName;
        string rootpath = @"\\chem-fs1.ers.local\Document_DB\Operators\" + System.Environment.UserName + @"\"; //Sets the path to the operators folder
        public ActionResult Index()
        {
            //Get Companies within Users folder
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
            ViewBag.Username = username;
            return View();
        }

        private void GetFolders()
        {
            //try
            //{
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
                //else
                //{
                //    MessageBox.Show("You do not currently have any Folder/Files assigned to you at the moment. Please check again later.", "VDoc Entry Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //}
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("An error has occured in this program.  The IT department has been notified of this error.", "Global Report Manager", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            //    string pathh = @"\\chem-fs1.ers.local\Log Files\vdoc.log";
            //    StreamWriter log;
            //    if (File.Exists(pathh))
            //        log = File.AppendText(pathh);
            //    else
            //        log = File.CreateText(pathh);
            //    string mod = "GetFolders";
            //    string pfile = "frmvesdataentry.cs";
            //    string user = System.Environment.UserName;
            //    log.WriteLine("Date: " + DateTime.Now.ToShortDateString() + "\n" + "Time: " + DateTime.Now.ToShortTimeString() + "\n" + "User: " + user + "\n" + "Error Message: " + ex.Message + "\n" + "File: " + pfile + "\n" + "Method: " + mod + "\n\n\n");
            //    log.Close();
            //    var smtpCreds = new NetworkCredential(@"CHEMTEL\emergency", "ERS*33602");
            //    SmtpClient smtp = new SmtpClient("mail.chemtelinc.com", 587);
            //    MailAddress from = new MailAddress("ers@ehs.com");
            //    MailAddressCollection to = new MailAddressCollection();
            //    MailMessage message = new MailMessage();
            //    smtp.UseDefaultCredentials = false;
            //    smtp.Credentials = smtpCreds;
            //    string msg = "Check the log file!";
            //    string subject = "Vdoc Error";
            //    to.Add("mpepitone@chemtelinc.com");
            //    message.To.Add(to.ToString());
            //    message.From = from;
            //    message.Subject = subject;
            //    message.Body = msg;
            //    smtp.Send(message);
            //}
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
                string reviewpath = @"\\chem-fs1.ers.local\Document_DB\Operators\Review\" + fc["SelectedCompany"].ToString() + @"\"; //Sets the destination folder
                MSDSReview m = new MSDSReview();
                m = GetData(fc); //Gets the data from the form
                if (!Directory.Exists(reviewpath)) { //Determines if the current company has a subfolder in the Review folder
                    Directory.CreateDirectory(reviewpath); //If not, it's created
                }
                string src = rootpath + fc["SelectedCompany"].ToString() + @"\" + fc["OldFileName"].ToString(); //Sets the source path
                string dest = reviewpath + fc["Filename"].ToString(); //Sets the destination path
                System.IO.File.Copy(src, dest, true);
                System.IO.File.Delete(src);
                AddRecord(constring, m);
                GetFiles(fc["SelectedCompany"].ToString(), 0);
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
                //string msg = "Check the log file!";
                //string subject = "Vdoc Error";
                //to.Add("mpepitone@chemtelinc.com");
                //message.To.Add(to.ToString());
                //message.From = from;
                //message.Subject = subject;
                //message.Body = msg;
                //smtp.Send(message);
            }
            return RedirectToAction("Index");
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
            m.Common_Name = fc["CommonName"].ToString();
            m.Username = username;
            m.Date_Entered = DateTime.Now;
            return m;
        }

        public void AddRecord(string constring, MSDSReview m)
        {
            string strsql = "INSERT INTO msdsreviewTest (Filename, Company, ProductName, CommonName, Manufacturer, ProductNumber, Date, MIS, Location, Dept, username, DateEntered, LastModified, Language) VALUES (@filename, @company, @productname, @commonname, @manufacturer, @productnumber, @date, @mis, @location, @dept, @username, @dateentered, @lastmodified, @language)";
            using (SqlConnection cn = new SqlConnection(constring))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(strsql, cn))
                {
                    cmd.Parameters.AddWithValue("@filename", m.Filename);
                    cmd.Parameters.AddWithValue("@company", m.Company);
                    cmd.Parameters.AddWithValue("@productname", m.Product_Name);
                    cmd.Parameters.AddWithValue("@commonname", m.Common_Name);
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