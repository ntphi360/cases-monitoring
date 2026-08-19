using HoSoMonitoring.Api;
using HoSoMonitoring.Data;
using Microsoft.EntityFrameworkCore;
using HoSoMonitoring.Core.SeedWorks;
using HoSoMonitoring.Data.SeedWorks;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.Repositories;
using HoSoMonitoring.Core.Models.Content;
using HoSoMonitoring.Core.Services;
using HoSoMonitoring.Data.Services;
using HoSoMonitoring.Core.Configurations;
using HoSoMonitoring.Core.Content;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

// Connection string 
var configuration = builder.Configuration;
var connectionString = configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

// Config DbContext and Repository and AutoMapper
builder.Services.AddDbContext<HoSoMonitoringContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped(typeof(IRepository<,>), typeof(RepositoryBase<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddHttpClient<IEmailService, ResendEmailService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpClient<IZaloNotificationService, ZaloNotificationService>();
var administrativeUnit = configuration
    .GetSection(AdministrativeUnitOptions.SectionName)
    .Get<AdministrativeUnitOptions>()
    ?? new AdministrativeUnitOptions();
builder.Services.AddSingleton(administrativeUnit);
var monitoring = configuration
    .GetSection(MonitoringOptions.SectionName)
    .Get<MonitoringOptions>()
    ?? new MonitoringOptions();
builder.Services.AddSingleton(monitoring);
var resend = configuration
    .GetSection(ResendOptions.SectionName)
    .Get<ResendOptions>()
    ?? new ResendOptions();
builder.Services.AddSingleton(resend);
var zalo = configuration
    .GetSection(ZaloOptions.SectionName)
    .Get<ZaloOptions>()
    ?? new ZaloOptions();
builder.Services.AddSingleton(zalo);
var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
{
    throw new InvalidOperationException("JWT secret phải được cấu hình bằng User Secrets hoặc environment variable và dài tối thiểu 32 ký tự.");
}
builder.Services.AddSingleton(jwt);
builder.Services.AddIdentity<User, AppRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<HoSoMonitoringContext>()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
                var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var user = userId == null ? null : await userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    context.Fail("Tài khoản không hoạt động.");
                    return;
                }
                var currentRoles = await userManager.GetRolesAsync(user);
                var tokenRoles = context.Principal!.FindAll(System.Security.Claims.ClaimTypes.Role).Select(x => x.Value);
                if (!new HashSet<string>(currentRoles, StringComparer.OrdinalIgnoreCase).SetEquals(tokenRoles))
                {
                    context.Fail("Quyền tài khoản đã thay đổi.");
                }
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<ICaseCodeParser, CaseCodeParser>();
builder.Services.AddScoped<ICaseCodeGenerator, CaseCodeGenerator>();

builder.Services.AddAutoMapper(cfg => { }, typeof(CaseInListDto).Assembly);

//Front-end
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});



// Business services and repositories
var services = typeof(CaseRepository)
    .Assembly
    .GetTypes()
    .Where(x =>
        x.GetInterfaces()
            .Any(i => i.Name == typeof(IRepository<,>).Name)
        && !x.IsAbstract
        && x.IsClass
        && !x.IsGenericType);

foreach (var service in services)
{
    var allInterfaces = service.GetInterfaces();

    var directInterface = allInterfaces
        .Except(allInterfaces.SelectMany(t => t.GetInterfaces()))
        .FirstOrDefault();

    if (directInterface != null)
    {
        builder.Services.Add(
            new ServiceDescriptor(
                directInterface,
                service,
                ServiceLifetime.Scoped));
    }
}


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Nhập JWT access token."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

//Seeding Data
app.MigrateDatabase();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
