using Microsoft.EntityFrameworkCore;
using Infractucture.Data;
using Core.Interfaces;
using API.Helpers;
using API.Middleware;
using Microsoft.AspNetCore.Mvc;
using API.Errors;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options => 
{
    options.InvalidModelStateResponseFactory = ActionContext => 
    {
        var errors = ActionContext.ModelState.Where(e=>e.Value.Errors.Count > 0)
        .SelectMany(x => x.Value.Errors)
        .Select(x => x.ErrorMessage).ToArray();

        var errorResponse = new ApiValidationErrorResponse
        {
            Errors = errors
        };
        return new BadRequestObjectResult(errorResponse);
    };
});

builder.Services.AddAutoMapper(typeof(MappingProfiles));


// builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddDbContext<StoreContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => 
{
    c.SwaggerDoc("v1", new OpenApiInfo {Title = "Skinet API", Version = "v1" });
});

var app = builder.Build();

// Automatically apply migrations and create the database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger<Program>();
    try
    {
        var dbContext = services.GetRequiredService<StoreContext>();
        dbContext.Database.Migrate();

        // Seed the database
        await StoreContextSeed.SeedAsync(dbContext, loggerFactory);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}



// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c=>{c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkiNet API v1");});
}


app.UseHttpsRedirection();

app.UseStatusCodePagesWithReExecute("/errors/{0}");

app.UseStaticFiles(); 

app.UseAuthorization();


app.MapControllers();

app.Run();