using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Proje_Yönetim_Takip_Sistemi.Models
{
    public class User : IdentityUser
    {
        public ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();
    }
}
