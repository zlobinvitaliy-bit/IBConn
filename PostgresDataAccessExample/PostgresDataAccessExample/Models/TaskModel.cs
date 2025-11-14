namespace PostgresDataAccessExample.Models
{
    public class TaskModel
    {
        public int Job64 { get; set; }
        public string Time { get; set; } = string.Empty;
        public string TDoc { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string FlowDirection { get; set; } = string.Empty;
        public string Car { get; set; } = string.Empty;
        public string Tank { get; set; } = string.Empty;
        public string CarDriver { get; set; } = string.Empty;
        public string SetTotal_V { get; set; } = string.Empty;
        public string Fact_V { get; set; } = string.Empty;
        public string SetTotal_M { get; set; } = string.Empty;
        public string Fact_M { get; set; } = string.Empty;
        public string SetDensity { get; set; } = string.Empty;
    }
}
