using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AIResumeBuilder.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AIResumeBuilder.Data;

namespace AIResumeBuilder.Controllers
{
    [Authorize]
    public class ResumeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ResumeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Resume/ChooseTemplate
        public IActionResult ChooseTemplate()
        {
            return View();
        }

        

        // GET: Resume/Create
        public IActionResult Create(string template = "Professional")
        {
            var resume = new Resume 
            { 
                Template = template 
            };
            
            ViewBag.Template = template;
            return View(resume);
        }

        // POST: Resume/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Resume resume)
        {
            if (resume == null)
            {
                ModelState.AddModelError("", "Resume data is missing");
                return View(new Resume());
            }

            // Get current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Clean up empty collection items
            resume.Educations = resume.Educations?.Where(e => 
                !string.IsNullOrEmpty(e.Institution) || 
                !string.IsNullOrEmpty(e.Degree) || 
                !string.IsNullOrEmpty(e.Field) || 
                e.GraduationYear > 0
            ).ToList() ?? new List<Education>();

            resume.Experiences = resume.Experiences?.Where(e =>
                !string.IsNullOrEmpty(e.Company) ||
                !string.IsNullOrEmpty(e.Position) ||
                !string.IsNullOrEmpty(e.Description) ||
                e.StartDate > 0
            ).ToList() ?? new List<Experience>();

            resume.Skills = resume.Skills?.Where(s =>
                !string.IsNullOrEmpty(s.Name) ||
                !string.IsNullOrEmpty(s.Level)
            ).ToList() ?? new List<Skill>();

            if (ModelState.IsValid)
            {
                try
                {
                    // Assign user and save to database
                    resume.UserId = user.Id;
                    resume.CreatedDate = DateTime.Now;
                    
                    // Ensure collections are initialized
                    resume.Educations ??= new List<Education>();
                    resume.Experiences ??= new List<Experience>();
                    resume.Skills ??= new List<Skill>();

                    _context.Resumes.Add(resume);
                    await _context.SaveChangesAsync();
                    
                    return RedirectToAction("Preview", new { id = resume.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving resume: {ex.Message}");
                }
            }
            
            ViewBag.Template = resume.Template;
            return View(resume);
        }

        // GET: Resume/Preview/5
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
                TempData["Error"] = "Resume not found";
                return RedirectToAction("ChooseTemplate");
            }
            
            return View(resume);
        }

        

        // GET: Resume/MyResumes
        public async Task<IActionResult> MyResumes()
        {
            var user = await _userManager.GetUserAsync(User);
            var resumes = await _context.Resumes
            .Where(r => r.UserId == user!.Id)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

            return View(resumes);
        }

        // Existing Export and GenerateAIContent methods remain the same...
        // [Keep your existing Export and GenerateAIContent methods]
    }
}