# Better .NET Templates
Better Templates provides templates for .NET developers that are designed around good practices and patterns.

[![.NET](https://github.com/richstokoe/better-dotnet-templates/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/richstokoe/better-dotnet-templates/actions/workflows/dotnet.yml)

The default .NET templates are revised frequently to showcase new runtime features, which often means reinforcing anti-patterns (technology-centric folder layouts, an ever-expanding `Program.cs`, mixed concerns). Better Templates aims to provide a set of stable templates that help you build long-lasting, well-architected applications that scale.

## Installation

```
dotnet new install RichStokoe.BetterTemplates
```

Or install locally during development:

```
dotnet new install <path-to-nupkg>
```

---

## MVC with Feature Slices (`better-mvc`)

The standard `dotnet new mvc` template organises code by technology layer: a `Controllers/` folder, a `Models/` folder, a `Views/` folder. This feels tidy at first, but as an application grows it becomes a navigation burden. Adding a new "Checkout" feature means touching three separate top-level folders. The folders become ever-expanding dumping grounds rather than meaningful boundaries.

With the "better-mvc" template, code is organised by feature. Each feature gets its own folder containing everything it owns:

```
MyApp/
├── Home/
│   ├── HomeController.cs
│   └── Views/
│       ├── Index.cshtml
│       └── Privacy.cshtml
├── Checkout/
│   ├── CheckoutController.cs
│   ├── CheckoutService.cs
│   ├── CheckoutViewModel.cs
│   └── Views/
│       ├── Index.cshtml
│       └── Confirm.cshtml
├── Shared/
│   ├── ErrorViewModel.cs
│   └── Views/
│       ├── _Layout.cshtml
│       └── Error.cshtml
├── SetupServices.cs
├── SetupPipeline.cs
└── Program.cs
```

The Razor view engine is configured to look for views in `/{Feature}/Views/{Action}.cshtml` before falling back to the conventional `Views/{Controller}/{Action}.cshtml` paths, so you can adopt the feature-slice layout incrementally when migrating an existing app.

`Program.cs` stays thin. Service registrations live in `SetupServices.cs` and middleware configuration in `SetupPipeline.cs`, both as extension methods on the builder/app — easy to find, easy to split further if needed.

**Usage**

```
dotnet new better-mvc -n MyApp
dotnet new better-mvc -n MyApp --framework net8.0
```

Supported frameworks: `net8.0`, `net9.0`, `net10.0` (default).

---

## React + TypeScript SPA with MVC Back End (`better-hybrid`)

Microsoft have mostly abandoned their SPA templates. The original `dotnet new react` template used a development proxy and `UseProxyToSpaDevelopmentServer`, which added configuration complexity and a fragile dev-time dependency. More fundamentally, it didn't give you a clear model for how the front and back ends would relate in production.

The BetterTemplates template produces a single deployable .NET application that serves a Vite-built React front end as static files from `wwwroot/`, alongside an MVC back end for server-side concerns (authentication flows, API endpoints, error pages, server-rendered fallbacks).

The React app lives in `ClientApp/` and is built with Vite. `vite.config.ts` outputs directly into `wwwroot/` so there are no separate deployment artefacts to coordinate. `MapFallbackToFile("index.html")` means the React router handles all client-side routes, while MVC controllers intercept any routes explicitly registered (e.g. `/login`, `/error`).

The MVC back end uses the same feature-slice layout as `better-mvc`:

```
MyApp/
├── ClientApp/           # React + TypeScript (Vite)
│   └── src/
├── Home/
│   ├── HomeController.cs
│   └── Views/
│       └── Error.cshtml
├── Shared/
│   ├── ErrorViewModel.cs
│   └── Views/
│       └── _Layout.cshtml
├── wwwroot/             # Built SPA output lands here
├── SetupServices.cs
├── SetupPipeline.cs
└── Program.cs
```

The `.csproj` automatically runs `npm install` on a debug build if `node_modules` is missing, and runs `npm run build` as part of `dotnet publish`.

**Usage**

```
dotnet new better-hybrid -n MyApp
dotnet new better-hybrid -n MyApp --Framework net8.0
```

Supported frameworks: `net8.0`, `net9.0`, `net10.0` (default).

**Prerequisites**: Node.js must be installed for the front-end build steps.

---

## Contributing

Templates are under `templates/`. The package is built with:

```
dotnet pack -c Release
```

To test locally after building:

```
./reinstalltemplate.sh
```

---

## License

[MIT](LICENSE) — provided as-is, without warranty of any kind. See the [LICENSE](LICENSE) file for the full terms.
