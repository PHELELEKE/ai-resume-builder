using System.ComponentModel.DataAnnotations;

namespace AIResumeBuilder.Models
{
    public class Experience
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Company name is required")]
        public string Company { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Position is required")]
        public string Position { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Start date is required")]
        [Range(1900, 2100, ErrorMessage = "Please enter a valid year (1900-2100)")]
        public int StartDate { get; set; }
        
        [Range(1900, 2100, ErrorMessage = "Please enter a valid year (1900-2100)")]
        public int? EndDate { get; set; }
        
        public bool IsCurrent { get; set; }
        
        public int ResumeId { get; set; }
        public Resume? Resume { get; set; }
    }
}