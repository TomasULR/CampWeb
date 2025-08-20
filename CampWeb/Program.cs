using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CampWeb.Components;
using CampWeb.Data;
using CampWeb.Models;
using CampWeb.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

// Use AddControllersWithViews instead of AddControllers
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

// Configure Npgsql data source with JSON support
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson(); // This fixes the List<string> serialization issue
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dataSource)
        .ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

// BEST PRACTICE: Use AddIdentity instead of AddDefaultIdentity for custom user class
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

// BEST PRACTICE: Configure application cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/prihlaseni";  // Keep your Czech path
    options.LogoutPath = "/odhlaseni";
    options.AccessDeniedPath = "/pristup-odepren";
    options.SlidingExpiration = true;

    // BEST PRACTICE: Secure cookie settings for production
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // Additional security settings
    options.Cookie.Name = "CampWeb.Auth";
    options.ReturnUrlParameter = "returnUrl";
});

// BEST PRACTICE: Proper service registration with interfaces
builder.Services.AddScoped<ICampService, CampService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ParentOnly", policy => policy.RequireRole("Parent"));
    options.AddPolicy("AdminOrParent", policy => policy.RequireRole("Admin", "Parent"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// BEST PRACTICE: Proper middleware order
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add custom route mappings
app.MapGet("/pristup-odepren", () => Results.Redirect("/prihlaseni"));

// Map controllers BEFORE database seeding
app.MapControllers();

// BEST PRACTICE: Runtime Identity seeding using UserManager/RoleManager
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        logger.LogInformation("Initializing database...");
        await context.Database.MigrateAsync();

        // BEST PRACTICE: Create roles using RoleManager
        await SeedRolesAsync(roleManager, logger);

        // BEST PRACTICE: Create users using UserManager
        await SeedUsersAsync(userManager, roleManager, logger);

        // Seed sample camps
        await SeedCampsAsync(context, logger);

        logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

app.Run();

// BEST PRACTICE: Separate seeding methods for better organization
static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
{
    string[] roleNames = { "Admin", "Parent" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                logger.LogInformation("Role '{RoleName}' created successfully", roleName);
            }
            else
            {
                logger.LogError("Failed to create role '{RoleName}': {Errors}",
                    roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}

static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager, ILogger logger)
{
    // Create admin user
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
            PhoneNumberConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            logger.LogInformation("Admin user created successfully");
        }
        else
        {
            logger.LogError("Failed to create admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    // Create parent user
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
            PhoneNumberConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(parentUser, "Heslo123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(parentUser, "Parent");
            logger.LogInformation("Parent user created successfully");
        }
        else
        {
            logger.LogError("Failed to create parent user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}

static async Task SeedCampsAsync(ApplicationDbContext context, ILogger logger)
{
    if (!context.Camps.Any())
    {
        var camps = new List<Camp>
        {
            new Camp
            {
                Name = "Letní dobrodružství na Šumavě",
                Location = "Šumava, České Budějovice",
                Type = "Přírodní",
                Price = 4500,
                AvailableSpots = 20,
                AgeGroup = "8-14 let",
                ShortDescription = "Týden plný dobrodružství v krásné přírodě Šumavy",
                Description = "Přijďte si užít týden plný dobrodružství v nádherné přírodě Šumavy. Program zahrnuje turistiku, táboráky, hry v přírodě a spoustu zábavy.",
                Latitude = 49.0522,
                Longitude = 13.3256,
                Activities = new List<string> { "Turistika", "Táboráky", "Hry v přírodě", "Orientační běh" },
                StartDate = DateTime.UtcNow.AddDays(30),
                EndDate = DateTime.UtcNow.AddDays(37)
            },
            new Camp
            {
                Name = "Sportovní tábor Plzeň",
                Location = "Plzeň, sportovní areál",
                Type = "Sportovní",
                Price = 3800,
                AvailableSpots = 25,
                AgeGroup = "10-16 let",
                ShortDescription = "Tábor zaměřený na různé sportovní aktivity",
                Description = "Sportovní tábor pro všechny milovníky pohybu. Fotbal, basketbal, atletika, plavání a mnoho dalšího.",
                Latitude = 49.7384,
                Longitude = 13.3736,
                Activities = new List<string> { "Fotbal", "Basketbal", "Atletika", "Plavání", "Volejbal" },
                StartDate = DateTime.UtcNow.AddDays(45),
                EndDate = DateTime.UtcNow.AddDays(52)
            },
            new Camp
            {
                Name = "Vědecký tábor - malí vědci",
                Location = "Plzeň, vědecké centrum",
                Type = "Vzdělávací",
                Price = 5200,
                AvailableSpots = 15,
                AgeGroup = "9-15 let",
                ShortDescription = "Objevte svět vědy zábavnou formou",
                Description = "Tábor pro zvídavé děti, které se chtějí dozvědět více o vědě. Experimenty, pozorování, výroba různých předmětů.",
                Latitude = 49.7472,
                Longitude = 13.3778,
                Activities = new List<string> { "Chemické experimenty", "Fyzikální pokusy", "Astronomie", "Biologie" },
                StartDate = DateTime.UtcNow.AddDays(60),
                EndDate = DateTime.UtcNow.AddDays(67)
            }
        };

        context.Camps.AddRange(camps);
        await context.SaveChangesAsync();
        logger.LogInformation("Sample camps seeded successfully");
    }
}
