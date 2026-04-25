using System.Text.Json;
using System.Text.Json.Serialization;

namespace MvcFeatureApp;

public static class SetupServices
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services
            // Adds common Mvc features
            .AddControllersWithViews()
            // Adds feature-slice view engine
            .AddFeatureSliceViewEngine();

        // Some sensible JSON serialization defaults
        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opts =>
        {
            opts.SerializerOptions.Converters.Clear();
            opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        return builder.Build();
    }

    private static IMvcBuilder AddFeatureSliceViewEngine(this IMvcBuilder builder)
    {
        builder.AddRazorOptions(options =>
        {
            // {0} - Action Name
            // {1} - Controller Name
            // {2} - Area Name

            options.ViewLocationFormats.Clear();
            options.AreaViewLocationFormats.Clear();

            // Views

            // Override default view lookup locations
            options.ViewLocationFormats.Add("/{1}/Views/{0}.cshtml");
            options.ViewLocationFormats.Add("/{1}/{0}.cshtml");
            options.ViewLocationFormats.Add("/Shared/Views/{0}.cshtml");

            // Fallback to default MVC view locations (supports phased migration to feature slices)
            options.ViewLocationFormats.Add("/Views/{1}/{0}.cshtml");
            options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");

            // Areas

            // Override the default area view lookup locations
            options.AreaViewLocationFormats.Add("/Areas/{2}/{1}/Views/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Areas/{2}/Shared/Views/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Shared/Views/{0}.cshtml");

            // Fallback to default MVC area view locations (supports phased migration to feature slices)
            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
            options.AreaViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
        });

        return builder;

    }
}