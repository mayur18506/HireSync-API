using System.Net;
using System.Net.Sockets;
using HireSync.API.Services;
using HireSync.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// React App CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// Gemini HttpClient with IPv4 Force (Fixes ISP Timeout Issue!)
builder.Services.AddHttpClient<IGeminiService, GeminiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(25);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    ConnectCallback = async (context, cancellationToken) =>
    {
        var entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, AddressFamily.InterNetwork, cancellationToken);
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(entry.AddressList, context.DnsEndPoint.Port, cancellationToken);
        return new NetworkStream(socket, ownsSocket: true);
    }
});

var app = builder.Build();

// Live Portfolio Demo ke liye Production mein bhi Swagger ON rakho
app.UseSwagger();
app.UseSwaggerUI();

// Root health check taaki Cloud URL kholte hi confirmation mile
app.MapGet("/", () => "HireSync AI .NET Backend is Live & Running! 🚀");

// Cloud SQL Server mein tables auto-create karne ke liye Auto-Migration
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration Error: {ex.Message}");
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();