# NMB-HLabSys – Haematology Laboratory Management System

NMB-HLabSys is an ASP.NET Core MVC web application developed to digitise the management of haematology laboratory test requests, patient samples and test results at Nelson Mandela Bay Haematology Laboratory.

## How the System Works

The system follows a laboratory testing workflow:

1. **Login:** Users log in using their registered email and password. The system identifies their role and gives them access to the relevant dashboard.
2. **Doctor:** Doctors register or select patients, create laboratory test requests and select the required tests.
3. **Sample Collection:** Patients provide the required samples, which are identified using sample barcodes.
4. **Technician:** Technicians receive and verify samples, process assigned tests and capture laboratory results.
5. **Result Verification:** A different technician verifies captured results to support accuracy and quality control.
6. **Doctor Approval:** Doctors review and release verified results to patients.
7. **Patient:** Patients log in to view their test requests, track progress and access released results.

## User Roles

- **Admin:** Manages medical conditions, allergies, medications and sample types.
- **Manager:** Manages doctors, technicians, test types, laboratory consumables, suppliers and stock orders.
- **Doctor:** Manages patients, creates test requests, monitors progress and releases results.
- **Technician:** Receives samples, performs tests, captures results and verifies results.
- **Patient:** Views test requests, medical history, appointments and released results.

## Technologies Used

- C#
- ASP.NET Core 8 MVC
- Entity Framework Core
- Microsoft SQL Server
- HTML, CSS and JavaScript
- Bootstrap

## Getting Started

1. Clone the repository:

   ```bash
   git clone https://github.com/YOUR_USERNAME/NMB-HLabSys.git
   ```

2. Open `NMB-HLabSys.sln` in Visual Studio.
3. Configure the SQL Server connection string in `appsettings.json`.
4. Restore NuGet packages:

   ```bash
   dotnet restore
   ```

5. Apply database migrations using Package Manager Console:

   ```powershell
   Update-Database
   ```

6. Run the application:

   ```bash
   dotnet run
   ```

The database is also automatically migrated and seeded when the application starts.

## Security Features

- Role-Based Access Control (RBAC).
- BCrypt password hashing.
- Password change and reset functionality.
- Patient consent management.
- Session management.
- Protected access to role-specific pages.

## Author

**Kamva Ndebelefele**
