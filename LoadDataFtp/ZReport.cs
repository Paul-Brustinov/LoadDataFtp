using System;

namespace LoadDataFtp
{
    internal class ZReport
    {
        public ZReport()
        {
        }

        public string FileName { get; internal set; }
        public string ReportNumber { get; internal set; }
        public Guid ReportGuid { get; internal set; }
        public DateTime DateOfReport { get; internal set; }
        public decimal AmountOfSalesCash { get; internal set; }
        public decimal AmountOfSalesCard { get; internal set; }
        public decimal AmountOfReturnsCash { get; internal set; }
        public decimal AmountOfReturnsCard { get; internal set; }

        public decimal AmountOfSalesBonusCash { get; internal set; }
        public decimal AmountOfSalesBonusCard { get; internal set; }
        public decimal AmountOfReturnsBonusCash { get; internal set; }
        public decimal AmountOfReturnsBonusCard { get; internal set; }
        public decimal AmountOfServicePayIn { get; internal set; }
        public decimal AmountOfServicePayOut { get; internal set; }
        public decimal AmountOfSales { get; internal set; }

        public long CreatedByUser { get; internal set; }
        public string CreatedByUserName { get; internal set; }
        public long DepartmentId { get; internal set; }


        public long NumberOfReceiptsSales { get; internal set; }
        public long NumberOfReceiptsReturn { get; internal set; }
        public long NumberOfReceiptsServicePayIn { get; internal set; }
        public long NumberOfReceiptsServicePayOut { get; internal set; }



    }
}