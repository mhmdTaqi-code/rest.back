using SmartDiningSystem.Domain.Entities;
using SmartDiningSystem.Domain.Enums;

namespace SmartDiningSystem.Infrastructure.Data.Seed;

public static class IraqiDemoDataBuilder
{
    public static IReadOnlyList<SeedOwnerDefinition> GetMenuOwners()
    {
        return new[]
        {
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111111"),
                "أحمد البغدادي",
                "ahmed_baghdadi",
                "9647700000001",
                Guid.Parse("21111111-1234-1111-1111-111111111111"),
                "مطعم بغدادي للمشاوي",
                "أفضل المشاوي العراقية على الفحم الطازج",
                "بغداد، الكرادة، شارع 62",
                "9647700000001",
                33.2952,
                44.4173,
                "https://images.unsplash.com/photo-1555939594-58d7cb561ad1",
                GenerateGrillsCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111112"),
                "أم علي",
                "um_ali",
                "9647700000002",
                Guid.Parse("21111111-1234-1111-1111-111111111112"),
                "بيت الدولمة",
                "ألذ دولمة عراقية بلمسة بيتية أصيلة",
                "بغداد، المنصور، الرواد",
                "9647700000002",
                33.3156,
                44.3414,
                "https://images.unsplash.com/photo-1549488344-1f9b8d2bd1f3",
                GenerateTraditionalCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111113"),
                "محمد التنور",
                "mohammed_t",
                "9647700000003",
                Guid.Parse("21111111-1234-1111-1111-111111111113"),
                "تنور الرافدين",
                "مخبوزات ومعجنات طازجة من التنور مباشرة",
                "بغداد، اليرموك، الأربع شوارع",
                "9647700000003",
                33.3080,
                44.3450,
                "https://images.unsplash.com/photo-1509440159596-0249088772ff",
                GenerateBakeryCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111114"),
                "حسن مسكوف",
                "hassan_m",
                "9647700000004",
                Guid.Parse("21111111-1234-1111-1111-111111111114"),
                "سمك المسكوف الأصيل",
                "أطيب سمك مسكوف عراقي على ضفاف نهر دجلة",
                "بغداد، أبو نواس",
                "9647700000004",
                33.3129,
                44.4093,
                "https://images.unsplash.com/photo-1580476262798-bddd9f4b7369",
                GenerateFishCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111115"),
                "فاطمة",
                "fatima_b",
                "9647700000005",
                Guid.Parse("21111111-1234-1111-1111-111111111115"),
                "فطورات بغداد",
                "فطور عراقي تقليدي - قيمر وكاهي وحليب",
                "بغداد، الكاظمية، باب المراد",
                "9647700000005",
                33.3768,
                44.3421,
                "https://images.unsplash.com/photo-1525648199074-ceee1cd15598",
                GenerateBreakfastCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111116"),
                "علي شاورما",
                "ali_shawarma",
                "9647700000006",
                Guid.Parse("21111111-1234-1111-1111-111111111116"),
                "شاورما دجلة",
                "شاورما لحم ودجاج بالبهارات العراقية المميزة",
                "بغداد، زيونة، شارع الربيعي",
                "9647700000006",
                33.3115,
                44.4539,
                "https://images.unsplash.com/photo-1561651823-34feb02250e4",
                GenerateShawarmaCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111117"),
                "زيد بروستد",
                "zaid_broasted",
                "9647700000007",
                Guid.Parse("21111111-1234-1111-1111-111111111117"),
                "زينو بروستد",
                "أطيب دجاج بروستد مقرمش مع البطاطا والثومية",
                "بغداد، عرصات الهندية",
                "9647700000007",
                33.3001,
                44.3980,
                "https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58",
                GenerateBroastedCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111118"),
                "عمر صاج",
                "omar_saj",
                "9647700000008",
                Guid.Parse("21111111-1234-1111-1111-111111111118"),
                "صاج الريف",
                "أطباق سريعة، صاج، بيتزا ووجبات خفيفة لذيذة",
                "بغداد، المنصور، 14 رمضان",
                "9647700000008",
                33.3142,
                44.3378,
                "https://images.unsplash.com/photo-1593504049359-74330189a345",
                GenerateFastFoodCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111119"),
                "حسين حلويات",
                "hussein_sweets",
                "9647700000009",
                Guid.Parse("21111111-1234-1111-1111-111111111119"),
                "حلويات حجي حسين",
                "أشهى الحلويات العربية العريقة والبوظة الطبيعية",
                "بغداد، المنصور، تقاطع الرواد",
                "9647700000009",
                33.3175,
                44.3340,
                "https://images.unsplash.com/photo-1579372785934-8b63a23dbd4d",
                GenerateSweetsCategories()
            ),
            new SeedOwnerDefinition(
                Guid.Parse("11111111-1234-1111-1111-111111111120"),
                "علي العزائم",
                "ali_azaem",
                "9647700000010",
                Guid.Parse("21111111-1234-1111-1111-111111111120"),
                "مطعم العزائم",
                "أكلات عراقية تراثية قوزي ومندي ومشاوي",
                "بغداد، الوزيرية",
                "9647700000010",
                33.3250,
                44.3850,
                "https://images.unsplash.com/photo-1603894584373-5ac82b2ae398",
                GenerateGrillsCategories().Concat(GenerateTraditionalCategories()).ToArray()
            )
        };
    }

    private static SeedCategoryDefinition[] GenerateGrillsCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "مشاوي", "أسياخ المشاوي على الفحم", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "كباب لحم عراقي", "كباب لحم ضأن طازج", 12000m, "https://images.unsplash.com/photo-1544025162-811c21cb0d72", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "تكة عجل", "تكة لحم غنم بالبهارات", 14000m, "https://images.unsplash.com/photo-1555939594-58d7cb561ad1", 2),
                new SeedMenuItemDefinition(Guid.NewGuid(), "معلاك شوي", "معلاك مشوي على الفحم", 10000m, "https://images.unsplash.com/photo-1529193591184-b1d58069ecdd", 3)
            }),
            new SeedCategoryDefinition(Guid.NewGuid(), "مقبلات", "مقبلات باردة وحارة", 2, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "حمص بطحينة", "حمص مع زيت الزيتون وليمون", 3000m, "https://images.unsplash.com/photo-1571197119282-7c4b5bdb8e45", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "جاجيك", "لبن مع الخيار والنعناع", 2500m, "https://images.unsplash.com/photo-1628198751529-65bf6b14299b", 2)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateTraditionalCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "أكلات عراقية", "أطباق عراقية تقليدية غنية", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "دولمة", "دولمة مشكلة مع لحم الضلع وورق العنب", 15000m, "https://images.unsplash.com/photo-1549488344-1f9b8d2bd1f3", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "برياني دجاج", "رز برياني مع الدجاج والمكسرات", 12000m, "https://images.unsplash.com/photo-1563379091339-03b21ab4a4f8", 2),
                new SeedMenuItemDefinition(Guid.NewGuid(), "قوزي غنم", "قوزي لحم غنم مع الرز والشعرية", 18000m, "https://images.unsplash.com/photo-1603894584373-5ac82b2ae398", 3)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateFishCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "أسماك", "أسماك نهرية مشوية", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "مسكوف عراقي", "سمك كارب مشوي على الحطب ببطء", 25000m, "https://images.unsplash.com/photo-1580476262798-bddd9f4b7369", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "سمك زبيدي قلي", "سمك زبيدي بحري مقلي مع الخبز", 18000m, "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2", 2)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateBreakfastCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "فطورات", "وجبات الصباح العراقية", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "قيمر وكاهي", "كاهي عراقي ساخن مع قيمر السدة", 8000m, "https://images.unsplash.com/photo-1525648199074-ceee1cd15598", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "مخلمة لحم", "مخلمة لحم غنم مع البيض والطماطة", 6000m, "https://images.unsplash.com/photo-1525385133512-2f3bdd039054", 2)
            }),
            new SeedCategoryDefinition(Guid.NewGuid(), "مشروبات الصباح", "شاي وحليب", 2, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "شاي مهيل", "شاي عراقي مهيل اصلي", 1000m, "https://images.unsplash.com/photo-1563514930140-5a3d702dc6cf", 1)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateShawarmaCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "شاورما", "شاورما دجاج ولحم", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "لفة كص لحم", "لفة شاورما لحم بخبز الصاج", 4000m, "https://images.unsplash.com/photo-1561651823-34feb02250e4", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "لفة كص دجاج", "لفة شاورما دجاج مع المايونيز", 3500m, "https://images.unsplash.com/photo-1528735602780-2552fd46c7af", 2)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateBroastedCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "بروستد", "وجبات دجاج مقرمش", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "وجبة بروستد 4 قطع", "دجاج بروستد مقرمش مع البطاطا والثومية", 10000m, "https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "وجبة بروستد 8 قطع", "دجاج بروستد عائلي سبايسي", 18000m, "https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58", 2)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateFastFoodCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "صاج وساندويشات", "وجبات سريعة ومقرمشة", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "صاج دجاج ماريا", "صاج دجاج مع صلصات خاصة وجبن", 5000m, "https://images.unsplash.com/photo-1593504049359-74330189a345", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "بيتزا بيبروني", "بيتزا إيطالية بلمسة عراقية", 10000m, "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3", 2)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateSweetsCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "حلويات", "حلويات شرقية وموطا", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "بقلاوة بالفستق", "بقلاوة عراقية دهن حر", 12000m, "https://images.unsplash.com/photo-1559864222-d7cb5fc2e057", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "زنود الست", "زنود الست محشية بالقشطة", 8000m, "https://images.unsplash.com/photo-1579372785934-8b63a23dbd4d", 2),
                new SeedMenuItemDefinition(Guid.NewGuid(), "موطا عربية", "موطا بالفسقتق الحلبي", 3000m, "https://images.unsplash.com/photo-1563805042-7684c8a9e9cb", 3)
            })
        };
    }

    private static SeedCategoryDefinition[] GenerateBakeryCategories()
    {
        return new[]
        {
            new SeedCategoryDefinition(Guid.NewGuid(), "معجنات", "معجنات وخبز حار", 1, new[]
            {
                new SeedMenuItemDefinition(Guid.NewGuid(), "لحم بعجين", "لحم بعجين حار ومقرمش", 1500m, "https://images.unsplash.com/photo-1509440159596-0249088772ff", 1),
                new SeedMenuItemDefinition(Guid.NewGuid(), "صمون عراقي", "صمون حار ومقرمش من التنور", 500m, "https://images.unsplash.com/photo-1600189025983-4903348126e8", 2)
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
