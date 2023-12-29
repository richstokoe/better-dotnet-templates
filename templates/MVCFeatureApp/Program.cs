namespace MvcFeatureApp;

public class Program
{
    public static void Main(string[] args)
    {
        // Add dependencies to SetupServices.cs
        var app = WebApplication.CreateBuilder(args).ConfigureServices();

        app.ConfigurePipeline();

        // Add minimal API routes here:
        // app.MapGet("/me", () => new { FirstName = "Joan", LastName = "Smith" });

        app.Run();
    }
}