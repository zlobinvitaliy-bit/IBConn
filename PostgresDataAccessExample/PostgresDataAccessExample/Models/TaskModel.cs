using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARM.Models
{
    public class TaskModel
    {
        public string Time { get; set; }
        public string TDoc { get; set; }
        public string Product { get; set; }
        public string FlowDirection { get; set; }
        public string Car { get; set; }
        public string Tank { get; set; }
        public string CarDriver { get; set; }
        public string SetTotal_V { get; set; }
        public string Fact_V { get; set; }
        public string SetTotal_M { get; set; }
        public string Fact_M { get; set; }
        public string SetDensity { get; set; }
    }
}
