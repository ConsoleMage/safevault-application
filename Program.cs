using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using safevault_application.Data;
using safevault_application.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "app.db")}";
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// legacy helper removed — Identity is the primary user store now

var maliciousInput = "<script>alert('XSS');</script>";
Console.WriteLine(ValidationHelpers.IsValidXSSInput(maliciousInput) ? "XSS Test Failed" : "XSS Test Passed");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await InitializeIdentityDataAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();

static async Task InitializeIdentityDataAsync(IServiceProvider services)
{
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    await EnsureRoleAsync(roleManager, db, "Admin");
    await EnsureRoleAsync(roleManager, db, "User");

    var initialAdminUser = Environment.GetEnvironmentVariable("INITIAL_ADMIN_USER") ?? "admin";
    var initialAdminPassword = Environment.GetEnvironmentVariable("INITIAL_ADMIN_PASSWORD") ?? "Admin123!";
    await CreateUserIfNotExistsAsync(userManager, initialAdminUser, initialAdminPassword, "Admin", "initial admin");

    var initialUser = Environment.GetEnvironmentVariable("INITIAL_USER") ?? "user";
    var initialUserPassword = Environment.GetEnvironmentVariable("INITIAL_USER_PASSWORD") ?? "User123!";
    await CreateUserIfNotExistsAsync(userManager, initialUser, initialUserPassword, "User", "demo");
}

static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, ApplicationDbContext db, string roleName)
{
    try
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
    catch (Exception ex) when (ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

static async Task CreateUserIfNotExistsAsync(UserManager<IdentityUser> userManager, string userName, string password, string roleName, string seedType)
{
    var existing = await userManager.FindByNameAsync(userName);
    if (existing != null)
    {
        return;
    }

    var user = new IdentityUser { UserName = userName, Email = $"{userName}@local" };
    var createResult = await userManager.CreateAsync(user, password);
    if (!createResult.Succeeded)
    {
        var errors = string.Join(',', createResult.Errors.Select(e => e.Description));
        Console.WriteLine($"Failed to seed {seedType} user '{userName}': {errors}");
        return;
    }

    await userManager.AddToRoleAsync(user, roleName);
    Console.WriteLine($"Seeded {seedType} user '{userName}'");
}
