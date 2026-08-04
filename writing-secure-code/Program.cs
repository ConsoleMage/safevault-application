using writing_secure_code.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "app.db")}";
builder.Services.AddSingleton(new LoginHelper(connectionString));

var maliciousInput = "<script>alert('XSS');</script>";
var isValid = ValidationHelpers.IsValidXSSInput(maliciousInput);
Console.WriteLine(isValid ? "XSS Test Failed" : "XSS Test Passed");

var app = builder.Build();

app.Services.GetRequiredService<LoginHelper>().InitializeDatabase();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
