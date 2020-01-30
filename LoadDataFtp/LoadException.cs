using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadDataFtp
{
    public class LoadException
    {
        public string Extrainfo { get; set; }
        public Exception e { get; set; }
    }
}
