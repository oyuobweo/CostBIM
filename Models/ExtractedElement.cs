using System.Collections.Generic;

namespace CostBIM.Models
{
    public class ExtractedElement
    {
        public string Id { get; set; } = string.Empty;
        public string Guid { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        
        public string Workset { get; set; } = string.Empty;
        
        // Dictionary for dynamically resolved custom parameters for verification
        public Dictionary<string, string> CustomParameters { get; set; } = new Dictionary<string, string>();
    }
}
