using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using static LoadDataFtp.FTP;
using System.Xml;
using System.Configuration;

namespace LoadDataFtp
{
    class Program
    {
        static void Main(string[] args)
        {
            string ftpUrl = ConfigurationManager.AppSettings["ftpUrl"];
            string login = ConfigurationManager.AppSettings["login"];
            string pass = ConfigurationManager.AppSettings["pass"];
            string inFolder = ConfigurationManager.AppSettings["inFolder"];

            if (args[0] == "LoadFromFtp"        ) LoadFromFtp(ftpUrl, login, pass, inFolder);
            if (args[0] == "LoadFromFolder"     ) LoadFromFolder(inFolder, false); // Load if not exists
            if (args[0] == "ReLoadFromFolder"   ) LoadFromFolder(inFolder, true);// Force reload
            if (args[0] == "PackFolder"         ) PackFolder(inFolder);


        }
        public static Dictionary<string, string> LoadFromFtp(string ftpUrl, string login, string pass, string inFolder)
        {
            Log.WriteMessage("LoadFromFtp started-----------------------------------------------------------------");

            var fTP = new FTP(ftpUrl, "", login, pass, false);
            Log.WriteMessage("getting fTP.ListDirectory");
            FileStr[] listFtpDirectory = fTP.ListDirectory("");


            //List<string> filesInFolder = Directory.GetFiles(inFolder).ToList();
            Log.WriteMessage("getting filesInDb");
            var filesInDb = Loader.GetImportedFilesFromDb().Values.ToList();

            //filesInFolder = filesInFolder.Select(i => i.Replace(inFolder, "")).ToList();

            var filteredList = listFtpDirectory.Where(i => !filesInDb.Contains(i.Name)).ToList();

            var files = new Dictionary<string, string>();

            Log.WriteMessage("Starting import...");
            foreach (var f in filteredList)
            {
                try
                {
                    fTP.DownloadFile(f.Name, inFolder);

                    files.Add(f.Name, "");
                }
                catch (Exception e)
                {
                    LoadException le = new LoadException() { e = e };
                    le.Extrainfo = f.Name;
                    Log.WriteError(le);
                    Console.WriteLine(string.Format("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}.{2}()] {3} {4}\r\n", DateTime.Now, le.e.TargetSite.DeclaringType, le.e.TargetSite.Name, le.e.Message, le.Extrainfo));
                }

                if (f.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (ZipArchive archive = ZipFile.OpenRead(inFolder + f.Name))
                        {
                            foreach (ZipArchiveEntry fromEntry in archive.Entries)
                            {
                                using (StreamReader reader = new StreamReader(fromEntry.Open(), Encoding.GetEncoding(1251)))
                                {
                                    XmlDocument doc = new XmlDocument();
                                    doc.Load(reader);
                                    Loader.Load(doc, fromEntry.FullName);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LoadException le = new LoadException() { e = e };
                        le.Extrainfo = f.Name;
                        Log.WriteError(le);
                        Console.WriteLine(string.Format("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}.{2}()] {3} {4}\r\n", DateTime.Now, le.e.TargetSite.DeclaringType, le.e.TargetSite.Name, le.e.Message, le.Extrainfo));
                    }
                }
                else
                {
                    try
                    {
                        using (FileStream reader = new FileStream(inFolder + f.Name, FileMode.Open, FileAccess.Read))
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(reader);
                            Loader.Load(doc, f.Name);
                        }
                    }
                    catch (Exception e)
                    {
                        LoadException le = new LoadException() { e = e };
                        le.Extrainfo = f.Name;
                        Log.WriteError(le);
                        Console.WriteLine(string.Format("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}.{2}()] {3} {4}\r\n", DateTime.Now, le.e.TargetSite.DeclaringType, le.e.TargetSite.Name, le.e.Message, le.Extrainfo));
                    }
                }

                Console.WriteLine(f.Name);
                if (f.Name.StartsWith("DOC_"))
                {
                    ARC.StoreToArc("DOC_", inFolder, f.Name);
                }
                else if (f.Name.StartsWith("Z_REPORT_"))
                {
                    ARC.StoreToArc("Z_REPORT_", inFolder, f.Name);
                }
                else
                {
                    ARC.StoreToArc("", inFolder, f.Name);
                }
            }
            Log.WriteMessage(string.Format("Imported {0} files----------------------------", filteredList.Count));
            return files;
        }

        public static Dictionary<string, string> LoadFromFolder(string inFolder, bool reLoad)
        {
            Log.WriteMessage("LoadFromFolder started");

            Log.WriteMessage("getting ListDirectory");
            var directoryFiles = Directory.GetFiles(inFolder).ToList();

            Log.WriteMessage("getting filesInDb");
            var filesInDb = Loader.GetImportedFilesFromDb().Values.Select(x => x.Replace(".zip", "")).ToList();

            var files = new Dictionary<string, string>();

            foreach (var f in directoryFiles)
            {
                if (f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using (ZipArchive archive = ZipFile.OpenRead(f))
                    {
                        foreach (ZipArchiveEntry fromEntry in archive.Entries)
                        {
                            if (!filesInDb.Contains(fromEntry.Name) || reLoad)
                            {
                                if (!files.ContainsKey(fromEntry.Name)) files.Add(fromEntry.Name, "");
                                Console.WriteLine(String.Format("Importing {0}", fromEntry.Name));
                                //Log.WriteMessage(String.Format("Importing {0}", fromEntry.Name));

                                try
                                {
                                    using (StreamReader reader = new StreamReader(fromEntry.Open(), Encoding.GetEncoding(1251)))
                                    {
                                        XmlDocument doc = new XmlDocument();
                                        doc.Load(reader);
                                        Loader.Load(doc, fromEntry.FullName);
                                    }
                                }
                                catch (Exception e)
                                {
                                    LoadException le = new LoadException() { e = e };
                                    le.Extrainfo = fromEntry.FullName;
                                    Log.WriteError(le);
                                    Console.WriteLine(string.Format("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}.{2}()] {3} {4}\r\n", DateTime.Now, le.e.TargetSite.DeclaringType, le.e.TargetSite.Name, le.e.Message, le.Extrainfo));
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (!filesInDb.Contains(f) || reLoad)
                    {
                        files.Add(f, "");
                        try
                        {
                            using (FileStream reader = new FileStream(f, FileMode.Open, FileAccess.Read))
                            {
                                Console.WriteLine(String.Format("Importing {0}", f));
                                //Log.WriteMessage(String.Format("Importing {0}", f));
                                XmlDocument doc = new XmlDocument();
                                doc.Load(reader);
                                Loader.Load(doc, Path.GetFileName(f));
                            }
                        }
                        catch (Exception e)
                        {
                            LoadException le = new LoadException() { e = e };
                            le.Extrainfo = f;
                            Log.WriteError(le);
                            Console.WriteLine(string.Format("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}.{2}()] {3} {4}\r\n", DateTime.Now, le.e.TargetSite.DeclaringType, le.e.TargetSite.Name, le.e.Message, le.Extrainfo));
                        }
                    }
                }
                Console.WriteLine(f);
            }

            return files;
        }


        public static Dictionary<string, string> PackFolder(string inFolder)
        {
            Log.WriteMessage("PackFolder started");

            Log.WriteMessage("getting ListDirectory");
            var directoryFiles = Directory.GetFiles(inFolder).Select((f) => Path.GetFileName(f)).ToList();
            foreach (var f in directoryFiles)
            {
                if (f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (f.StartsWith("DOC_"))
                        {
                            ARC.StoreToArc("DOC_", inFolder, f);
                        }
                        else if (f.StartsWith("Z_REPORT_"))
                        {
                            ARC.StoreToArc("Z_REPORT_", inFolder, f);
                        }
                        else
                        {
                            ARC.StoreToArc("", inFolder, f);
                        }
                        Console.WriteLine(f);
                    }
                    catch (Exception e)
                    {
                        LoadException le = new LoadException() { e = e };
                        le.Extrainfo = f;
                        Log.WriteError(le);
                        Console.WriteLine(string.Format("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}.{2}()] {3} {4}\r\n", DateTime.Now, le.e.TargetSite.DeclaringType, le.e.TargetSite.Name, le.e.Message, le.Extrainfo));
                    }
                }
            }
            return new Dictionary<string, string>();
        }
    }
}
