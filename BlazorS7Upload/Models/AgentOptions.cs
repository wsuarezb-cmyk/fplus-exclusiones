namespace BlazorS7Upload.Models
{
    public class AgentOptions
    {
        public bool Enabled { get; set; }
        public string Url { get; set; } = "";
        public string Title { get; set; } = "Asistente Forte";
        public string Subtitle { get; set; } = "";
        public string ButtonLabel { get; set; } = "Pregúntale al asistente";
        public string AllowedRoles { get; set; } = "";
    }
}
