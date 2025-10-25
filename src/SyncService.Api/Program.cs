using SyncService.Core.Interfaces;
using SyncService.Core.Services;
using SyncService.Infrastructure.Services;
using SyncService.Infrastructure.Configuration; //  Needed for D365Config
using Microsoft.Extensions.Options; // Needed for IOptions


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Register custom application services
builder.Services.AddScoped<IExternalInventoryService, MockExternalInventoryService>();
// Comment out or remove the mock registration
//  builder.Services.AddScoped<ID365DataverseConnector, MockD365DataverseConnector>();
builder.Services.AddScoped<ISynchronizationOrchestrator, SynchronizationOrchestrator>();

// Bind the D365 config section
builder.Services.Configure<D365Config>(builder.Configuration.GetSection("D365"));

// Register HttpClientFactory
builder.Services.AddHttpClient("D365Client"); // Register a named HttpClient

// Register the real connector
builder.Services.AddScoped<ID365DataverseConnector, D365DataverseConnector>();

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