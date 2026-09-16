using Soccer.Application.DependencyInjection;
using Soccer.Infrastructure.DependencyInjection;

// View > Terminal:
// cd react.client
// npm install

var builder = WebApplication.CreateBuilder(args);

string? connection = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddInfrastructure(connection);
builder.Services.AddApplication();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();