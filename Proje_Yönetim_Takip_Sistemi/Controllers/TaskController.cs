using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proje_Yönetim_Takip_Sistemi.Data;
using Proje_Yönetim_Takip_Sistemi.Models;

namespace Proje_Yönetim_Takip_Sistemi.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        public TaskController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignUsers(int taskId)
        {
            var task = await _context.Tasks
                .Include(t => t.TaskUsers)
                .ThenInclude(tu => tu.User)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return NotFound();

            var allUsers = await _context.Users.ToListAsync();
            var assignedUserIds = task.TaskUsers.Select(tu => tu.UserId).ToList();

            ViewBag.Users = allUsers;
            ViewBag.AssignedUsers = assignedUserIds;
            ViewBag.TaskId = taskId;

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> AssignUsers(int taskId, string[] selectedUsers)
        {
            var task = await _context.Tasks
                .Include(t => t.TaskUsers)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                return NotFound();
            }

            //Mevcut kullanıcıları kaldır
            _context.TaskUsers.RemoveRange(task.TaskUsers);

            //Eğer yeni kullanıcı seçilmişse, ekleyelim
            if (selectedUsers != null && selectedUsers.Length > 0)
            {
                foreach (var userId in selectedUsers)
                {
                    if (!string.IsNullOrEmpty(userId))
                    {
                        task.TaskUsers.Add(new TaskUser { TaskId = taskId, UserId = userId });
                    }
                }
            }

            //Değişiklikleri kaydet
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Bu görev başka biri tarafından güncellendi. Lütfen tekrar deneyin.");

                //Eğer hata olursa ViewBag.Users ve ViewBag.AssignedUsers tekrar ayarlanmalı!
                var allUsers = await _context.Users.ToListAsync();
                var assignedUserIds = task.TaskUsers.Select(tu => tu.UserId).ToList();

                ViewBag.Users = allUsers;
                ViewBag.AssignedUsers = assignedUserIds;
                ViewBag.TaskId = taskId;

                return View(task);
            }

            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }


        // Bir projeye ait tüm görevleri listeleme
        public async Task<IActionResult> Index(int projectId)
        {
            var tasks = await _context.Tasks
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();
            ViewBag.ProjectId = projectId;
            return View(tasks);
        }

        // Görev oluşturma sayfası
        [Authorize(Roles = "Admin")]
        public IActionResult Create(int projectId)
        {
            ViewBag.ProjectId = projectId;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TaskName,Description,AssignedUser,StartDate,EndDate,Status")] TaskModel task, int projectId)
        {
            if (!ModelState.IsValid)
            {
                // ModelState hatalarını yazdır
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = "Hata: " + string.Join(" | ", errors);
                ViewBag.ProjectId = projectId; // Form tekrar açıldığında projectId kaybolmasın
                var users = await _context.Users.ToListAsync();
                ViewBag.Users = users;
                return View(task);
            }

            try
            {
                task.ProjectId = projectId; // Foreign key set et
                _context.Add(task);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Görev eklenirken bir hata oluştu: " + ex.Message;
                return View(task);
            }
        }

        //GÜNCELLEME (EDIT) GET
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin"); //Kullanıcı admin mi?

            var task = await _context.Tasks
                .Include(t => t.TaskUsers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            //Eğer kullanıcı admin değilse ve göreve atanmadıysa, erişim yok
            if (!isAdmin && !task.TaskUsers.Any(tu => tu.UserId == user.Id))
            {
                return Forbid();
            }

            ViewBag.IsAdmin = isAdmin; //View'e admin olup olmadığını gönderiyoruz
            return View(task);
        }

        //GÜNCELLEME (EDIT) POST
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit(int id, string status, string taskName, string description, DateTime startDate, DateTime endDate)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var task = await _context.Tasks
                .Include(t => t.TaskUsers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            //Eğer kullanıcı admin değilse ve göreve atanmadıysa, erişim yok!
            if (!isAdmin && !task.TaskUsers.Any(tu => tu.UserId == user.Id))
            {
                return Forbid();
            }

            //Eğer admin ise tüm alanları güncelleyebilir
            if (isAdmin)
            {
                task.TaskName = taskName;
                task.Description = description;
                task.StartDate = startDate;
                task.EndDate = endDate;
            }

            //Herkes `Status` değiştirebilir
            task.Status = status;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Görev başarıyla güncellendi!";
            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }


        //SİLME (DELETE) GET - Kullanıcıya onay sor
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            return View(task);
        }

        //SİLME (DELETE) POST - Veritabanından siler
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            try
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Görev silinirken hata oluştu: " + ex.Message;
                return View(task);
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int taskId, string status)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var task = await _context.Tasks
                    .Include(t => t.TaskUsers)
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task == null)
                {
                    return Json(new { success = false, message = "Görev bulunamadı!" });
                }

                if (!task.TaskUsers.Any(tu => tu.UserId == user.Id))
                {
                    return Json(new { success = false, message = "Bu görevi güncelleme yetkiniz yok!" });
                }

                //Status'u Güncelle ve EF Core'a Bildir
                task.Status = status;
                _context.Entry(task).State = EntityState.Modified;  //EF'ye değişikliği bildiriyoruz

                await _context.SaveChangesAsync();

                //Güncellenmiş veriyi kontrol et
                var updatedTask = await _context.Tasks.FindAsync(taskId);
                Console.WriteLine($"✅ Güncellenmiş Durum: {updatedTask.Status}");

                return Json(new { success = true, message = "Görev durumu başarıyla güncellendi!", newStatus = updatedTask.Status });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ HATA: " + ex.ToString());
                return Json(new { success = false, message = "Bir hata oluştu!", error = ex.Message });
            }
        }




    }
}




