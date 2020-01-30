using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LoadDataFtp
{
    public static class Loader
    {
        static string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ImpCnn"].ConnectionString;

        public static bool Load(XmlDocument doc, string name)
        {
            if (name.StartsWith("DOC_"))
            {
                return LoadReceipt(doc, name);
            }
            if (name.StartsWith("Z_REPORT_"))
            {
                return LoadZReport(doc, name);
            }
            return true;
        }

        private static bool LoadReceipt(XmlDocument doc, string name)
        {
            var dctDocs = ParseRecipiesToDict(doc, name);
            SaveRecepiesToDb(dctDocs);

            return true;
        }
        private static bool LoadZReport(XmlDocument doc, string name)
        {
            var dctReps = ParseZReportsToDict(doc, name);
            SaveZReportsToDb(dctReps);
            return true;
        }
        private static IDictionary<Guid, Receipt> ParseRecipiesToDict(XmlDocument doc, string name)
        {
            XmlNodeList colXML = doc.GetElementsByTagName("Document");
            var recDict = new Dictionary<Guid, Receipt>();
            foreach (XmlElement el in colXML)
            {
                var rec = new Receipt() { Details = new Dictionary<long, RecDetail>() };
                rec.FileName = name;
                foreach (XmlElement c in el.ChildNodes)
                {
                    switch (c.Name)
                    {
                        case "Id":
                            rec.IdXml = Convert.ToInt64(c.InnerXml);
                            break;
                        case "DocumentNumber":
                            rec.DocumentNumber = c.InnerXml;
                            break;
                        case "DocumentGuid":
                            rec.DocumentGuid = Guid.ParseExact(c.InnerXml, "B");
                            break;
                        case "SupportingDocument":
                            rec.Memo = c.InnerXml;
                            break;
                        case "DateOfCreate":
                            rec.DocDate = Convert.ToDateTime(c.InnerXml);
                            break;
                        case "DateOfApprove":
                            rec.DateOfApprove = Convert.ToDateTime(c.InnerXml);
                            break;
                        case "Amount":
                            rec.DocSum = StringToDecimal(c.InnerXml);
                            break;
                        case "DocumentType":
                            rec.DocType = Convert.ToInt64(c.InnerXml);
                            break;
                        case "DepartmentId":
                            rec.AgID = Convert.ToInt64(c.InnerXml);
                            break;
                        case "FranchiseContractorId":
                            rec.FranchiseContractorId = Convert.ToInt64(c.InnerXml);
                            break;
                        case "UserId":
                            rec.UserId = Convert.ToInt64(c.InnerXml);
                            break;
                        case "UserName":
                            rec.UserName = c.InnerXml;
                            break;
                        case "BonusPaid":
                            rec.BounsPaid = StringToDecimal(c.InnerXml);
                            break;
                        case "BonusPaymentRecordId":
                            rec.BounsPaidId = Convert.ToInt64(c.InnerXml);
                            break;
                        case "Detail":
                            rec.Details = ParseRecipsDetails(c);
                            break;
                    }
                }
                recDict.Add(rec.DocumentGuid, rec);
            }
            return recDict;
        }

        private static IDictionary<long, RecDetail> ParseRecipsDetails(XmlElement doc)
        {
            var dctDetails = new Dictionary<long, RecDetail>();
            var Details = doc.GetElementsByTagName("DocumentDetail");
            if (Details.Count == 0) return dctDetails;
            RecDetail recDetail;

            foreach (XmlElement det in Details)
            {
                recDetail = new RecDetail();
                foreach (XmlElement element in det.ChildNodes)
                {
                    switch (element.Name)
                    {
                        case "Id":
                            recDetail.IdXml = Convert.ToInt64(element.InnerXml);
                            break;
                        case "DocumentId":
                            recDetail.DocumentId = Convert.ToInt64(element.InnerXml);
                            break;
                        case "GoodId":
                            recDetail.GoodId = Convert.ToInt64(element.InnerXml);
                            break;
                        case "GoodsItemName":
                            recDetail.GoodsItemName = element.InnerXml;
                            break;
                        case "Quantity":
                            recDetail.Quantity = StringToDecimal(element.InnerXml);
                            break;
                        case "SalePrice":
                            recDetail.SalePrice = StringToDecimal(element.InnerXml);
                            break;
                        case "MoneySum":
                            recDetail.MoneySum = StringToDecimal(element.InnerXml);
                            break;
                        case "BonusSum":
                            recDetail.BonusSum = StringToDecimal(element.InnerXml);
                            break;
                    }
                }
                dctDetails.Add(recDetail.IdXml, recDetail);
            }
            return dctDetails;
        }


        private static void SaveRecepiesToDb(IDictionary<Guid, Receipt> recepies)
        {
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = connectionString;
                conn.Open();

                foreach (Receipt r in recepies.Values)
                {
                    using (var command = new SqlCommand("[dbo].[PointImport_DocMerge]", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    })
                    {
                        command.Parameters.Add("@DOC_ID_XML", SqlDbType.Int).Value = r.IdXml;
                        command.Parameters.Add("@DOC_GUID", SqlDbType.UniqueIdentifier).Value = r.DocumentGuid;
                        command.Parameters.Add("@DOC_DATE", SqlDbType.DateTime).Value = r.DocDate;
                        command.Parameters.Add("@DOC_TYPE", SqlDbType.Int).Value = r.DocType;
                        command.Parameters.Add("@DOC_NO", SqlDbType.NVarChar).Value = r.IdXml;
                        command.Parameters.Add("@DOC_SUM", SqlDbType.Money).Value = r.DocSum;
                        command.Parameters.Add("@DOC_MEMO", SqlDbType.NVarChar).Value = r.Memo;
                        command.Parameters.Add("@DOC_FILE_NAME", SqlDbType.NVarChar).Value = r.FileName;
                        command.Parameters.Add("@AG_ID", SqlDbType.Int).Value = r.AgID;
                        command.Parameters.Add("@FR_CONTR_ID", SqlDbType.Int).Value = r.FranchiseContractorId;
                        command.Parameters.Add("@DOC_APPROVE_DATE", SqlDbType.DateTime).Value = r.DateOfApprove;
                        command.Parameters.Add("@DOC_USER_ID", SqlDbType.Int).Value = r.UserId;
                        command.Parameters.Add("@DOC_USER_NAME", SqlDbType.NVarChar).Value = r.UserName;
                        command.Parameters.Add("@BONUS_PAID", SqlDbType.Money).Value = r.BounsPaid;
                        command.Parameters.Add("@BONUS_PAID_ID", SqlDbType.Int).Value = r.BounsPaidId;


                        SqlParameter parameter;
                        parameter = command.Parameters.AddWithValue("@Jrn", CreateReceptDataTable(r));
                        parameter.SqlDbType = SqlDbType.Structured;
                        parameter.TypeName = "dbo._JRN_IMP_TYPE";
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private static DataTable CreateReceptDataTable(Receipt r)
        {
            DataTable table = new DataTable();
            table.Columns.Add("J_ID_XML", typeof(long));
            table.Columns.Add("DOC_ID", typeof(long));
            table.Columns.Add("DOC_ID_XML", typeof(long));
            table.Columns.Add("J_ENT", typeof(long));
            table.Columns.Add("ENT_NAME", typeof(string));
            table.Columns.Add("J_PRC", typeof(decimal));
            table.Columns.Add("J_QTY", typeof(decimal));
            table.Columns.Add("J_SUM", typeof(decimal));
            table.Columns.Add("MONEY_SUM", typeof(decimal));
            table.Columns.Add("BOUNUS_SUM", typeof(decimal));

            foreach (RecDetail rd in r.Details.Values)
            {
                DataRow row = table.NewRow();
                row["J_ID_XML"] = rd.IdXml;
                row["DOC_ID"] = r.DocID;
                row["DOC_ID_XML"] = r.IdXml;
                row["J_ENT"] = rd.GoodId;
                row["ENT_NAME"] = rd.GoodsItemName;
                row["J_PRC"] = rd.SalePrice;
                row["J_QTY"] = rd.Quantity;
                row["J_SUM"] = rd.MoneySum + rd.BonusSum;
                row["MONEY_SUM"] = rd.MoneySum;
                row["BOUNUS_SUM"] = rd.BonusSum;
                table.Rows.Add(row);
            }
            return table;
        }

        private static decimal StringToDecimal(string s)
        {
            s.Replace(",", "").Replace(" ", "");
            CultureInfo culture = new CultureInfo("en-US");
            return Convert.ToDecimal(s, culture);
        }

        public static IDictionary<string, string> GetImportedFilesFromDb()
        {
            IDictionary<string, string> dct = new Dictionary<string, string>();
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = connectionString;
                conn.Open();

                using (var command = new SqlCommand("[dbo].[PointImport_GetImportedFiles]", conn)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    command.Parameters.Add("@START_DATE", SqlDbType.DateTime).Value = DateTime.Now.AddMonths(-1);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                dct.Add(reader["DOC_FILE_NAME"].ToString(), reader["DOC_FILE_NAME"].ToString() + ".zip");
                            }
                        }
                    }
                }
            }
            return dct;
        }
        
        private static IDictionary<Guid, ZReport> ParseZReportsToDict(XmlDocument doc, string name)
        {
            XmlNodeList colXML = doc.GetElementsByTagName("Report");
            var repDict = new Dictionary<Guid, ZReport>();
            foreach (XmlElement el in colXML)
            {
                var rep = new ZReport();
                rep.FileName = name;
                foreach (XmlElement c in el.ChildNodes)
                {
                    switch (c.Name)
                    {
                        case "ReportNumber":
                            rep.ReportNumber = c.InnerXml;
                            break;
                        case "ReportGuid":
                            rep.ReportGuid = Guid.ParseExact(c.InnerXml, "B");
                            break;
                        case "DateOfReport":
                            rep.DateOfReport = Convert.ToDateTime(c.InnerXml);
                            break;
                        case "AmountOfSalesCash":
                            rep.AmountOfSalesCash = StringToDecimal(c.InnerXml);
                            break;  
                        case "AmountOfSalesCard":
                            rep.AmountOfSalesCard = StringToDecimal(c.InnerXml);
                            break;                         
                        case "AmountOfReturnsCash":
                            rep.AmountOfReturnsCash = StringToDecimal(c.InnerXml);
                            break;
                        case "AmountOfReturnsCard":
                            rep.AmountOfReturnsCard = StringToDecimal(c.InnerXml);
                            break;
                        case "AmountOfSalesBonusCash":
                            rep.AmountOfSalesBonusCash = StringToDecimal(c.InnerXml);
                            break;
                        case "AmountOfSalesBonusCard":
                            rep.AmountOfSalesBonusCard = StringToDecimal(c.InnerXml);
                            break;                        
                        case "AmountOfReturnsBonusCash":
                            rep.AmountOfReturnsBonusCash = StringToDecimal(c.InnerXml);
                            break;                        
                        case "AmountOfReturnsBonusCard":
                            rep.AmountOfReturnsBonusCard = StringToDecimal(c.InnerXml);
                            break;                        
                        case "AmountOfServicePayIn":
                            rep.AmountOfServicePayIn = StringToDecimal(c.InnerXml);
                            break;                        
                        case "AmountOfServicePayOut":
                            rep.AmountOfServicePayOut = StringToDecimal(c.InnerXml);
                            break;                        
                        case "AmountOfSales":
                            rep.AmountOfSales = StringToDecimal(c.InnerXml);
                            break;                        
                        case "CreatedByUser":
                            rep.CreatedByUser = Convert.ToInt64(c.InnerXml);
                            break;                        
                        case "CreatedByUserName":
                            rep.CreatedByUserName = c.InnerXml;
                            break;
                        case "ArrayOfDetails":
                            var ad = c.GetElementsByTagName("DepartmentId");
                            if (ad.Count > 0) rep.DepartmentId = Convert.ToInt64(ad[0].InnerXml);
                            break;
                        case "NumberOfReceiptsSales":
                            rep.NumberOfReceiptsSales = Convert.ToInt64(c.InnerXml);
                            break;
                        case "NumberOfReceiptsReturn":
                            rep.NumberOfReceiptsReturn = Convert.ToInt64(c.InnerXml);
                            break;
                        case "NumberOfReceiptsServicePayIn":
                            rep.NumberOfReceiptsServicePayIn = Convert.ToInt64(c.InnerXml);
                            break;
                        case "NumberOfReceiptsServicePayOut":
                            rep.NumberOfReceiptsServicePayOut = Convert.ToInt64(c.InnerXml);
                            break;
                    }
                }
                repDict.Add(rep.ReportGuid, rep);
            }
            return repDict;
        }

        private static void SaveZReportsToDb(IDictionary<Guid, ZReport> reports)
        {
            using (SqlConnection conn = new SqlConnection())
            {
                conn.ConnectionString = connectionString;
                conn.Open();

                foreach (ZReport r in reports.Values)
                {
                    using (var command = new SqlCommand("[dbo].[PointImport_ZReportMerge]", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    })
                    {
                        command.Parameters.Add("@REP_GUID", SqlDbType.UniqueIdentifier).Value = r.ReportGuid;
                        command.Parameters.Add("@REP_NO", SqlDbType.NVarChar).Value = r.ReportNumber;
                        command.Parameters.Add("@REP_DATE", SqlDbType.DateTime).Value = r.DateOfReport;

                        command.Parameters.Add("@REP_SUM_ALL", SqlDbType.Money).Value = r.AmountOfSales;

                        command.Parameters.Add("@REP_SUM", SqlDbType.Money).Value = r.AmountOfSalesCash;
                        command.Parameters.Add("@REP_SUM_CARD", SqlDbType.Money).Value = r.AmountOfSalesCard;
                        command.Parameters.Add("@REP_RET_SUM", SqlDbType.Money).Value = r.AmountOfReturnsCash;
                        command.Parameters.Add("@REP_RET_SUM_CARD", SqlDbType.Money).Value = r.AmountOfReturnsCard;

                        command.Parameters.Add("@REP_SUM_BONUS_CASH", SqlDbType.Money).Value = r.AmountOfSalesBonusCash;
                        command.Parameters.Add("@REP_SUM_BONUS_CARD", SqlDbType.Money).Value = r.AmountOfSalesBonusCard;
                        command.Parameters.Add("@REP_RET_SUM_BONUS_CASH", SqlDbType.Money).Value = r.AmountOfReturnsBonusCash;
                        command.Parameters.Add("@REP_RET_SUM_BONUS_CARD", SqlDbType.Money).Value = r.AmountOfReturnsBonusCard;
                        command.Parameters.Add("@REP_SERV_PAY_IN", SqlDbType.Money).Value = r.AmountOfServicePayIn;
                        command.Parameters.Add("@REP_SERV_PAY_OUT", SqlDbType.Money).Value = r.AmountOfServicePayOut;

                        command.Parameters.Add("@DEP_ID", SqlDbType.Int).Value = r.DepartmentId;
                        command.Parameters.Add("@REP_USER_ID", SqlDbType.Int).Value = r.CreatedByUser;
                        command.Parameters.Add("@REP_USER_NAME", SqlDbType.NVarChar).Value = r.CreatedByUserName;
                        command.Parameters.Add("@REP_FILE_NAME", SqlDbType.NVarChar).Value = r.FileName;

                        command.Parameters.Add("@REP_REC_NUMBER", SqlDbType.Int).Value = r.NumberOfReceiptsSales;
                        command.Parameters.Add("@REP_RET_REC_NUMBER", SqlDbType.Int).Value = r.NumberOfReceiptsReturn;
                        command.Parameters.Add("@REP_SERV_PAY_IN_NUMBER", SqlDbType.Int).Value = r.NumberOfReceiptsServicePayIn;
                        command.Parameters.Add("@REP_SERV_PAY_OUT_NUMBER", SqlDbType.Int).Value = r.NumberOfReceiptsServicePayOut;
                        
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
