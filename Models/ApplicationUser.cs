using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace AIResumeBuilder.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        
        // Navigation property for resumes
        public virtual ICollection<Resume> Resumes { get; set; } = new List<Resume>();
    }
}