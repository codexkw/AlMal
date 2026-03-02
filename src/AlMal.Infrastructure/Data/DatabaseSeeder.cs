using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AlMalDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AlMalDbContext>>();

        try
        {
            await SeedSuperAdminAsync(userManager, logger);
            await SeedSectorsAsync(context, logger);
            await SeedStocksAsync(context, logger);
            await SeedMarketIndicesAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string email = "admin@almal.kw";

        if (await userManager.FindByEmailAsync(email) != null)
        {
            logger.LogInformation("SuperAdmin user already exists, skipping");
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = "مدير النظام",
            EmailConfirmed = true,
            UserType = UserType.CertifiedAnalyst,
            IsVerified = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, "Admin@AlMal2026!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "SuperAdmin");
            logger.LogInformation("SuperAdmin user created: {Email}", email);
        }
        else
        {
            logger.LogWarning("Failed to create SuperAdmin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedSectorsAsync(AlMalDbContext context, ILogger logger)
    {
        if (await context.Sectors.AnyAsync())
        {
            logger.LogInformation("Sectors already seeded, skipping");
            return;
        }

        var sectors = new List<Sector>
        {
            new() { NameAr = "البنوك", NameEn = "Banks", SortOrder = 1 },
            new() { NameAr = "الخدمات المالية", NameEn = "Financial Services", SortOrder = 2 },
            new() { NameAr = "التأمين", NameEn = "Insurance", SortOrder = 3 },
            new() { NameAr = "العقار", NameEn = "Real Estate", SortOrder = 4 },
            new() { NameAr = "الصناعة", NameEn = "Industrials", SortOrder = 5 },
            new() { NameAr = "الخدمات الاستهلاكية", NameEn = "Consumer Services", SortOrder = 6 },
            new() { NameAr = "السلع الاستهلاكية", NameEn = "Consumer Goods", SortOrder = 7 },
            new() { NameAr = "الاتصالات", NameEn = "Telecommunications", SortOrder = 8 },
            new() { NameAr = "التكنولوجيا", NameEn = "Technology", SortOrder = 9 },
            new() { NameAr = "الرعاية الصحية", NameEn = "Healthcare", SortOrder = 10 },
            new() { NameAr = "المواد الأساسية", NameEn = "Basic Materials", SortOrder = 11 },
            new() { NameAr = "النفط والغاز", NameEn = "Oil & Gas", SortOrder = 12 },
            new() { NameAr = "المرافق", NameEn = "Utilities", SortOrder = 13 }
        };

        context.Sectors.AddRange(sectors);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} sectors", sectors.Count);
    }

    private static async Task SeedStocksAsync(AlMalDbContext context, ILogger logger)
    {
        if (await context.Stocks.AnyAsync())
        {
            logger.LogInformation("Stocks already seeded, skipping");
            return;
        }

        var banks = await context.Sectors.FirstAsync(s => s.NameEn == "Banks");
        var financial = await context.Sectors.FirstAsync(s => s.NameEn == "Financial Services");
        var telecom = await context.Sectors.FirstAsync(s => s.NameEn == "Telecommunications");
        var realEstate = await context.Sectors.FirstAsync(s => s.NameEn == "Real Estate");
        var industrials = await context.Sectors.FirstAsync(s => s.NameEn == "Industrials");
        var consumer = await context.Sectors.FirstAsync(s => s.NameEn == "Consumer Services");
        var insurance = await context.Sectors.FirstAsync(s => s.NameEn == "Insurance");
        var oilGas = await context.Sectors.FirstAsync(s => s.NameEn == "Oil & Gas");

        var stocks = new List<Stock>
        {
            // Banks
            new() { Symbol = "NBK", NameAr = "بنك الكويت الوطني", NameEn = "National Bank of Kuwait", SectorId = banks.Id, LastPrice = 1.020m, DayChange = 0.010m, DayChangePercent = 0.99m, IsActive = true },
            new() { Symbol = "KFH", NameAr = "بيت التمويل الكويتي", NameEn = "Kuwait Finance House", SectorId = banks.Id, LastPrice = 0.880m, DayChange = -0.005m, DayChangePercent = -0.57m, IsActive = true },
            new() { Symbol = "GBK", NameAr = "بنك الخليج", NameEn = "Gulf Bank", SectorId = banks.Id, LastPrice = 0.310m, DayChange = 0.003m, DayChangePercent = 0.98m, IsActive = true },
            new() { Symbol = "ABK", NameAr = "البنك الأهلي الكويتي", NameEn = "Al Ahli Bank of Kuwait", SectorId = banks.Id, LastPrice = 0.290m, DayChange = -0.002m, DayChangePercent = -0.68m, IsActive = true },
            new() { Symbol = "BURG", NameAr = "بنك برقان", NameEn = "Burgan Bank", SectorId = banks.Id, LastPrice = 0.228m, DayChange = 0.001m, DayChangePercent = 0.44m, IsActive = true },

            // Telecommunications
            new() { Symbol = "ZAIN", NameAr = "زين", NameEn = "Zain Group", SectorId = telecom.Id, LastPrice = 0.620m, DayChange = 0.008m, DayChangePercent = 1.31m, IsActive = true },
            new() { Symbol = "OOREDOO", NameAr = "أوريدو الكويت", NameEn = "Ooredoo Kuwait", SectorId = telecom.Id, LastPrice = 0.790m, DayChange = -0.010m, DayChangePercent = -1.25m, IsActive = true },
            new() { Symbol = "STC", NameAr = "الاتصالات الكويتية", NameEn = "STC Kuwait", SectorId = telecom.Id, LastPrice = 0.860m, DayChange = 0.005m, DayChangePercent = 0.58m, IsActive = true },

            // Financial Services
            new() { Symbol = "KIA", NameAr = "مجموعة المشاريع الاستثمارية", NameEn = "KIPCO", SectorId = financial.Id, LastPrice = 0.172m, DayChange = 0.002m, DayChangePercent = 1.18m, IsActive = true },
            new() { Symbol = "AAYAN", NameAr = "أعيان للإجارة والاستثمار", NameEn = "Aayan Leasing", SectorId = financial.Id, LastPrice = 0.085m, DayChange = -0.001m, DayChangePercent = -1.16m, IsActive = true },

            // Real Estate
            new() { Symbol = "MABANEE", NameAr = "مبانئ", NameEn = "Mabanee Company", SectorId = realEstate.Id, LastPrice = 0.830m, DayChange = 0.015m, DayChangePercent = 1.84m, IsActive = true },
            new() { Symbol = "ALIMTIAZ", NameAr = "الامتياز للاستثمار", NameEn = "Al Imtiaz Investment", SectorId = realEstate.Id, LastPrice = 0.115m, DayChange = -0.003m, DayChangePercent = -2.54m, IsActive = true },
            new() { Symbol = "SALHIA", NameAr = "الصالحية العقارية", NameEn = "Salhia Real Estate", SectorId = realEstate.Id, LastPrice = 0.400m, DayChange = 0.000m, DayChangePercent = 0.00m, IsActive = true },

            // Industrials
            new() { Symbol = "AGILITY", NameAr = "أجيليتي", NameEn = "Agility Public Warehousing", SectorId = industrials.Id, LastPrice = 0.950m, DayChange = 0.020m, DayChangePercent = 2.15m, IsActive = true },
            new() { Symbol = "HUMANSOFT", NameAr = "هيومن سوفت", NameEn = "Humansoft Holding", SectorId = industrials.Id, LastPrice = 3.400m, DayChange = -0.040m, DayChangePercent = -1.16m, IsActive = true },

            // Consumer Services
            new() { Symbol = "AMERICANA", NameAr = "أمريكانا", NameEn = "Americana Restaurants", SectorId = consumer.Id, LastPrice = 2.180m, DayChange = 0.030m, DayChangePercent = 1.40m, IsActive = true },
            new() { Symbol = "JAZEERA", NameAr = "طيران الجزيرة", NameEn = "Jazeera Airways", SectorId = consumer.Id, LastPrice = 1.020m, DayChange = -0.015m, DayChangePercent = -1.45m, IsActive = true },

            // Insurance
            new() { Symbol = "GIG", NameAr = "الخليج للتأمين", NameEn = "Gulf Insurance Group", SectorId = insurance.Id, LastPrice = 0.720m, DayChange = 0.006m, DayChangePercent = 0.84m, IsActive = true },

            // Oil & Gas
            new() { Symbol = "KPETRO", NameAr = "البترول الوطنية الكويتية", NameEn = "Kuwait National Petroleum", SectorId = oilGas.Id, LastPrice = 0.165m, DayChange = -0.002m, DayChangePercent = -1.20m, IsActive = true },
            new() { Symbol = "IFA", NameAr = "مجموعة إيفا للفنادق والمنتجعات", NameEn = "IFA Hotels & Resorts", SectorId = oilGas.Id, LastPrice = 0.098m, DayChange = 0.001m, DayChangePercent = 1.03m, IsActive = true },
        };

        context.Stocks.AddRange(stocks);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} stocks", stocks.Count);
    }

    private static async Task SeedMarketIndicesAsync(AlMalDbContext context, ILogger logger)
    {
        if (await context.MarketIndices.AnyAsync())
        {
            logger.LogInformation("Market indices already seeded, skipping");
            return;
        }

        var indices = new List<MarketIndex>
        {
            new() { NameAr = "المؤشر العام", Type = MarketIndexType.Main, Value = 7850.230m, Change = 15.450m, ChangePercent = 0.20m, Date = DateTime.UtcNow },
            new() { NameAr = "مؤشر السوق الأول", Type = MarketIndexType.Premier, Value = 8520.100m, Change = -12.300m, ChangePercent = -0.14m, Date = DateTime.UtcNow },
            new() { NameAr = "مؤشر السوق الرئيسي", Type = MarketIndexType.Main, Value = 6210.870m, Change = 8.200m, ChangePercent = 0.13m, Date = DateTime.UtcNow },
        };

        context.MarketIndices.AddRange(indices);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} market indices", indices.Count);
    }
}
