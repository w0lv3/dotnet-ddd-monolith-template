using Example.Api.Authentication;
using Example.Api.Extensions;
using Example.Api.OpenApi;
using Example.Application;
using Example.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices();
builder.Services.AddApiAuthentication(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddApiOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapApiOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
