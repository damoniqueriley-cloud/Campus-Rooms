using CampusRooms.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Razor Pages: the Identity pages (register, log in, log out) are served
// from the Identity UI package and need Razor Pages turned on.
builder.Services.AddRazorPages();

// EF Core: any controller that asks for an AppDbContext gets one,
// connected to the database named in appsettings.json.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CampusRoomsDb")));

// Identity (Task 5): accounts live in the same database, no email
// confirmation, because the app cannot send email.
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()                    // Admin and Staff (Task 6)
    .AddEntityFrameworkStores<AppDbContext>();

// A visitor who opens a locked page goes to the login page; someone logged
// in without the right role goes to the Access denied page.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// CanManageRooms means "Admin or Staff" (Task 6): the Add and Edit pages need it
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageRooms", policy => policy.RequireRole("Admin", "Staff"));
});

var app = builder.Build();

// Made once at start-up (Task 6): the two roles, and the account named in
// AdminEmail becomes an Admin the next time the app starts after that
// account has registered. No password is written in the code.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var role_manager = services.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (string role_name in new[] { "Admin", "Staff" })
    {
        if (!await role_manager.RoleExistsAsync(role_name))
        {
            await role_manager.CreateAsync(new IdentityRole(role_name));
        }
    }

    string? admin_email = app.Configuration["AdminEmail"];
    if (admin_email != null)
    {
        var user_manager = services.GetRequiredService<UserManager<IdentityUser>>();
        IdentityUser? admin = await user_manager.FindByEmailAsync(admin_email);
        if (admin != null && !await user_manager.IsInRoleAsync(admin, "Admin"))
        {
            await user_manager.AddToRoleAsync(admin, "Admin");
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // who are you?
app.UseAuthorization();    // what may you do?

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
