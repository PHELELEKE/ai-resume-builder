using System.ComponentModel.DataAnnotations;

namespace AIResumeBuilder.Models
{
    public class Education
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Institution name is required")]
        public string Institution { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Degree is required")]
        public string Degree { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Field of study is required")]
        public string Field { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Graduation year is required")]
        [Range(1900, 2100, ErrorMessage = "Invalid graduation year")]
        public int GraduationYear { get; set; }
        
        public int ResumeId { get; set; }
        public Resume? Resume { get; set; }
    }
}