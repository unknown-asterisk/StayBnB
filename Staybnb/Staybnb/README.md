# Staybnb – ASP.NET Core 8 MVC + EF Core + SQL Server

## Requirements
- .NET 8 SDK
- SQL Server Express (or LocalDB)
- (Optional) SQL Server Management Studio, to browse the database

## Database
The connection string is in `appsettings.json`:

    Server=.\SQLEXPRESS;Database=StaybnbDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True

If your machine uses LocalDB instead, change `Server=` to `(localdb)\MSSQLLocalDB`.

The `Migrations` folder is included. On startup the app creates the database
if it doesn't exist, applies all migrations, and seeds the roles, the
SuperAdmin account and the amenities. No SQL scripts or manual setup are needed.

## How to run
1. Unzip the project and open a terminal in the folder containing `Staybnb.csproj`.
2. Run:

       dotnet restore
       dotnet run

3. Open http://localhost:5000 in your browser.

(Only if the Migrations folder is missing: `dotnet tool install --global dotnet-ef`
then `dotnet ef migrations add InitialCreate`.)

## Default login
| Role       | Email                    | Password   |
|------------|--------------------------|------------|
| SuperAdmin | superadmin@staybnb.com   | Admin@123  |

All other users register through the Sign up page and start as Guests.

## Roles
- **Guest:** browse and filter properties, book, check in (upload ID/Passport), message hosts.
- **Host:** granted when an Admin approves a host application (the Guest role is removed
  and the Host role added). Manages properties, booking requests, check-in process and
  guest document verification.
- **Admin:** reviews host applications and views the activity log.
- **SuperAdmin:** everything an Admin can do, plus promoting Guests to Admin.

## Suggested test flow
1. Log in as SuperAdmin to confirm the Admin dashboard loads.
2. Register a new user (Guest) and submit a property under **Become a Host**
   (an image is required).
3. As SuperAdmin, approve the application under **Host Requests**.
4. Log in as that user again. They now have the Host role and see the Host dashboard.
5. Register a second Guest and book the property.
6. As the Host, approve the booking under **Booking Requests**.
7. As the Guest, open **My Bookings → Check-in** and upload an ID.
8. As the Host, open **Check-ins**, verify the document and confirm check-in
   (booking status becomes CheckedIn).
9. As SuperAdmin, open **Users** to promote a Guest to Admin, then open **Activity Logs**.

## Booking price
TotalPrice = (PricePerNight × Nights) + CleaningFee + ServiceFee
(ServiceFee is 12% of the nightly subtotal.)

## Known limitations
- Reviews and wishlist exist in the data model but have no user interface yet.
- Payments are simulated: a payment is marked Paid when the host approves the booking.
- Messaging is database-backed (no real-time updates).
