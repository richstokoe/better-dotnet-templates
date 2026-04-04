namespace HybridReactSpaWithMVCBackEnd;

internal static class SetupPipeline
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        // MVC handles server-rendered routes (auth, error pages, etc.)
        // No defaults — '/' falls through to the React SPA fallback below.
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller}/{action}/{id?}");

        // Serve the React app's index.html for any route not matched above.
        // The SPA then handles client-side routing.
        app.MapFallbackToFile("index.html");

        return app;
    }
}