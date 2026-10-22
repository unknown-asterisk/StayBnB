# Staybnb – ASP.NET Core 8 MVC + EF Core + SQL Server

## Run
1. Install the .NET 8 SDK and SQL Server / LocalDB. Edit `ConnectionStrings:DefaultConnection` in appsettings.json if needed.
2. `dotnet tool install --global dotnet-ef`
3. `dotnet ef migrations add InitialCreate`   (generates the Migrations folder)
4. `dotnet run` - the app applies migrations and seeds roles + SuperAdmin on startup.

## Seeded SuperAdmin
superadmin@staybnb.com / Admin@123

## Demo flow
Register (Guest) -> Become a Host (with image) -> log in as SuperAdmin, approve (Guest role removed, Host added)
-> Host dashboard -> another Guest books -> Host approves -> Guest uploads ID -> Host verifies and confirms check-in.
SuperAdmin: Users -> promote a Guest to Admin. Admin: Activity Logs.
