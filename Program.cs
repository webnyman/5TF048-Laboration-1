using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PracticeLogger.Data;
using PracticeLogger.Models;
using PracticeLogger.DAL;
using Microsoft.AspNetCore.Identity.UI.Services;
using PracticeLogger.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


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

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];

builder.Services
    .AddAuthentication() // kompletterar befintlig auth, skriver inte över Identity
    .AddJwtBearer("ApiJwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
});


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

// 4) API
builder.Services.AddControllers(); // utöver MVC Views
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// API versioning (enkelt upplägg)
builder.Services.AddApiVersioning(o =>
{
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    o.ReportApiVersions = true;
});

// CORS – öppna för din klient under utveckling
builder.Services.AddCors(o =>
{
    o.AddPolicy("Client", p => p
        .WithOrigins("https://localhost:5173", "http://localhost:5173") // t.ex. Vite/React
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});



var app = builder.Build();

// 5) Felhantering
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Practice/Error");
    app.UseHsts();
}

// 6) Middleware-ordning
// app.UseHttpsRedirection(); // valfritt
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();   // <-- före Authorization
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("Client");     // om du behöver CORS
app.MapControllers();      // API-rutter

// 7) Endpoints
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PracticeSession}/{action=Index}/{id?}");
app.MapRazorPages();

app.MapGet("/Account/Login", () => Results.Redirect("/Identity/Account/Login"));
app.MapGet("/Account/Register", () => Results.Redirect("/Identity/Account/Register"));

app.Run();
