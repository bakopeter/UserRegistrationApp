# Entity Framework Core & Migrációk — Teljes Útmutató és Gyakorlati Kézikönyv

Ez a kézikönyv átfogó áttekintést nyújt a .NET **Entity Framework Core (EF Core)** keretrendszeréről, a sémamigrációk működéséről, a terminálban használható fejlesztői eszközökről, valamint egy komplett regisztrációs alkalmazás megvalósításáról és a felmerülő gyakori hibák elhárításáról.

---

## 1. Elméleti Áttekintés

### Mi az az Entity Framework Core?
Az **Entity Framework Core (EF Core)** a Microsoft hivatalos, nyílt forráskódú **Object-Relational Mapper (ORM)** keretrendszere .NET platformra. 

A klasszikus adatbázis-kezelés során a fejlesztőnek manuálisan kell SQL parancsokat írnia (`SELECT`, `INSERT`, `UPDATE`, `DELETE`) és az adatbázis rekordjait kézzel kell átalakítania C# objektumokká (vagy fordítva). Az ORM keretrendszer áthidalja ezt a szakadékot:
* Az adatbázis **tábláit C# osztályokként** (entitások) kezelhetjük.
* Az SQL lekérdezések helyett típusbiztos **LINQ (Language Integrated Query)** kifejezéseket használhatunk.
* Az adatbázis-műveleteket, a kapcsolatokat és a tranzakciókat a keretrendszer automatikusan kezeli.

### Mi a Migráció és hogyan működik?
A **migráció** az adatbázis sémájának verziókövető rendszere. Ahogyan a Git segítségével követjük a forráskód változásait, úgy a migrációkkal követhetjük az adatbázis szerkezetének (táblák, oszlopok, indexek, kulcsok) fejlődését.

A **Code-First (Kód-első)** megközelítés működési folyamata:
1. **Modellmódosítás:** Módosítod a C# entitásosztályokat (pl. új tulajdonságot adsz a `User` osztályhoz) vagy a `DbContext` konfigurációt.
2. **Migráció generálása (`dotnet ef migrations add`):** Az EF Core összehasonlítja a C# modell jelenlegi állapotát a legutóbbi állapot pillanatképével (`DbContextSnapshot`). A különbségekből legyárt egy C# fájlt, amely tartalmazza az `Up()` (módosítások alkalmazása) és `Down()` (visszagörgetés) metódusokat.
3. **Adatbázis frissítése (`dotnet ef database update`):** Az EF Core lefordítja a migrációs fájl C# kódját a céladatbázis saját SQL DDL nyelvévé (pl. `CREATE TABLE`, `ALTER TABLE`), majd lefutatja azt.
4. **Állapotkövetés (`__EFMigrationsHistory`):** Az EF Core a céladatbázisban létrehoz egy speciális `__EFMigrationsHistory` nevű táblát. Ebben tárolja a már lefutott migrációk azonosítóit, így pontosan tudja, mely változtatásokat kell még alkalmaznia.

---

## 2. `dotnet ef` Terminál Parancsok Gyűjteménye

A parancsok használatához a `dotnet-ef` globális eszközt kell telepíteni (`dotnet tool install --global dotnet-ef`), valamint a projekthez hozzá kell adni a `Microsoft.EntityFrameworkCore.Design` NuGet csomagot.

| Parancs | Leírás | Használati eset |
| :--- | :--- | :--- |
| `dotnet ef migrations add <Név>` | Új migrációs C# fájlt hoz létre a legutóbbi modellváltozások alapján. | Amikor új entitást hozol létre vagy módosítasz egy meglévő tulajdonságot. |
| `dotnet ef migrations list` | Kilistázza az összes migrációt és jelzi, melyik futott le az adatbázison. | Ellenőrzésre, hogy az adatbázis szinkronban van-e a kóddal. |
| `dotnet ef migrations remove` | Eltávolítja a legutolsó migrációt a forráskódból. | Ha hibáztál a migráció létrehozásakor (csak akkor működik, ha még nem futott le az adatbázison!). |
| `dotnet ef migrations script` | SQL szkriptet generál a migrációkból. | CI/CD pipeline-okhoz és éles (production) adatbázis frissítésekhez. |
| `dotnet ef database update` | Alkalmazza az összes le nem futott migrációt az adatbázison. | Fejlesztés közben az adatbázis naprakész állapotba hozására. |
| `dotnet ef database update <Név>` | Egy konkrét migrációs állapotba lépteti az adatbázist. | Korábbi állapotra való visszagörgetéshez (rollback). |
| `dotnet ef database drop` | Törli a teljes céladatbázist. | Tiszta lap megteremtésére fejlesztői környezetben. |
| `dotnet ef dbcontext info` | Információt ad a `DbContext`-ről és a használt adatbázis-szolgáltatóról. | Kapcsolatok és konfigurációk ellenőrzésére. |
| `dotnet ef dbcontext scaffold "<ConnString>" <Provider>` | Meglévő adatbázisból generál C# entitásokat és `DbContext`-et. | Database-First megközelítés esetén, meglévő rendszereknél. |

---

## 3. Lépésről Lépésre Útmutató: Regisztrációs Alkalmazás

### 1. Lépés: Projekt és függőségek előkészítése

Hozzuk létre a konzolalkalmazást, a Solution fájlt, és telepítsük az EF Core SQLite csomagokat:

```bash
# 1. Konzol projekt létrehozása
dotnet new console -n UserRegistrationApp
cd UserRegistrationApp

# 2. Solution fájl létrehozása és projekt hozzáadása (Visual Studio kompatibilitás)
dotnet new sln
dotnet sln add UserRegistrationApp.csproj

# 3. EF Core SQLite provider és Design csomagok telepítése
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.*
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.*
```

A **`UserRegistrationApp.csproj`** fájl tartalmának ellenőrzése:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.*" />
  </ItemGroup>

</Project>
```

---

### 2. Lépés: Az Adatmodell (User.cs) megírása

Hozd létre a `Models/User.cs` fájlt:

```csharp
namespace UserRegistrationApp.Models;

public class User
{
    public int Id { get; set; } // Elsődleges kulcs (Primary Key, Auto-Increment)
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

### 3. Lépés: A DbContext megírása (Data/AppDbContext.cs)

Hozd létre a `Data/AppDbContext.cs` fájlt. 

> **FONTOS DÍZÁJNDÖNTÉS:** Az SQLite adatbázis elérési útját az `AppContext.BaseDirectory` használatával adjuk meg. Ez megelőzi azt a gyakori hibát, hogy a parancssor és a futó alkalmazás két különböző helyre hozza létre az `app.db` fájlt!

```csharp
using Microsoft.EntityFrameworkCore;
using UserRegistrationApp.Models;

namespace UserRegistrationApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Abszolút útvonal képzése a futási könyvtárhoz
        string dbPath = System.IO.Path.Combine(AppContext.BaseDirectory, "app.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Egyedi (Unique) index beállítása az Email mezőre Fluent API segítségével
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}
```

---

### 4. Lépés: Migráció generálása és futtatása

Futtasd a terminálban az alábbi parancsot a migráció létrehozásához:

```bash
dotnet ef migrations add InitialCreate
```

Ekkor létrejön a `Migrations/` mappa a szükséges C# kóddal. Az adatbázis manuális frissítéséhez futtathatod a `dotnet ef database update` parancsot, vagy rábízhatod a kódra az automatikus frissítést (lásd következő lépés).

---

### 5. Lépés: A Regisztrációs Üzleti Logika (Services/UserService.cs)

Hozd létre a `Services/UserService.cs` fájlt, amely kezeli az email kisbetűsítését (normalizálás), a duplikáció ellenőrzését és az adatbázis-szintű kivételkezelést:

```csharp
using Microsoft.EntityFrameworkCore;
using UserRegistrationApp.Data;
using UserRegistrationApp.Models;

namespace UserRegistrationApp.Services;

public class UserService
{
    public async Task<bool> RegisterUserAsync(string username, string email, string rawPassword)
    {
        using var db = new AppDbContext();

        // 1. Email normalizálása (szóközök eltávolítása és kisbetűsítés)
        string normalizedEmail = email.Trim().ToLowerInvariant();

        // 2. C# oldali ellenőrzés (LINQ-to-SQL átfordítás)
        bool emailExists = await db.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (emailExists)
        {
            Console.WriteLine($"[FIGYELMEZTETÉS] A(z) '{normalizedEmail}' email cím már regisztrálva van!");
            return false;
        }

        // 3. Új entitás példányosítása
        var newUser = new User
        {
            Username = username,
            Email = normalizedEmail,
            PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rawPassword))
        };

        // 4. Change Tracker-be helyezés (State = Added)
        await db.Users.AddAsync(newUser);

        // 5. Mentés az adatbázisba biztonsági hálóval (DbUpdateException elkapása)
        try
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"[SIKER] Regisztráció sikeres! Új Felhasználó ID: {newUser.Id}");
            return true;
        }
        catch (DbUpdateException)
        {
            Console.WriteLine($"[ADATBÁZIS HIBA] A(z) '{normalizedEmail}' email címmel már létezik fiók!");
            return false;
        }
    }
}
```

---

### 6. Lépés: A Belépési Pont (Program.cs)

Egészítsd ki a `Program.cs` fájlt az automatikus migrációfuttatással (`db.Database.Migrate()`) és a teszt hívásokkal:

```csharp
using Microsoft.EntityFrameworkCore;
using UserRegistrationApp.Data;
using UserRegistrationApp.Services;

// 1. Automatikus adatbázis-létezés és migráció ellenőrzés indításkor
using (var db = new AppDbContext())
{
    db.Database.Migrate();
}

// 2. Szolgáltatás példányosítása és regisztrációs tesztek
var userService = new UserService();

Console.WriteLine("=== REGISZTRÁCIÓ TESZT ===");

// Első próbálkozás (Sikeres)
await userService.RegisterUserAsync("kovacs_janos", "Janos@Example.com", "TitkosJelszo123");

// Második próbálkozás ugyanazzal az email címmel (Elutasítva)
await userService.RegisterUserAsync("pisti99", "janos@example.com", "MasikJelszo");
```

---

## 4. Menet Közben Felmerült Hibák és Hibaelhárítás

A fejlesztési folyamat során tapasztalt hibák elemzése és végleges megoldásuk:

### 1. Hiba: `CS0246: The type or namespace name 'DbContextOptionBuilder' could not be found`
* **Ok:** Gépelési hiba (typo) a `DbContextOptionsBuilder` osztály nevében (hiányzott a többes számú `s` betű az `Options` végén).
* **Megoldás:** Javítsd a metódus paraméterét: `protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)`.

### 2. Hiba: `CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement`
* **Ok:** A Fluent API láncolása közben felesleges pontosvessző (`;`) került az egyik sor végére, vagy lemaradt a zárójelpár `()` a metódushívás végéről (pl. `.IsUnique`).
* **Megoldás:** Ügyelj arra, hogy a láncolat csak a legvégén tartalmazzon pontosvesszőt:
  ```csharp
  modelBuilder.Entity<User>()
      .HasIndex(u => u.Email)
      .IsUnique(); // Helyes szintaxis
  ```

### 3. Hiba: `The current Visual Studio version does not support targeting .NET 10.0`
* **Ok:** A `dotnet new` a gépen lévő legújabb kísérleti SDK-t (`net10.0`) állította be, amelyet a telepített Visual Studio még nem támogatott.
* **Megoldás:** Állítsd át a `.csproj` fájlban a `<TargetFramework>` értékét `net8.0`-ra és frissítsd a NuGet verziókat `8.0.*`-ra.

### 4. Hiba: `SqliteException: SQLite Error 1: 'no such table: Users'` & Dupla adatbázisfájlok
* **Ok:** A relatív `"Data Source=app.db"` megadás miatt a `dotnet ef` parancsok a projekt gyökerében hozták létre az `app.db`-t, míg az alkalmazás futás közben a `bin/Debug/net8.0/app.db` üres fájlt kereste. A `dotnet ef database drop` csak a gyökérben lévő fájlt törölte.
* **Megoldás:** 
  1. Használj abszolút útvonalat az `AppDbContext`-ben: `System.IO.Path.Combine(AppContext.BaseDirectory, "app.db")`.
  2. Hívd meg a `db.Database.Migrate()` metódust az alkalmazás indításakor a `Program.cs`-ben.

### 5. Hiba: `SqliteException: SQLite Error 19: 'UNIQUE constraint failed: Users.Email'`
* **Ok:** Az adatbázis sikeresen érvényesítette az egyediségi szabályt. Kivételkezelés nélkül ez leállítja a programot.
* **Megoldás:** Tarts fenn biztonsági hálót! Csomagold a `db.SaveChangesAsync()` hívást `try-catch (DbUpdateException)` blokkba, és használj email normalizálást (`Trim().ToLowerInvariant()`).

### 6. Kérdés: Visual Studio Solution (`.sln`) fájl hiánya CLI használatakor
* **Ok:** A `dotnet new console` önmagában nem hoz létre `.sln` fájlt, de a Visual Studio igényli azt a mentéshez és a projektek kezeléséhez.
* **Megoldás:** Hozd létre terminálból a `dotnet new sln` és `dotnet sln add UserRegistrationApp.csproj` parancsokkal, és mentsd közvetlenül a `.csproj` mellé.

### 7. Kérdés: Top-Level Statements vs. Klasszikus `Program.Main`
* **Ok:** A `dotnet new console` C# 9+ óta nem generál `class Program` és `static void Main` keretet.
* **Tudnivaló:** Ez csak szintaktikai tisztítás, a C# fordító a háttérben automatikusan legyártja a `Program` osztályt. Mindkét formátum 100%-ban egyenértékű.

---

## 5. Éles Környezeti Ajánlások (Production Best Practices)

1. **Ne használj `db.Database.Migrate()`-et nagyvállalati/éles környezetben:**
   Több párhuzamos alkalmazáspéldány (pl. Kubernetes pod-ok) esetén versenyhelyzet (race condition) alakulhat ki. Élesben generálj SQL szkriptet a CI/CD pipeline-ban:
   ```bash
   dotnet ef migrations script -i -o apply_migrations.sql
   ```
2. **Soha ne tárolj jelszót sima szövegként vagy egyszerű Base64-ként:**
   Használj biztonságos jelszó-hashelő algoritmust (pl. `BCrypt.Net` vagy ASP.NET Core `PasswordHasher<T>`).
3. **Tranzakciók:**
   Ha több táblát módosítasz egyszerre, a `SaveChangesAsync()` automatikusan egyetlen tranzakcióban hajtja végre azokat.
