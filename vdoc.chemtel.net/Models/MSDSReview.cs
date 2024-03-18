using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace vdoc.chemtel.net.Models
{
    public class MSDSReview
    {
        #region Private Fields

        private int _id;
        private string _filename, _company, _productname, _manufacturer, _productnumber, _date, _mis, _location, _dept, _username, _lastmodified, _language;
        private DateTime _dateentered;

        #endregion
        #region Public Properties
        #region Ints

        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }

        #endregion
        #region Datetimes

        public DateTime Date_Entered
        {
            get { return _dateentered; }
            set { _dateentered = value; }
        }

        #endregion
        #region Strings

        public string Filename
        {
            get { return _filename; }
            set { _filename = value; }
        }

        public string Company
        {
            get { return _company; }
            set { _company = value; }
        }

        public string Product_Name
        {
            get { return _productname; }
            set { _productname = value; }
        }

        public string Manufacturer
        {
            get { return _manufacturer; }
            set { _manufacturer = value; }
        }

        public string Product_Number
        {
            get { return _productnumber; }
            set { _productnumber = value; }
        }

        public string Date
        {
            get { return _date; }
            set { _date = value; }
        }

        public string MIS
        {
            get { return _mis; }
            set { _mis = value; }
        }

        public string Location
        {
            get { return _location; }
            set { _location = value; }
        }

        public string Department
        {
            get { return _dept; }
            set { _dept = value; }
        }

        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public string Last_Modified
        {
            get { return _lastmodified; }
            set { _lastmodified = value; }
        }

        public string Language
        {
            get { return _language; }
            set { _language = value; }
        }

        #endregion
        #endregion
        #region Public Constructors

        public MSDSReview()
        {
            _id = 0;
            _dateentered = DateTime.MinValue;
            _filename = "";
            _company = "";
            _productname = "";
            _manufacturer = "";
            _productnumber = "";
            _date = "";
            _mis = "";
            _location = "";
            _dept = "";
            _username = "";
            _lastmodified = "";
            _language = "";
        }

        #endregion
        #region Add Method


        #endregion
    }
}