using BetterWebAgent.Agents;
using BetterWebAgent.Chats;

namespace BetterWebAgent;

internal static class SetupPipeline
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        // SignalR hubs — clients connect via @microsoft/signalr in ClientApp/src/hooks.
        app.MapHub<ChatHub>("/hubs/chat");
        app.MapHub<AgentHub>("/hubs/agents");

        // MVC handles server-rendered routes (error pages etc.) — '/' falls through to the React SPA fallback.
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller}/{action}/{id?}");

        // Serve the React app's index.html for any route not matched above.
        app.MapFallbackToFile("index.html");

        return app;
    }
}
