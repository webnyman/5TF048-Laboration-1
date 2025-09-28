// Create a builder for the web application.
using PracticeLogger.DAL;

var builder = WebApplication.CreateBuilder(args);

// Add services for controllers with views (MVC pattern).
builder.Services.AddControllersWithViews();

// Add session services to enable session variables.
builder.Services.AddSession();   // Important for session variables

builder.Services.AddScoped<IPracticeSessionRepository, PracticeSessionRepository>();
builder.Services.AddScoped<IInstrumentRepository, InstrumentRepository>();


// Build the web application.
var app = builder.Build();

// Enable serving static files (e.g., CSS, JS, images).
app.UseStaticFiles();

// Enable routing middleware.
app.UseRouting();

// Enable session middleware to allow session state management.
app.UseSession();   // Important: activates session handling

// Configure the default route so the app starts at Practice/Start.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PracticeSession}/{action=Index}/{id?}");

if (app.Environment.IsDevelopment())
{
    // Detaljerad felinfo i dev
    app.UseDeveloperExceptionPage();
}
else
{
    // Egen felsida i prod
    app.UseExceptionHandler("/Practice/Error");
    app.UseHsts();
}


// Run the web application.
app.Run();
