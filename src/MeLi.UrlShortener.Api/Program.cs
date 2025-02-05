using MeLi.UrlShortener.Application.Services;
using MeLi.UrlShortener.Domain.Interfaces;
using MeLi.UrlShortener.Api.Middleware;
using MeLi.UrlShortener.Application.Interfaces;
using MeLi.UrlShortener.Infrastructure.Configuration;
using MeLi.UrlShortener.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var mongoDbSettings = builder.Configuration.GetSection(MongoDbSettings.SectionName)
    .Get<MongoDbSettings>();
mongoDbSettings?.Validate();

// Add services to the container.
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

// MongoDB
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddSingleton<IUrlRepository, MongoUrlRepository>();

builder.Services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
builder.Services.AddSingleton<IUrlValidator, UrlValidator>();
builder.Services.AddScoped<IUrlService, UrlService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseErrorHandling();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
