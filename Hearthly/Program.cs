using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using Hearthly.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure EF Core to use SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Register null email sender
builder.Services.AddTransient<IEmailSender, NullEmailSender>();

// 3. Configure Identity with ApplicationUser and roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 4. Redirect unauthorized users to the login page
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
});

// 5. Add Razor Pages (for the Identity UI) and MVC
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());
});
builder.Services.AddHostedService<RuleOfThreeCompletionService>();
builder.Services.AddHostedService<RuleOfThreeResetService>();
builder.Services.AddDataProtection()
    .SetApplicationName("HearthlyVault")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Keys")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// 6. HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Apply any pending migrations at startup
using (var migrateScope = app.Services.CreateScope())
{
    var db = migrateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// 7. Seed roles and assign your admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var roleName in new[] { IdentityRoles.Admin, IdentityRoles.User })
    {
        if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
        {
            roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
        }
    }

    var adminEmail = builder.Configuration["AdminEmail"] ?? "sean@endevr.co.za";
    var adminUser = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
    if (adminUser != null &&
        !userManager.IsInRoleAsync(adminUser, IdentityRoles.Admin).GetAwaiter().GetResult())
    {
        userManager.AddToRoleAsync(adminUser, IdentityRoles.Admin).GetAwaiter().GetResult();
    }
}

// 8. Localization
var defaultCulture = new CultureInfo("en-ZA")
{
    NumberFormat = { NumberDecimalSeparator = "." }
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

app.UseRequestLocalization(localizationOptions);

// 9. Route mapping
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
