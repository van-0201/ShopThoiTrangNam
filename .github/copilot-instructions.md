## ShopThoiTrangNam — Copilot instructions (concise)

This repository is an ASP.NET Core MVC web shop (net9.0) using Entity Framework Core and ASP.NET Identity. The goal of these instructions is to help an AI coding agent be immediately productive by highlighting project-specific patterns, developer workflows, and common pitfalls.

- Runtime & project entry: `Program.cs` (top-level statements). The app seeds Identity roles and two users (Admin and Customer) on startup using values from configuration keys `AdminCredentials` and `CustomerCredentials`.
- Data access: `Data/ApplicationDbContext.cs` — inherits `IdentityDbContext<ApplicationUser>` and exposes DbSets (Products, Categories, Orders, OrderDetails, ShoppingCarts, ProductImages, ApplicationUsers).

Quick run (developer machine, PowerShell):

1. Restore & build

   dotnet restore
   dotnet build

2. Apply EF migrations (Migrations/ exists) and update DB

   dotnet ef database update

3. Run the app

   dotnet run

Notes: the project uses SQL Server by default. Edit `appsettings.Development.json` or `appsettings.json` to set `ConnectionStrings:DefaultConnection`. If you prefer LocalDB, point `DefaultConnection` at `(localdb)\\mssqllocaldb`.

Key architecture & conventions (important for code changes):

- MVC pattern with server-rendered Razor views in `Views/` and controllers in `Controllers/`.
- Identity + Roles: Roles used are `Admin` and `Customer`. Controllers like `ShoppingCartController` use `[Authorize(Roles = "Customer")]` (see `Controllers/ShoppingCartController.cs`). The app seeds roles and admin/customer users on startup (see `Program.cs`).
- Product + variant model: variants are represented as separate `Product` rows. `Product.ParentProductId` links a variant to its parent/original product. Variant attributes include `Size`, `Color`, `Price`, `StockQuantity`. `StoreController.GetVariant` returns a JSON payload used by client-side code to switch variants.
- View data patterns: controllers frequently set `ViewBag.Categories`, `ViewBag.Colors`, `ViewBag.Sizes`, and `ViewData["SelectedCategoryId"]`. Use these exact keys when making view changes to avoid breaking existing pages.
- TempData messages: controllers set `TempData["SuccessMessage"]` and `TempData["ErrorMessage"]` for UI notifications (see `ProductsController.Create`). Respect these keys in layout or partials.
- AJAX endpoints: `StoreController.GetVariant` and `ShoppingCartController.AddToCart` return JSON responses used by client-side JS. Keep their shapes stable when refactoring.

Migrations & DB notes:

- A migration exists in `Migrations/20250928074047_InitialCreate.cs`. Use `dotnet ef database update` to apply. If changing the model, add migrations with `dotnet ef migrations add <Name>`.
- If `dotnet ef` isn't available, install or use the CLI tool: `dotnet tool install --global dotnet-ef` (or use Visual Studio Package Manager Console `Update-Database`).

Configuration & secrets:

- Admin and customer seed credentials are read from `AdminCredentials` and `CustomerCredentials` in configuration. By default these are in `appsettings.json` (example: `admin@shop.com` / `Admin@123`). To avoid committing secrets, override in `appsettings.Development.json`, environment variables, or user secrets.
- Common env var mapping:
  - Connection string: `ConnectionStrings__DefaultConnection`
  - Admin email: `AdminCredentials__Email`
  - Admin password: `AdminCredentials__Password`

Common pitfalls / debugging tips:

- If roles or users are not created on startup, check the configured connection string and that the DB is reachable. Seeding runs during app startup scope in `Program.cs`.
- If variants don't show or AddToCart fails, confirm the UI posts `productId`, `color`, and `size` (see `ShoppingCartController.AddToCart`). The code looks up a variant by `(ProductId == productId || ParentProductId == productId) && Size == size && Color == color`.
- There are two connection strings in `appsettings.json` (`DefaultConnection` and `ApplicationDbContext`). The app uses `DefaultConnection` in `Program.cs`. Prefer editing `DefaultConnection`.

Files to inspect when working on a feature/change:

- `Program.cs` — app startup, Identity seeding.
- `Data/ApplicationDbContext.cs` — EF DbSets and configuration.
- `Models/Product.cs` and `Models/ShoppingCart.cs` — data shape and variant keys.
- `Controllers/StoreController.cs`, `Controllers/ProductsController.cs`, `Controllers/ShoppingCartController.cs` — where product listing, detail, cart and variant logic live.
- `Views/Store/Details.cshtml`, `Views/Products/*`, `Views/Shared/_Layout.cshtml` — UI and client-side hooks (TempData, ViewBag keys, AJAX scripts).

When changing public APIs/JSON endpoints, update the callers in the Views/JS to match. Prefer backward-compatible JSON shapes if you expect in-place deployment.

If any section above is unclear or you want the file to include examples/snippets for one of the controllers or models, tell me which area to expand and I'll iterate.
