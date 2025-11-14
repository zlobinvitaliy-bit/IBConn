using System;

namespace PostgresDataAccessExample.Models
{
    public class TaskModel
    {
        public int Id { get; set; }
        public DateTime Time { get; set; } // RecTime
        public string TDoc { get; set; } // Doc
        public string ProductName { get; set; } // Product (from JOIN)
        public string Car { get; set; } // Machine (from JOIN)
        public int Direction { get; set; }
    }
}
