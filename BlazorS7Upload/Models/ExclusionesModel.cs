using System.Data;

namespace BlazorS7Upload.Models
{
    public class ExclusionesModel
    {
        public string date { get; set; }
        public string sellerId { get; set; }
        public string deliveryOrderNumber { get; set; }
        public string itemId { get; set; }
        public string sku_falabella { get; set; }
        public string responsable { get; set; }
        public string motivo { get; set; }
        public string kpi_name { get; set; }
        public string pais { get; set; }
        public string comentario { get; set; }
        public string tipo_exclusion { get; set; }
        public bool IsSelected { get; set; } = false;
        public string Excluido { get; set; }
    }
}
