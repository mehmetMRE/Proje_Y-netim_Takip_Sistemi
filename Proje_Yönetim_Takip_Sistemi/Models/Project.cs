namespace Proje_Yönetim_Takip_Sistemi.Models
{
    public enum ProjectStatus
    {
        Beklemede = 0,
        DevamEdiyor = 1,
        Tamamlandi = 2
    }
    public class Project
    {
        public int Id { get; set; }
        public string project_name { get; set; }
        public string statement { get; set; }
        public DateTime start_day { get; set; }
        public DateTime end_day { get; set; }
        public ProjectStatus status { get; set; }
        //Projeye atanmış kullanıcılar (Many-to-Many)
        public ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();

        //Bir projeye birden fazla görev eklenebilir
        public ICollection<TaskModel> Tasks { get; set; } = new List<TaskModel>();
    }
}
