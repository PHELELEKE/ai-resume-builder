using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AIResumeBuilder.Models;
using AIResumeBuilder.Data;

namespace AIResumeBuilder.Controllers
{
    [Authorize]
    public class ClassicResumeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClassicResumeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Create()
        {
            return View(new Resume { Template = "Classic" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Resume resume)
        {
            if (ModelState.IsValid)
            {
                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Set user and template
                resume.UserId = user.Id;
                resume.Template = "Classic";
                resume.CreatedDate = DateTime.Now;
                
                // Ensure collections are initialized
                resume.Educations ??= new List<Education>();
                resume.Experiences ??= new List<Experience>();
                resume.Skills ??= new List<Skill>();

                // Clean empty items
                resume.Educations = resume.Educations.Where(e => !string.IsNullOrEmpty(e.Institution)).ToList();
                resume.Experiences = resume.Experiences.Where(e => !string.IsNullOrEmpty(e.Company)).ToList();
                resume.Skills = resume.Skills.Where(s => !string.IsNullOrEmpty(s.Name)).ToList();

                // Save to database
                _context.Resumes.Add(resume);
                await _context.SaveChangesAsync();
                
                return RedirectToAction("Preview", new { id = resume.Id });
            }
            
            return View(resume);
        }

        public async Task<IActionResult> Preview(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var resume = await _context.Resumes
                .Include(r => r.Educations)
                .Include(r => r.Experiences)
                .Include(r => r.Skills)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user!.Id);

            if (resume == null)
            {
                return RedirectToAction("Create");
            }
            return View(resume);
        }

        public IActionResult Export(int id, string format = "pdf")
        {
            // Redirect to main Resume controller for export
            return RedirectToAction("Export", "Resume", new { id, format });
        }

        [HttpPost]
        public JsonResult GenerateAIContent([FromBody] AIContentRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.JobTitle))
                {
                    return Json(new { success = false, error = "Job title is required" });
                }

                var aiContent = $"""
                    🤖 AI-Generated Professional Summary for {request.JobTitle}:
                    
                    Results-driven {request.JobTitle} with comprehensive experience in relevant technologies and methodologies. 
                    Proven track record of delivering high-quality solutions and exceeding performance expectations. 
                    Strong analytical skills combined with excellent communication abilities.
                    
                    [Note: This is mock AI content. Customize as needed for your specific experience.]
                    """;
                
                return Json(new { success = true, content = aiContent });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Internal server error" });
            }
        }
    }
}