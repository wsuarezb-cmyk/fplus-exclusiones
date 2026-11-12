namespace BlazorS7Upload.Models
{
    public class ExclusionesSubmotivoModel
    {
        public long id { get; set; }
        public long fk_motivo { get; set; }
        public string name { get; set; }
        public string status { get; set; }
        public string pais { get; set; }
        public string motivo_name { get; set; }
        public long fk_kpi { get; set; }
    }
}
