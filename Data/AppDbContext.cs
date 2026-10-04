using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using UserRegistrationApp.Models;

namespace UserRegistrationApp.Data;

internal class AppDbContext : DbContext
{
    //A User tábla leképezése
    public DbSet<User> Users { get; set; } = null!;

    //Adatbázis-kapcsolat konfigurálása
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Abszolút útvonal előállítása a futási könyvtárhoz
        string dbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    //Séma szabályok konfigurálása Fluent API-val
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Biztosítjuk, hogy az email cím egyedi legyen az adatbázisban
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}
