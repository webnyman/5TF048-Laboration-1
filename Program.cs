// Create a builder for the web application.
var builder = WebApplication.CreateBuilder(args);

// Add services for controllers with views (MVC pattern).
builder.Services.AddControllersWithViews();

// Add session services to enable session variables.
builder.Services.AddSession();   // Important for session variables

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
    pattern: "{controller=Practice}/{action=Start}/{id?}".Trim());

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
