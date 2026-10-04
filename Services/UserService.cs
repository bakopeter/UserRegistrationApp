using Microsoft.EntityFrameworkCore;
using UserRegistrationApp.Data;
using UserRegistrationApp.Models;

namespace UserRegistrationApp.Services;

public class UserService
{
    public async Task<bool> RegisterUserAsync(string username, string email, string rawPassword)
    {
        using var db = new AppDbContext();

        // 1. EMAIL NORMALIZÁLÁS (Tisztítás és kisbetűsítés)
        string normalizedEmail = email.Trim().ToLowerInvariant();

        // 2. ELSŐDLEGES ELLENŐRZÉS (C# / LINQ oldalon)
        bool emailExists = await db.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (emailExists)
        {
            Console.WriteLine($"[FIGYELMEZTETÉS] A(z) '{normalizedEmail}' email cím már regisztrálva van!");
            return false;
        }

        var newUser = new User
        {
            UserName = username,
            Email = normalizedEmail,
            PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rawPassword))
        };

        await db.Users.AddAsync(newUser);

        // 3. BIZTONSÁGI HÁLÓ (Kivételkezelés az adatbázis szintű hiba elkapására)
        try
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"[SIKER] Regisztráció sikeres! Új Felhasználó ID: {newUser.Id}");
            return true;
        }
        catch (DbUpdateException)
        {
            // Ha párhuzamos kérések vagy egyéb ok miatt az adatbázis dobja a UNIQUE hibát
            Console.WriteLine($"[ADATBÁZIS HIBA] A(z) '{normalizedEmail}' email címmel már létezik fiók!");
            return false;
        }
    }
}