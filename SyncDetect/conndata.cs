using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SyncDetect
{
    class conndata
    {
        XmlNode xmlNode;
        XmlDocument xmlDoc = new XmlDocument();
        FileStream xmllock;
        static private byte[] AESKey;

        // Create byte array for additional entropy when using Protect method.
        static byte[] s_additionalEntropy = { 155, 128, 32, 200, 12, 1, 64, 255, 0, 245, 33, 18, 16, 6, 90, 15 };

        public static void loadSafeKeys()
        {
            if (File.Exists("datastored"))
            {
                using (FileStream fs = new FileStream("dataentr", FileMode.Open))
                {
                    byte[] tmpIV = new byte[128];
                    int readed = fs.Read(tmpIV, 0, 128);

                    string ivb64str = Encoding.UTF8.GetString(tmpIV, 0, readed);
                    s_additionalEntropy = Convert.FromBase64String(ivb64str);
                }

                using (FileStream fs = new FileStream("datastored", FileMode.Open))
                {
                    byte[] encryptedSecret = new byte[fs.Length];

                    fs.Read(encryptedSecret, 0, (int)fs.Length);

                    AESKey = Unprotect(encryptedSecret);
                }
            }
            else
            {
                using (FileStream fs = new FileStream("datastored", FileMode.Create))
                {
                    using (Aes aesAlg = Aes.Create())
                    {
                        aesAlg.GenerateKey();
                        aesAlg.GenerateIV();

                        using (FileStream fsentrop = new FileStream("dataentr", FileMode.OpenOrCreate))
                        {
                            fsentrop.SetLength(0);
                            string ivb64 = Convert.ToBase64String(aesAlg.IV);
                            byte[] writebytes = Encoding.UTF8.GetBytes(ivb64);
                            fsentrop.Write(writebytes, 0, writebytes.Length);
                        }

                        AESKey = (byte[])aesAlg.Key.Clone();

                        PrintValues(AESKey);
                        byte[] encryptedSecret = Protect(AESKey);
                        fs.Write(encryptedSecret, 0, encryptedSecret.Length);
                    }
                }
            }
        }

        public static byte[] Protect(byte[] data)
        {
            try
            {
                // Encrypt the data using DataProtectionScope.CurrentUser. The result can be decrypted
                // only by the same current user.
                return ProtectedData.Protect(data, s_additionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Data was not encrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public static byte[] Unprotect(byte[] data)
        {
            try
            {
                //Decrypt the data using DataProtectionScope.CurrentUser.
                return ProtectedData.Unprotect(data, s_additionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Data was not decrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public static void PrintValues(Byte[] myArr)
        {
            foreach (Byte i in myArr)
            {
                Console.Write("\t{0}", i);
            }
            Console.WriteLine();
        }

        public static String WildCardToRegular(String value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }

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
            xmllock = File.Open("db.xml", FileMode.Open, FileAccess.Read, FileShare.None);

            loadSafeKeys();
            // xmlList[0].InnerText;
        }

        public static string XmlNodeContentB64(XmlNode node)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(node.InnerText));
        }

        public static string XmlNdSNodeContentB64(XmlNode nd, string field)
        {
            XmlNode idnode = nd.SelectSingleNode(field);
            return Encoding.UTF8.GetString(Convert.FromBase64String(idnode.InnerText));
        }

        public static string XmlGetSNodeContentB64(XmlDocument doc, string field)
        {
            XmlNode idnode = doc.DocumentElement.SelectSingleNode(field);
            return Encoding.UTF8.GetString(Convert.FromBase64String(idnode.InnerText));
        }

        public static string XmlGetElSNodeContentB64(XmlElement e, string field)
        {
            XmlNode idnode = e.SelectSingleNode(field);
            return Encoding.UTF8.GetString(Convert.FromBase64String(idnode.InnerText));
        }

        public static void XmlAppendNode(XmlDocument doc, XmlElement e, string name, string value)
        {
            XmlNode newElem = doc.CreateNode("element", name, "");
            newElem.InnerText = value == null ? "" : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

            e.AppendChild(newElem);
        }

        public static XmlDocument readEncryptedXML(string file, out byte[] IV)
        {
            XmlDocument doc = new XmlDocument();

            IV = new byte[16];
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = AESKey;

                using (FileStream fs = new FileStream(file + ".iv", FileMode.Open))
                {
                    byte[] tmpIV = new byte[32];
                    int readed = fs.Read(tmpIV, 0, 32);

                    string ivb64str = Encoding.UTF8.GetString(tmpIV, 0, readed);
                    aesAlg.IV = Convert.FromBase64String(ivb64str);
                    IV = aesAlg.IV;
                }

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);


                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (FileStream fsenc = new FileStream(file, FileMode.Open))
                    {
                        fsenc.CopyTo(msDecrypt);
                        msDecrypt.Seek(0, SeekOrigin.Begin);

                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                string xmltext = srDecrypt.ReadToEnd();

                                doc.LoadXml(xmltext);
                            }
                        }
                    }
                }
            }

            return doc;
        }

        public static void writeEncryptedXML(string file, byte[] IV, XmlDocument xdoc)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = AESKey;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (FileStream fs = new FileStream(file + ".iv", FileMode.OpenOrCreate))
                {
                    fs.SetLength(0);
                    string ivb64 = Convert.ToBase64String(aesAlg.IV);
                    byte[] writebytes = Encoding.UTF8.GetBytes(ivb64);
                    fs.Write(writebytes, 0, writebytes.Length);
                }

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(xdoc.OuterXml);
                        }
                    }

                    using (FileStream fs = new FileStream(file, FileMode.OpenOrCreate))
                    {
                        fs.SetLength(0);
                        byte[] writebytes = msEncrypt.ToArray();
                        fs.Write(writebytes, 0, writebytes.Length);
                    }
                }
            }
        }
    }
}
