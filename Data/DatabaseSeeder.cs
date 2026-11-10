using NMB_HLabSys.Models.Domain;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace NMB_HLabSys.Data;

public static class DatabaseSeeder
{
    private const string DemoPassword = "Demo123!";
    private const int UsersPerRole = 5;

    // ─────────────────────────────────────────────────────────────
    //  ROLES ONLY — 5 demo accounts per role (idempotent)
    // ─────────────────────────────────────────────────────────────
    public static void SeedRolesOnly(ApplicationDbContext db)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, workFactor: 12);

        SeedManagers(db, hash);
        SeedAdmins(db, hash);
        SeedDoctors(db, hash);
        SeedTechnicians(db, hash);
        SeedPatients(db, hash);
    }

    // ── shared helper ────────────────────────────────────────────
    private static User GetOrCreateUser(
        ApplicationDbContext db, string email, string role, string hash)
    {
        var existing = db.Users.FirstOrDefault(u => u.Email == email);
        if (existing != null) return existing;

        var u = new User
        {
            Email = email,
            PasswordHash = hash,
            Role = role,
            IsFirstLogin = false
        };
        db.Users.Add(u);
        db.SaveChanges();
        return u;
    }

    // ── 1. MANAGERS ──────────────────────────────────────────────
    private static void SeedManagers(ApplicationDbContext db, string hash)
    {
        var people = new (string Name, string Surname)[]
        {
            ("Nomsa",   "Mkhize"),
            ("Zola",    "Dlamini"),
            ("Patrick", "Mahlangu"),
            ("Yolanda", "Nkosi"),
            ("Sizwe",   "Mthembu")
        };

        for (int i = 0; i < UsersPerRole; i++)
        {
            var (name, surname) = people[i];
            var email = i == 0
                ? "manager@nmbhlabsys.local"
                : $"manager{i + 1}@nmbhlabsys.local";

            var u = GetOrCreateUser(db, email, "Manager", hash);
            if (!db.Managers.Any(m => m.UserId == u.UserId))
            {
                db.Managers.Add(new Manager
                {
                    UserId = u.UserId,
                    Name = name,
                    Surname = surname
                });
                db.SaveChanges();
            }
        }
    }

    // ── 2. ADMINS ────────────────────────────────────────────────
    private static void SeedAdmins(ApplicationDbContext db, string hash)
    {
        var people = new (string Name, string Surname)[]
        {
            ("System",   "Admin"),
            ("Lindiwe",  "Khumalo"),
            ("Thabo",    "Mokoena"),
            ("Ayesha",   "Patel"),
            ("Johan",    "van der Merwe")
        };

        for (int i = 0; i < UsersPerRole; i++)
        {
            var (name, surname) = people[i];
            var email = i == 0
                ? "admin@nmbhlabsys.local"
                : $"admin{i + 1}@nmbhlabsys.local";

            var u = GetOrCreateUser(db, email, "Admin", hash);
            if (!db.Admins.Any(a => a.UserId == u.UserId))
            {
                db.Admins.Add(new Admin
                {
                    UserId = u.UserId,
                    Name = name,
                    Surname = surname,
                    IsActive = true
                });
                db.SaveChanges();
            }
        }
    }

    // ── 3. DOCTORS ───────────────────────────────────────────────
    private static void SeedDoctors(ApplicationDbContext db, string hash)
    {
        var people = new (string Name, string Surname)[]
        {
            ("Thandiwe", "Mbeki"),
            ("Sipho",    "Radebe"),
            ("Lerato",   "Mokoena"),
            ("Pieter",   "Botha"),
            ("Naledi",   "Molefe")
        };

        for (int i = 0; i < UsersPerRole; i++)
        {
            var (name, surname) = people[i];
            var email = i == 0
                ? "doctor1@nmbhlabsys.local"
                : $"doctor{i + 1}@nmbhlabsys.local";

            var u = GetOrCreateUser(db, email, "Doctor", hash);
            if (!db.Doctors.Any(d => d.UserId == u.UserId))
            {
                db.Doctors.Add(new Doctor
                {
                    UserId = u.UserId,
                    Name = name,
                    Surname = surname,
                    HPCSANumber = $"MP{1001 + i}",
                    ContactNumber = $"041 555 {i + 1:D4}"
                });
                db.SaveChanges();
            }
        }
    }

    // ── 4. TECHNICIANS ───────────────────────────────────────────
    private static void SeedTechnicians(ApplicationDbContext db, string hash)
    {
        var people = new (string Name, string Surname)[]
        {
            ("Sizwe",       "Khumalo"),
            ("Ayanda",      "Ndlovu"),
            ("Bongani",     "Dlamini"),
            ("Zanele",      "Nkosi"),
            ("Kelebogile",  "Molefe")
        };

        for (int i = 0; i < UsersPerRole; i++)
        {
            var (name, surname) = people[i];
            var email = i == 0
                ? "tech1@nmbhlabsys.local"
                : $"tech{i + 1}@nmbhlabsys.local";

            var u = GetOrCreateUser(db, email, "Technician", hash);
            if (!db.Technicians.Any(t => t.UserId == u.UserId))
            {
                db.Technicians.Add(new Technician
                {
                    UserId = u.UserId,
                    Name = name,
                    Surname = surname,
                    EmployeeNumber = $"TECH-{i + 1:D3}",
                    SAIDNumber = $"900101500{i + 1:D4}"
                });
                db.SaveChanges();
            }
        }
    }

    // ── 5. PATIENTS ──────────────────────────────────────────────
    private static void SeedPatients(ApplicationDbContext db, string hash)
    {
        var people = new (string Name, string Surname)[]
        {
            ("Sandile",   "Mthembu"),
            ("Precious",  "Mahlangu"),
            ("Tshepo",    "Molefe"),
            ("Ntombi",    "Sithole"),
            ("Andile",    "Zulu")
        };

        for (int i = 0; i < UsersPerRole; i++)
        {
            var (name, surname) = people[i];
            var email = i == 0
                ? "patient1@nmbhlabsys.local"
                : $"patient{i + 1}@nmbhlabsys.local";

            var u = GetOrCreateUser(db, email, "Patient", hash);
            if (!db.Patients.Any(p => p.UserId == u.UserId))
            {
                db.Patients.Add(new Patient
                {
                    UserId = u.UserId,
                    Name = name,
                    Surname = surname,
                    SAIDNumber = $"9001015001{i + 1:D3}",
                    DateOfBirth = new DateTime(1990, 1, 1).AddYears(-i),
                    CellNumber = $"082 555 {i + 1:D4}",
                    HomeAddress = $"{i + 1} Main Street, Gqeberha"
                });
                db.SaveChanges();
            }
        }
    }
}