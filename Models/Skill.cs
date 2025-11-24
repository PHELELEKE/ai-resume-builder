using System.ComponentModel.DataAnnotations;

namespace AIResumeBuilder.Models
{
    public class Skill
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Skill name is required")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Skill level is required")]
        public string Level { get; set; } = "Intermediate";
        
        public string Category { get; set; } = "Technical";
        
        public int ResumeId { get; set; }
        public Resume? Resume { get; set; }
    }
}