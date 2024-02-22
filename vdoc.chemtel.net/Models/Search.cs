using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace vdoc.chemtel.net.Models
{
    public class Search
    {
        public static List<string> GetLanguages(string constring)
        {
            List<string> lang = new List<string>();
            string strsql = "SELECT language FROM languages";
            using (SqlConnection cn = new SqlConnection(constring))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(strsql, cn))
                {
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                        lang.Add(rdr[0].ToString());
                }
            }
            return lang;
        }
    }
}