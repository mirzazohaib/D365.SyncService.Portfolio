using SyncService.Core.Interfaces;
using SyncService.Core.Services;
using SyncService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Register custom application services
builder.Services.AddScoped<IExternalInventoryService, MockExternalInventoryService>();
builder.Services.AddScoped<ID365DataverseConnector, MockD365DataverseConnector>();
builder.Services.AddScoped<ISynchronizationOrchestrator, SynchronizationOrchestrator>();

// 2. Register services for controllers and API documentation
builder.Services.AddControllers();          // Tells the app to use controller routing
builder.Services.AddEndpointsApiExplorer(); // Needed for Swagger
builder.Services.AddSwaggerGen();           // Needed for Swagger

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Routing for controllers
app.MapControllers();

app.Run();