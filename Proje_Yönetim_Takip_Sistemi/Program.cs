using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proje_Yönetim_Takip_Sistemi.Data;
using Proje_Yönetim_Takip_Sistemi.Models;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı Bağlantısı
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//**Identity Konfigurasyonu (Admin ve User Rolleri İçin)**
builder.Services.AddDefaultIdentity<User>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; //E-mail doğrulaması kapalı
})
.AddRoles<IdentityRole>() //Roller eklendi
.AddEntityFrameworkStores<ApplicationDbContext>();

// Razor Pages ve MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

//**Admin ve User Rollerini Otomatik Olarak Oluştur**
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    await SeedRolesAndAdminUser(userManager, roleManager);
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); //Authentication'ı ekle
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

//**Roller ve Admin Kullanıcısını Otomatik Olarak Ekleyen Metot**
async Task SeedRolesAndAdminUser(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
{
    string adminEmail = "admin@gmail.com";
    string adminPassword = "Admin.365";

    // Eğer Admin rolü yoksa, ekle
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Eğer User rolü yoksa, ekle
    if (!await roleManager.RoleExistsAsync("User"))
    {
        await roleManager.CreateAsync(new IdentityRole("User"));
    }

    // Eğer Admin kullanıcı yoksa, oluştur ve Admin rolüne ata
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
