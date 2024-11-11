namespace BlazorWorkbox.Models
{
    public class WorkboxItem
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string Language { get; set; }
        public int Version { get; set; }
        public string TemplateName { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime Updated { get; set; }
        public Guid WorkflowStateId { get; set; }
        public string ItemUri { get; set; }
    }
}