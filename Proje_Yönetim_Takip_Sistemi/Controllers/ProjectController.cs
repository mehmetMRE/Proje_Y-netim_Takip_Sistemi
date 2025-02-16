using Proje_Yönetim_Takip_Sistemi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proje_Yönetim_Takip_Sistemi.Data;

using System.Linq;
using System.Threading.Tasks;

namespace Proje_Yönetim_Takip_Sistemi.Controllers
{
    [Authorize] // Kullanıcı girişi zorunlu
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        public ProjectController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;

        }


        // Kullanıcıları Atama
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignUsers(int projectId)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectUsers)
                .ThenInclude(pu => pu.User)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();

            var allUsers = await _userManager.Users.ToListAsync();
            var assignedUserIds = project.ProjectUsers.Select(pu => pu.UserId).ToList();

            ViewBag.Users = allUsers;
            ViewBag.AssignedUsers = assignedUserIds;
            ViewBag.ProjectId = projectId;

            return View();
        }
        //Kullanıcıları Atama İşlemi
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AssignUsers(int projectId, string[] selectedUsers)
        {
            //Projeyi ve ilişkili kullanıcıları al
            var project = await _context.Projects
                .Include(p => p.ProjectUsers)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                return NotFound();  // Proje bulunamazsa hata döndür
            }

            //Kullanıcı listesi boşsa, projeden tüm kullanıcıları kaldır
            _context.ProjectUsers.RemoveRange(project.ProjectUsers);

            //eğer yeni kullanıcı seçildiyse, onları ekle
            if (selectedUsers != null && selectedUsers.Length > 0)
            {
                foreach (var userId in selectedUsers)
                {
                    if (!string.IsNullOrEmpty(userId))
                    {
                        project.ProjectUsers.Add(new ProjectUser { ProjectId = projectId, UserId = userId });
                    }
                }
            }

            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Bu proje başka biri tarafından güncellendi. Lütfen tekrar deneyin.");

                var allUsers = await _context.Users.ToListAsync();
                var assignedUserIds = project.ProjectUsers.Select(pu => pu.UserId).ToList();

                ViewBag.Users = allUsers;
                ViewBag.AssignedUsers = assignedUserIds;
                ViewBag.ProjectId = projectId;

                return View(project);
            }

            return RedirectToAction("Index");
        }


        //Sadece Admin tüm projeleri görebilir, kullanıcılar sadece kendi projelerini görür**
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (User.IsInRole("Admin"))
            {
                return View(await _context.Projects.ToListAsync());
            }
            else
            {
                var userProjects = await _context.Projects
                    .Where(p => p.ProjectUsers.Any(pu => pu.UserId == user.Id))
                    .ToListAsync();
                return View(userProjects);
            }
        }

        //Create
        //Edit
        //Delete
        //Sadece Admin yeni proje oluşturabilir
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employee/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,project_name,statement,start_day,end_day,status")] Project project)
        {
            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // GET: Employee/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Projects.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        // POST: Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,project_name,statement,start_day,end_day,status")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // GET: Employee/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Projects
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            // Redirect to the Index after successful deletion
            return RedirectToAction(nameof(Index), "Project");
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
}
