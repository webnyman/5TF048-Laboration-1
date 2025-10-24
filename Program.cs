using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PracticeLogger.Data;
using PracticeLogger.Models;
using PracticeLogger.DAL;
using Microsoft.AspNetCore.Identity.UI.Services;
using PracticeLogger.Services;

var builder = WebApplication.CreateBuilder(args);

// 1) DbContext + Identity
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentityCore<ApplicationUser>(opt =>
    {
        opt.Password.RequiredLength = 8;
        opt.User.RequireUniqueEmail = true;
        opt.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    // Vart skickas man vid 401 (ej inloggad)?
    options.LoginPath = "/Identity/Account/Login";

    // Vart skickas man vid 403 (saknar rättigheter/roll)?
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    // (valfritt) hur länge gäller cookien
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    opt.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    opt.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
.AddIdentityCookies();

// 2) MVC/Razor/Session
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSession();

// 3) DAL
builder.Services.AddScoped<IPracticeSessionRepository, PracticeSessionRepository>();
builder.Services.AddScoped<IInstrumentRepository, InstrumentRepository>();

builder.Services.AddSingleton<IEmailSender, DevMailSender>();

builder.Services.AddScoped<IRuleCoachService, RuleCoachService>();


var app = builder.Build();

// 4) Felhantering
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Practice/Error");
    app.UseHsts();
}

// 5) Middleware-ordning
// app.UseHttpsRedirection(); // valfritt
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();   // <-- före Authorization
app.UseAuthorization();

// 6) Endpoints
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PracticeSession}/{action=Index}/{id?}");
app.MapRazorPages();

app.MapGet("/Account/Login", () => Results.Redirect("/Identity/Account/Login"));
app.MapGet("/Account/Register", () => Results.Redirect("/Identity/Account/Register"));


// 7) (valfritt) Seed – ALLTID före Run(), i scope
// using (var scope = app.Services.CreateScope())
// {
//     var sp = scope.ServiceProvider;
//     await IdentitySeeder.SeedAsync(sp);
// }

app.Run();
