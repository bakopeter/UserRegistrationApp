using Microsoft.EntityFrameworkCore;
using UserRegistrationApp.Data;
using UserRegistrationApp.Services;

// 1. Automatukus adatbázis-frissítés/létrehozás indításkor
using (var db = new AppDbContext())
{
    db.Database.Migrate();
}

// 2. A regisztrációs logika futtatása
var userService = new UserService();

Console.WriteLine("=== REGISZTRÁCIÓ TESZT ===");

// 1. Első regisztráció (Sikeresnek kell lennie)
await userService.RegisterUserAsync("kovacs_janos", "janos@example.com", "TitkosJelszo123");

// 2. Második regisztráció ugyanazzal az email címmel (El kell utasítani)
await userService.RegisterUserAsync("pisti_99", "janos@example.com", "MasikJelszo");