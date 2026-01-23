
using Microsoft.EntityFrameworkCore;
using DrugTracker.Data;
using DrugTracker.Repositories.Interfaces;
using DrugTracker.Repositories.Implementations;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<DrugTrackerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HCAminiEHR")));

// Register Repositories and UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDrugBatchRepository, DrugBatchRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IBlockchainLedgerRepository, BlockchainLedgerRepository>();

// Services
builder.Services.AddScoped<DrugTracker.Services.IBlockchainService, DrugTracker.Services.BlockchainService>();
builder.Services.AddScoped<DrugTracker.Services.IBatchService, DrugTracker.Services.BatchService>();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    // .AddCookie(options =>
    // {
    //     options.LoginPath = "/Account/Login";
    //     options.AccessDeniedPath = "/Account/AccessDenied";
    //     options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    // });
    .AddCookie("ManufacturerScheme", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "DrugTracker.Manufacturer";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(3);
        options.SlidingExpiration = true;
    })
    .AddCookie("DistributorScheme", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "DrugTracker.Distributor";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(3);
        options.SlidingExpiration = true;
    })
    .AddCookie("PharmacyScheme", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "DrugTracker.Pharmacy";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(3);
        options.SlidingExpiration = true;
    })
    .AddCookie("AdminScheme", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "DrugTracker.Admin";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(3);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DrugTrackerDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Ensure Authentication Middleware is added
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
