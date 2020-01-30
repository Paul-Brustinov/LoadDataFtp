using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadDataFtp
{
    public class Receipt
    {
        public long IdXml { get; set; }
        public long DocID { get; internal set; }
        public string DocumentNumber { get; set; }
        public Guid DocumentGuid { get; set; }
        public Guid TopDocumentGuid { get; set; }
        public string Memo { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DateOfApprove { get; set; }
        public decimal DocSum { get; set; }
        public long DocType { get; set; }
        public long AgID { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }

        public IDictionary<long, RecDetail> Details { get; set; }
        public string FileName { get; internal set; }
        public long FranchiseContractorId { get; set; }
        public decimal BounsPaid { get; set; }
        public long BounsPaidId { get; set; }

    }
}
