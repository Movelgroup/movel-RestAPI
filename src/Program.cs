using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using apiEndpointNameSpace.Services;
using apiEndpointNameSpace.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register custom services
builder.Services.AddSingleton<IDataProcessor, DataProcessorService>();
builder.Services.AddSingleton<IFirestoreService>(sp => new FirestoreService(builder.Configuration["GoogleCloudProjectId"]));
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();

// Add SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<ChargerHub>("/chargerhub");

app.Run();