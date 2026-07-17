namespace BlazorS7Upload.Models
{
    public class ExclusionesMotivoModel
    {
        public int id { get; set; }
        public int fk_kpi { get; set; }
        public string name { get; set; }
        public string status { get; set; }
        public string kpi_name { get; set; }
    }
}
