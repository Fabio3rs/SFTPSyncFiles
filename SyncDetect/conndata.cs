using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Data;

namespace SyncDetect
{
    class conndata
    {
        XmlNode xmlNode;
        XmlDocument xmlDoc = new XmlDocument();

        public void FillDataTable(ref DataTable dt)
        {
            XmlNodeList svrlist = xmlDoc.DocumentElement.SelectNodes("Server");

            foreach (XmlNode nd in svrlist)
            {
                XmlNodeList svraddress = nd.SelectNodes("Svraddr");
                XmlNodeList svrport = nd.SelectNodes("Svrport");
                XmlNodeList svruser = nd.SelectNodes("Svruser");
                XmlNodeList svrpasswd = nd.SelectNodes("Svrpasswd");
                XmlNodeList svrdir = nd.SelectNodes("Svrdirectory");
                XmlNodeList localdir = nd.SelectNodes("Localdirectory");

                DataRow dr = dt.Rows.Add();
                dr["id"] = 1;
                dr["sourced"] = localdir[0].InnerText;
                dr["targetd"] = svrdir[0].InnerText;
                dr["server"] = svraddress[0].InnerText;
                dr["port"] = Convert.ToInt32(svrport[0].InnerText);
                dr["user"] = svruser[0].InnerText;
                dr["passwd"] = svrpasswd[0].InnerText;
            }
        }

        public void get(DataRow dr)
        {
            xmlNode = xmlDoc.DocumentElement.SelectSingleNode("Server");

            XmlNodeList svraddress = xmlNode.SelectNodes("Svraddr");
            XmlNodeList svrport = xmlNode.SelectNodes("Svrport");
            XmlNodeList svruser = xmlNode.SelectNodes("Svruser");
            XmlNodeList svrpasswd = xmlNode.SelectNodes("Svrpasswd");
            XmlNodeList svrdir = xmlNode.SelectNodes("Svrdirectory");
            XmlNodeList localdir = xmlNode.SelectNodes("Localdirectory");

            dr["id"] = 1;
            dr["sourced"] = localdir[0].InnerText;
            dr["targetd"] = svrdir[0].InnerText;
            dr["server"] = svraddress[0].InnerText;
            dr["port"] = Convert.ToInt32(svrport[0].InnerText);
            dr["user"] = svruser[0].InnerText;
            dr["passwd"] = svrpasswd[0].InnerText;
        }

        public conndata()
        {
            xmlDoc.Load("db.xml");

            // xmlList[0].InnerText;
        }

    }
}
