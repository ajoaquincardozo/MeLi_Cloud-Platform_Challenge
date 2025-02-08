using MeLi.UrlShortener.Application.Services;
using MeLi.UrlShortener.Domain.Interfaces;
using MeLi.UrlShortener.Api.Middleware;
using MeLi.UrlShortener.Application.Interfaces;
using MeLi.UrlShortener.Infrastructure.Configuration;
using MeLi.UrlShortener.Infrastructure.Persistence;
using MeLi.UrlShortener.Application.Config;
using MeLi.UrlShortener.Application.Cache;
using MeLi.UrlShortener.Infrastructure.Cache;
using MeLi.UrlShortener.Infrastructure.Persistence.Mapping;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var mongoDbSettings = builder.Configuration.GetSection(MongoDbSettings.SectionName)
    .Get<MongoDbSettings>();
mongoDbSettings?.Validate();

var redisSettings = builder.Configuration.GetSection(RedisSettings.SectionName)
    .Get<RedisSettings>();
redisSettings?.Validate();

// Add services to the container.
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(MongoDbSettings.SectionName));
builder.Services.Configure<GeneralConfig>(builder.Configuration.GetSection(GeneralConfig.SectionName));

// MongoDB
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddScoped<IUrlRepository, MongoUrlRepository>();
builder.Services.AddSingleton<IUrlAnalyticsRepository, MongoUrlAnalyticsRepository>();

// Redis
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection(RedisSettings.SectionName));
builder.Services.AddSingleton<IUrlCache, RedisUrlCache>();

//Service of Application
builder.Services.AddScoped<IShortCodeGenerator, ShortCodeGenerator>();
builder.Services.AddScoped<IUrlValidator, UrlValidator>();
builder.Services.AddScoped<IUrlService, UrlService>();
builder.Services.AddScoped<IUrlAnalyticsService, UrlAnalyticsService>();

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
MongoDbMapping.Configure();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

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

var context = app.Services.GetRequiredService<IMongoDbContext>();
context.CreateIndexes();
app.Run();
