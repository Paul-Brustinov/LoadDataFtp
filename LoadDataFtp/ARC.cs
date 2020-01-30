using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadDataFtp
{
    public static class ARC
    {
        public static void StoreToArc(string prefix, string inFolder, string FileName)
        {
            string unZipFileName = FileName.Replace(".zip", "");
            string ArcFileName = GetArcFileName(prefix, unZipFileName);

            if (ArcFileName == "") return;  // Если файл не импортирован, выходим

            if (FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using (ZipArchive archive = ZipFile.OpenRead(inFolder + FileName))
                { 
                    foreach (ZipArchiveEntry fromEntry in archive.Entries)
                    {
                        using (FileStream zipTo = new FileStream(inFolder + @"ARC\" + ArcFileName, FileMode.OpenOrCreate))
                        {
                            using (ZipArchive arcTo = new ZipArchive(zipTo, ZipArchiveMode.Update))
                            {
                                ZipArchiveEntry ToEntry = arcTo.CreateEntry(unZipFileName);
                                using (var inputS = fromEntry.Open())
                                {
                                    inputS.CopyTo(ToEntry.Open());
                                }
                            }
                        }
                    }
                }
                File.Delete(inFolder + FileName);
            }
            else
            {
                using (FileStream inputS = new FileStream(inFolder + FileName, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream zipTo = new FileStream(inFolder + @"ARC\" + ArcFileName, FileMode.OpenOrCreate))
                    {
                        using (ZipArchive arcTo = new ZipArchive(zipTo, ZipArchiveMode.Update))
                        {
                            ZipArchiveEntry ToEntry = arcTo.CreateEntry(unZipFileName);
                            inputS.CopyTo(ToEntry.Open());
                        }
                    }
                }
                File.Delete(inFolder + FileName);
            }
        }
    
        private static string GetArcFileName(string prefix, string fileName)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ImpCnn"].ConnectionString;
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = connectionString;
                conn.Open();
                using (var command = new SqlCommand("[dbo].[PointImport_GetFileDate]", conn)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    command.Parameters.Add("@DOC_FILE_NAME", SqlDbType.NVarChar).Value = fileName;
                    command.Parameters.Add("@Prefix", SqlDbType.NVarChar).Value = prefix;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read()) return reader["FILE_NAME"].ToString() + ".zip";
                        return "";
                    }
                }
            }
        }
    }
}