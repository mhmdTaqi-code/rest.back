using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Infrastructure.Data.Seed;

public static class IraqiDemoDataBuilder
{
    private static readonly Random Rng = new(100); // Fixed seed for stable demo data
    
    // Base Baghdad coordinates
    private const double BaseLat = 33.3152;
    private const double BaseLng = 44.3661;

    public static IReadOnlyList<SeedOwnerDefinition> GetMenuOwners()
    {
        return new[]
        {
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111111"),
                "Ahmed Al-Baghdadi",
                "baghdadgrill",
                "9647700000001",
                Guid.Parse("21111111-1234-1111-1111-111111111111"),
                "مطعم بغدادي للمشاوي",
                "أفضل المشاوي العراقية على الفحم",
                "بغداد، الكرادة داخل",
                "9647700000201",
                BaseLat + GetRandomOffset(),
                BaseLng + GetRandomOffset(),
                "https://images.unsplash.com/photo-1555939594-58d7cb561ad1",
                GenerateGrillsCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111112"),
                "Umm Ali",
                "baytaldolma",
                "9647700000002",
                Guid.Parse("21111111-1234-1111-1111-111111111112"),
                "بيت الدولمة",
                "ألذ دولمة عراقية بلمسة بيتية",
                "بغداد، المنصور",
                "9647700000202",
                BaseLat + GetRandomOffset(),
                BaseLng + GetRandomOffset(),
                "https://images.unsplash.com/photo-1549488344-1f9b8d2bd1f3",
                GenerateTraditionalCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111113"),
                "Mohammed Tannour",
                "tannourafidain",
                "9647700000003",
                Guid.Parse("21111111-1234-1111-1111-111111111113"),
                "تنور الرافدين",
                "مخبوزات ومعجنات طازجة من التنور",
                "بغداد، اليرموك",
                "9647700000203",
                BaseLat + GetRandomOffset(),
                BaseLng + GetRandomOffset(),
                "https://images.unsplash.com/photo-1509440159596-0249088772ff",
                GenerateBreakfastCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111114"),
                "Hassan Masgouf",
                "samakmasgouf",
                "9647700000004",
                Guid.Parse("21111111-1234-1111-1111-111111111114"),
                "سمك المسكوف الأصيل",
                "أطيب سمك مسكوف على ضفاف دجلة",
                "بغداد، أبو نواس",
                "9647700000204",
                BaseLat + GetRandomOffset(),
                BaseLng + GetRandomOffset(),
                "https://images.unsplash.com/photo-1580476262798-bddd9f4b7369",
                GenerateFishCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111115"),
                "Fatima Breakfast",
                "ftooratbaghdad",
                "9647700000005",
                Guid.Parse("21111111-1234-1111-1111-111111111115"),
                "فطورات بغداد",
                "فطور عراقي تقليدي - قيمر وكاهي",
                "بغداد، الكاظمية",
                "9647700000205",
                BaseLat + GetRandomOffset(),
                BaseLng + GetRandomOffset(),
                "https://images.unsplash.com/photo-1525648199074-ceee1cd15598",
                GenerateBreakfastCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111116"),
                "Ali Shawarma",
                "iraqishawarma",
                "9647700000006",
                Guid.Parse("21111111-1234-1111-1111-111111111116"),
                "شاورما دجلة",
                "شاورما لحم ودجاج بالبهارات العراقية",
                "بغداد، زيونة",
                "9647700000206",
                BaseLat + GetRandomOffset(),
                BaseLng + GetRandomOffset(),
                "https://images.unsplash.com/photo-1561651823-34feb02250e4",
                GenerateShawarmaCategories()
            )
        };
    }

    private static double GetRandomOffset()
    {
        return (Rng.NextDouble() - 0.5) * 0.05;
    }

    private static SeedCategoryDefinition[] GenerateGrillsCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "مشاوي", "أسياخ المشاوي على הפحم", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "كباب عراقي", "كباب لحم ضأن طازج", 12000m, "https://images.unsplash.com/photo-1544025162-811c21cb0d72", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "تكة لحم", "تكة لحم غنم بالبهارات", 14000m, "https://images.unsplash.com/photo-1555939594-58d7cb561ad1", 2),
                new SeedMenuItemDefinition(Guid.NewGuid(), "معلاك شوي", "معلاك عجل مشوي", 10000m, "https://images.unsplash.com/photo-1529193591184-b1d58069ecdd", 3)
            }),
            new SeedCategoryDefinition(Guid.NewGuid(), "مقبلات", "مقبلات باردة وحارة", 2, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "حمص بطحينة", "حمص مع زيت الزيتون", 3000m, "https://images.unsplash.com/photo-1571197119282-7c4b5bdb8e45", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "جاجيك", "لبن مع الخيار والنعناع", 2500m, "https://images.unsplash.com/photo-1628198751529-65bf6b14299b", 2)
            }),
            new SeedCategoryDefinition(Guid.NewGuid(), "مشروبات", "مشروبات باردة", 3, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "عيران", "لبن عيران مثلج", 1500m, "https://images.unsplash.com/photo-1556881286-fc6915169721", 1)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateTraditionalCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "أكلات عراقية", "أطباق عراقية أصيلة", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "دولمة عراقية", "دولمة مشكلة مع لحم الضلع", 15000m, "https://images.unsplash.com/photo-1549488344-1f9b8d2bd1f3", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "برياني عراقي", "رز برياني مع الدجاج واللوز", 12000m, "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8", 2),
                new SeedMenuItemDefinition(Guid.NewGuid(), "مرق بامية", "مرق بامية باللحم مع الرز", 11000m, "https://images.unsplash.com/photo-1512621776951-a57141f2eefd", 3),
                new SeedMenuItemDefinition(Guid.NewGuid(), "قوزي", "قوزي لحم غنم مع الرز", 18000m, "https://images.unsplash.com/photo-1603894584373-5ac82b2ae398", 4)
            }),
            new SeedCategoryDefinition(Guid.NewGuid(), "حلويات", "حلويات عربية", 2, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "زلابية", "زلابية مقرمشة بالقطر", 5000m, "https://images.unsplash.com/photo-1579372785934-8b63a23dbd4d", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "بقلاوة", "بقلاوة بالفستق", 7000m, "https://images.unsplash.com/photo-1559864222-d7cb5fc2e057", 2)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateFishCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "أسماك", "أسماك نهرية مشوية", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "سمك مسكوف", "سمك كارب مشوي على الحطب", 25000m, "https://images.unsplash.com/photo-1580476262798-bddd9f4b7369", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "سمك قلي", "سمك زبيدي مقلي", 18000m, "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2", 2)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateBreakfastCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "فطورات", "وجبات الصباح العراقية", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "قيمر وكاهي", "كاهي ساخن مع القيمر والعسل", 8000m, "https://images.unsplash.com/photo-1525648199074-ceee1cd15598", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "مخلمة", "مخلمة لحم مع البيض", 6000m, "https://images.unsplash.com/photo-1525385133512-2f3bdd039054", 2),
                new SeedMenuItemDefinition(Guid.NewGuid(), "فول مدمس", "فول مع الزيت والكمون", 4000m, "https://images.unsplash.com/photo-1596797038530-2c107229654b", 3)
            }),
            new SeedCategoryDefinition(Guid.NewGuid(), "مشروبات ساخنة", "شاي وقهوة", 2, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "شاي عراقي", "شاي مهيل", 1000m, "https://images.unsplash.com/photo-1563514930140-5a3d702dc6cf", 1)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateShawarmaCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "شاورما", "شاورما دجاج ولحم", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "لفة شاورما لحم", "لفة شاورما بخبز الصاج", 4000m, "https://images.unsplash.com/photo-1561651823-34feb02250e4", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "لفة شاورما دجاج", "لفة شاورما دجاج مع المايونيز", 3500m, "https://images.unsplash.com/photo-1528735602780-2552fd46c7af", 2),
                new SeedMenuItemDefinition(Guid.NewGuid(), "ماعون شاورما مكس", "شاورما مقطعة مع بطاطس ومقبلات", 8000m, "https://images.unsplash.com/photo-1594000216744-845112faef31", 3)
            }),
            new SeedCategoryDefinition(Guid.NewGuid(), "سندويشات", "سندويشات سريعة", 2, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "صاج فلافل", "فلافل مع الطحينة", 2000m, "https://images.unsplash.com/photo-1593504049359-74330189a345", 1)
            })
        };
    }
}

public sealed record SeedOwnerDefinition(
    Guid OwnerId,
    string OwnerName,
    string OwnerUsername,
    string OwnerPhoneNumber,
    Guid RestaurantId,
    string RestaurantName,
    string RestaurantDescription,
    string RestaurantAddress,
    string RestaurantPhoneNumber,
    double Latitude,
    double Longitude,
    string ImageUrl,
    IReadOnlyList<SeedCategoryDefinition> Categories);

public sealed record SeedCategoryDefinition(
    Guid CategoryId,
    string Name,
    string? Description,
    int DisplayOrder,
    IReadOnlyList<SeedMenuItemDefinition> Items);

public sealed record SeedMenuItemDefinition(
    Guid MenuItemId,
    string Name,
    string? Description,
    decimal Price,
    string ImageUrl,
    int DisplayOrder);
