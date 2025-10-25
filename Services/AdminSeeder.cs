using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PracticeLogger.Models;

public static class AdminSeeder
{
    public static async Task EnsureAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        const string roleName = "Admin";
        const string adminEmail = "admin@test.se";
        const string adminPassword = "Admin1234!";
        const string displayName = "Site Admin";

        // 1️⃣ Skapa roll om den inte finns
        if (!await roleMgr.RoleExistsAsync(roleName))
        {
            await roleMgr.CreateAsync(new ApplicationRole
            {
                Name = roleName,
                Description = "Full behörighet att hantera instrument, användare och data."
            });
        }

        // 2️⃣ Skapa användare om den inte finns
        var user = await userMgr.FindByEmailAsync(adminEmail);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = displayName,
                EmailConfirmed = true
            };

            var createResult = await userMgr.CreateAsync(user, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new Exception($"Kunde inte skapa admin-användare: {errors}");
            }
        }

        // 3️⃣ Koppla roll
        if (!await userMgr.IsInRoleAsync(user, roleName))
        {
            await userMgr.AddToRoleAsync(user, roleName);
        }
    }
}
