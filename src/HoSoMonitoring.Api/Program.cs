using HoSoMonitoring.Api;
using HoSoMonitoring.Data;
using Microsoft.EntityFrameworkCore;
using HoSoMonitoring.Core.SeedWorks;
using HoSoMonitoring.Data.SeedWorks;
using HoSoMonitoring.Core.Repositories;
using HoSoMonitoring.Data.Repositories;
using HoSoMonitoring.Core.Models.Content;



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

builder.Services.AddAutoMapper(cfg => { }, typeof(CaseInListDto).Assembly);

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

var app = builder.Build();

//Seeding Data
app.MigrateDatabase();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
