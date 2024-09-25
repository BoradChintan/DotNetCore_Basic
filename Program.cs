using DotNETBasic.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Net;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("https://localhost:7188");

// Add services to the container.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));


builder.Services.AddDistributedMemoryCache();



builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

// Add IHttpContextAccessor to the service collection
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<DataverseService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    return new DataverseService(configuration, httpContextAccessor);
});

// Chat service register
builder.Services.AddSingleton<ChatService>();

builder.Services.AddSingleton<WebSocketService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(6000);  
    options.Cookie.HttpOnly = true; 
    options.Cookie.IsEssential = true; 
});

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/AccessDenied");
    app.UseHsts();
}

// Apply session middleware
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("CorsPolicy"); // Enable CORS

app.UseWebSockets();
app.Map("/ws", async context =>
{
    Console.WriteLine("WebSocket request received.");
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var weatherWebSocket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("WebSocket accepted.");

        var webSocketService = app.Services.GetRequiredService<WebSocketService>();
        await webSocketService.HandleWebSocketAsync(weatherWebSocket);
    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        Console.WriteLine("Invalid WebSocket request.");
    }
});

// New WebSocket for chat
app.Map("/chat/ws", async context =>
{
    Console.WriteLine("Chat WebSocket request received.");
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var chatWebSocket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("Chat WebSocket accepted.");

        // Handle Chat WebSocket connection
        var chatWebSocketService = app.Services.GetRequiredService<ChatService>();
        await chatWebSocketService.HandleWebSocketAsync(chatWebSocket);
    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        Console.WriteLine("Invalid Chat WebSocket request.");
    }
});



app.UseRouting();

// Enable WebSockets
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
