using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CampWeb.Components;
using CampWeb.Data;
using CampWeb.Models;
using CampWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Entity Framework and Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Use InMemory database for development
        options.UseInMemoryDatabase("CampWebDb");
    }
    else
    {
        // Use SQL Server for production
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/prihlaseni";
    options.AccessDeniedPath = "/pristup-odepren";
    options.SlidingExpiration = true;
});

// Add custom services
builder.Services.AddScoped<ICampService, CampService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    if (app.Environment.IsDevelopment())
    {
        // Ensure database is created for InMemory
        context.Database.EnsureCreated();
        
        // Seed additional test data
        await SeedTestDataAsync(scope.ServiceProvider);
    }
    else
    {
        // Apply migrations for SQL Server
        context.Database.Migrate();
    }
}

app.Run();

// Seed test data for development
static async Task SeedTestDataAsync(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Create roles
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    
    if (!await roleManager.RoleExistsAsync("Parent"))
    {
        await roleManager.CreateAsync(new IdentityRole("Parent"));
    }

    // Create test admin user
    var adminEmail = "admin@letnítabory.cz";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "Táborů",
            EmailConfirmed = true,
            PhoneNumber = "+420123456789",
            PhoneNumberConfirmed = true
        };
        
        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    // Create test parent user
    var parentEmail = "rodic@example.com";
    var parentUser = await userManager.FindByEmailAsync(parentEmail);
    
    if (parentUser == null)
    {
        parentUser = new ApplicationUser
        {
            UserName = parentEmail,
            Email = parentEmail,
            FirstName = "Jan",
            LastName = "Novák",
            EmailConfirmed = true,
            PhoneNumber = "+420987654321",
            PhoneNumberConfirmed = true
        };
        
        var result = await userManager.CreateAsync(parentUser, "Heslo123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(parentUser, "Parent");
        }
    }
}