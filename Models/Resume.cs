using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace AIResumeBuilder.Models
{
    public class Resume
    {
        public Resume()
        {
            Educations = new List<Education>();
            Experiences = new List<Experience>();
            Skills = new List<Skill>();
        }

        public int Id { get; set; }
        
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Target job title is required")]
        [Display(Name = "Target Job Title")]
        public string TargetJobTitle { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Summary is required")]
        [StringLength(500, ErrorMessage = "Summary cannot exceed 500 characters")]
        public string Summary { get; set; } = string.Empty;
        
        public List<Education> Educations { get; set; }
        public List<Experience> Experiences { get; set; }
        public List<Skill> Skills { get; set; }
        
        public string Template { get; set; } = "Professional";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // ADD THESE PROPERTIES FOR USER RELATIONSHIP
        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}