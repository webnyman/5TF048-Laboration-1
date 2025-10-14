// Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PracticeLogger.DAL;
using PracticeLogger.Data;
using PracticeLogger.Models;    

// === Lägg till dina Identity-typer & DbContext-namespace ===
// using PracticeLogger.Data;           // ApplicationDbContext
// using PracticeLogger.Models.Auth;    // ApplicationUser, ApplicationRole

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// 1) Databaser & Identity
// ---------------------------
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); // appsettings.json

builder.Services
    .AddIdentityCore<ApplicationUser>(opt =>
    {
        opt.Password.RequiredLength = 8;
        opt.User.RequireUniqueEmail = true;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

// Cookie-auth (Identity UI använder detta)
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    opt.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    opt.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
.AddIdentityCookies();

// ---------------------------
// 2) MVC / Razor / Session
// ---------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();   // behövs för Identity UI-sidorna
builder.Services.AddSession();      // om du fortfarande använder session (ex. filter, flash)

// ---------------------------
// 3) DI – Repositories (DAL)
// ---------------------------
builder.Services.AddScoped<IPracticeSessionRepository, PracticeSessionRepository>();
builder.Services.AddScoped<IInstrumentRepository, InstrumentRepository>();

var app = builder.Build();

// ---------------------------
// 4) Felhantering & säkerhet
// ---------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Practice/Error");
    app.UseHsts();
}

// (valfritt) app.UseHttpsRedirection();
app.UseStaticFiles();

// ---------------------------
// 5) Ordning på middleware
// ---------------------------
app.UseRouting();

app.UseSession();           // om du använder session
app.UseAuthentication();    // <-- Viktigt: före UseAuthorization
app.UseAuthorization();

// ---------------------------
// 6) Routing
// ---------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PracticeSession}/{action=Index}/{id?}");

// Identity UI (Login/Logout/Register etc.)
app.MapRazorPages();

app.Run();
