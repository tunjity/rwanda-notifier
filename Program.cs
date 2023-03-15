using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using notifier.Interface;
using notifier.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<ISendNotificationService, SendNotificationService>();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Notify API",
        Description = "An ASP.NET Core Web API for managing Notification",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Example Contact",
            Url = new Uri("https://example.com/contact")
        },
        License = new OpenApiLicense
        {
            Name = "Example License",
            Url = new Uri("https://example.com/license")
        }
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger(options =>
    //{
    //    options.SerializeAsV2 = true;
    //});
    //app.UseSwaggerUI(options =>
    //{
    //    //options.SwaggerEndpoint("/swagger/index.html", "v1");
    //    //options.RoutePrefix = string.Empty;
    //});

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint($"v1/swagger.json", "NotifyAPIService");
    });

}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
