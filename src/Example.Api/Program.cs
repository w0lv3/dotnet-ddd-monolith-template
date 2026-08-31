using Example.Api.Extensions;
using Example.Application;
using Example.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Configuration, builder.Environment.IsDevelopment());

var app = builder.Build();

app.UseApi();

app.Run();

public partial class Program;
