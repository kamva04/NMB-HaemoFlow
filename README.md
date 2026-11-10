# NMB-HLabSys

NMB-HLabSys is an ASP.NET Core MVC web application for managing workflows at the Nelson Mandela Bay Haematology Laboratory. It provides role-based tools for patients, doctors, laboratory technicians, managers, and administrators.

## Features

- **Patients:** manage profile and medical history, submit and track test requests, view released results and reports, and manage doctor consent and appointments.
- **Doctors:** manage patient records and test requests, review patient history, propose appointments, and release results.
- **Technicians:** receive samples, manage laboratory work, capture and verify results, and handle consumable requests.
- **Managers:** manage test categories and types, staff assignments, suppliers, stock, supply requests, and reports.
- **Administrators:** manage medical-history categories and catalog entries, and sample types.
- **Shared workflows:** notifications, account and password management, PDF reports, and barcode support.

## Technology

- .NET 8 / ASP.NET Core MVC with Razor views
- Entity Framework Core 8 with SQL Server
- Bootstrap, jQuery, and Bootstrap Icons
- BCrypt password hashing
- iText 7 PDF generation and NetBarcode

## Prerequisites

- .NET 8 SDK
- SQL Server instance accessible to the application
- SMTP credentials if email features are required

## Configuration

The application reads configuration from `appsettings.json`, environment-specific settings, environment variables, and .NET user secrets. Set the database connection string and email settings for your environment. Do not commit credentials or production secrets.

For local development, open a terminal in the directory containing `NMB-HLabSys.csproj`, then initialize user secrets and set the values you need:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=NMB_HLabSys;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Email:SmtpServer" "smtp.example.com"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Email:From" "your-address@example.com"
dotnet user-secrets set "Email:Password" "your-smtp-password"
dotnet user-secrets set "Email:DisplayName" "NMB-HLabSys"
dotnet user-secrets set "Email:EnableSSL" "true"
dotnet user-secrets set "PasswordReset:PublicOrigin" "https://localhost:5001"
```

Adjust the SQL Server connection string for your authentication method and server. Configure SMTP values if you want the application to send email. User secrets are for local development; use your deployment platform's secret store for deployed environments.

## Run locally

From the project directory:

```powershell
dotnet restore
dotnet run
```

On startup, the application applies pending Entity Framework Core migrations and seeds demo users for each role. Ensure the configured SQL Server is running and the application can create or update its database before launching.

The exact local URL is printed by `dotnet run`. Open it in a browser and use HTTPS when prompted.

## Demo accounts

The seeder creates five demo accounts for each role and uses the same development password for the seeded users. The seed data is defined in `Data/DatabaseSeeder.cs`. These accounts and passwords are for development only; change or disable them before deploying to a shared or production environment.

## Project layout

```text
Controllers/   MVC controllers and application workflows
Data/          Entity Framework Core context and database seeding
Filters/       MVC filters, including first-login password enforcement
Helpers/       Shared validation and display helpers
Migrations/    Entity Framework Core database migrations
Models/        Domain entities and view models
Service/       Authentication, email, PDF, notification, and barcode services
Views/          Razor pages organized by controller
wwwroot/        Static assets, stylesheets, scripts, and libraries
```

## Database migrations

Migrations are applied automatically during application startup through `Database.Migrate()`. Add and review a migration when the data model changes, then verify it against a development database before deploying.

## Security notes

- Keep database and SMTP credentials out of source control; use .NET user secrets locally and an approved secrets manager for deployments.
- Rotate credentials if they have ever been committed or shared.
- Seeded demo accounts must not be used in production.
- Use HTTPS for deployed environments.