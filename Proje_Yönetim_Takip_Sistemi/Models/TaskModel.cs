using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Proje_Yönetim_Takip_Sistemi.Models
{

    public class TaskModel
    {
        public int Id { get; set; }
        public string TaskName { get; set; }
        public string Description { get; set; }
        public string AssignedUser { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [Required]
        public string Status { get; set; }

        // Foreign Key
        public int ProjectId { get; set; }

        // Navigasyon özelliği (nullable yapıldı)
        public Project? Project { get; set; }
        // Göreve atanacak kullanıcılar
        public ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();
    }
    public class TaskUser
    {
        public int TaskId { get; set; }  
        public TaskModel Task { get; set; }

        public string UserId { get; set; } 
        public User User { get; set; }
    }



}