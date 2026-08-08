using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using writing_secure_code.Data;
using writing_secure_code.Helpers;

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
var isValid = ValidationHelpers.IsValidXSSInput(maliciousInput);
Console.WriteLine(isValid ? "XSS Test Failed" : "XSS Test Passed");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    var initialAdminUser = Environment.GetEnvironmentVariable("INITIAL_ADMIN_USER") ?? "admin";
    var initialAdminPassword = Environment.GetEnvironmentVariable("INITIAL_ADMIN_PASSWORD") ?? "Admin123!";

    try
    {
        if (!roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
        {
            roleManager.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();
        }
    }
    catch (Exception ex)
    {
        // If the Identity tables don't exist (e.g. migrating from legacy DB), recreate the database
        if (ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase))
        {
            // If DB exists but missing Identity tables, drop and recreate schema
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // retry creating the role
            if (!roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();
            }
        }
        else throw;
    }

    var existing = userManager.FindByNameAsync(initialAdminUser).GetAwaiter().GetResult();
    if (existing == null)
    {
        var newUser = new IdentityUser { UserName = initialAdminUser, Email = initialAdminUser + "@local" };
        var createResult = userManager.CreateAsync(newUser, initialAdminPassword).GetAwaiter().GetResult();
        if (createResult.Succeeded)
        {
            userManager.AddToRoleAsync(newUser, "Admin").GetAwaiter().GetResult();
            Console.WriteLine($"Seeded initial admin user '{initialAdminUser}'");
        }
        else
        {
            Console.WriteLine($"Failed to seed admin user '{initialAdminUser}': {string.Join(',', createResult.Errors.Select(e => e.Description))}");
        }
    }

    // Seed a regular demo user (in 'User' role)
    var initialUser = Environment.GetEnvironmentVariable("INITIAL_USER") ?? "user";
    var initialUserPassword = Environment.GetEnvironmentVariable("INITIAL_USER_PASSWORD") ?? "User123!";

    if (!roleManager.RoleExistsAsync("User").GetAwaiter().GetResult())
    {
        roleManager.CreateAsync(new IdentityRole("User")).GetAwaiter().GetResult();
    }

    var existingUser = userManager.FindByNameAsync(initialUser).GetAwaiter().GetResult();
    if (existingUser == null)
    {
        var demoUser = new IdentityUser { UserName = initialUser, Email = initialUser + "@local" };
        var createUserResult = userManager.CreateAsync(demoUser, initialUserPassword).GetAwaiter().GetResult();
        if (createUserResult.Succeeded)
        {
            userManager.AddToRoleAsync(demoUser, "User").GetAwaiter().GetResult();
            Console.WriteLine($"Seeded demo user '{initialUser}'");
        }
        else
        {
            Console.WriteLine($"Failed to seed demo user '{initialUser}': {string.Join(',', createUserResult.Errors.Select(e => e.Description))}");
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
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
