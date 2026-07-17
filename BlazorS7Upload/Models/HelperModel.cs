using System.Data;

namespace BlazorS7Upload.Models
{
    public class HelperModel
    {
        public string dropdownSelectionpais { get; set; } = "CO";
        public string datos_consulta { get; set; }
        public string mensaje { get; set; }
        public bool isConsulting { get; set; }
        public DataTable dt { get; set; }
    }
}
