using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using safevault_application.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace safevault_application.Tests;

public class AccountAuthenticationTests
{
    private readonly WebApplicationFactory<Program> _factory = new();

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldFail()
    {
        var userManager = GetUserManager();
        var user = new IdentityUser { UserName = "testuser" };
        await userManager.CreateAsync(user, "CorrectPassword123!");

        var result = await userManager.CheckPasswordAsync(user, "WrongPassword!");

        Assert.False(result);
    }

    [Fact]
    public async Task Login_WithNonexistentUser_ShouldFail()
    {
        var userManager = GetUserManager();
        var fakeUser = new IdentityUser { UserName = "ghost" };

        var result = await userManager.CheckPasswordAsync(fakeUser, "AnyPassword123!");

        Assert.False(result);
    }

    [Fact]
    public async Task Access_ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/secure-data");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Access_ProtectedEndpoint_WithInvalidToken_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        var response = await client.GetAsync("/api/secure-data");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserRole_CannotAccess_AdminEndpoint()
    {
        var client = await AuthenticateAsync("user", "User123!");
        var response = await client.GetAsync("/api/admin-only");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminRole_CanAccess_AdminEndpoint()
    {
        var client = await AuthenticateAsync("admin", "Admin123!");
        var response = await client.GetAsync("/api/admin-only");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpClient> AuthenticateAsync(string username, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByNameAsync(username);
        Assert.NotNull(user);
        Assert.True(await userManager.CheckPasswordAsync(user, password));
        var role = await userManager.IsInRoleAsync(user, "Admin") ? "Admin" : "User";

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var jwtSettings = configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!));
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    private static UserManager<IdentityUser> GetUserManager()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var store = new UserStore<IdentityUser, IdentityRole, ApplicationDbContext, string>(
            new ApplicationDbContext(options));
        var optionsAccessor = new IdentityOptions();
        var identityOptions = Microsoft.Extensions.Options.Options.Create(optionsAccessor);
        var userValidators = new[] { new UserValidator<IdentityUser>() };
        var passwordValidators = new[] { new PasswordValidator<IdentityUser>() };
        var services = new Microsoft.Extensions.Logging.Abstractions.NullLogger<UserManager<IdentityUser>>();

        return new UserManager<IdentityUser>(
            store,
            identityOptions,
            new PasswordHasher<IdentityUser>(),
            userValidators,
            passwordValidators,
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new Microsoft.Extensions.DependencyInjection.ServiceCollection().BuildServiceProvider(),
            services);
    }
}