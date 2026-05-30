using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure.Persistence;

public static class CatalogDbSeeder
{
    public static async Task SeedCatalogDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        await db.Database.MigrateAsync();

        if (await db.Products.AnyAsync())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var coreline = new Brand
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = "Coreline",
            Slug = "coreline",
            CreatedAt = now,
            UpdatedAt = now
        };

        var papertrail = new Brand
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Name = "Papertrail",
            Slug = "papertrail",
            CreatedAt = now,
            UpdatedAt = now
        };

        var electronics = new Category
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Name = "Electronics",
            Slug = "electronics",
            IsActive = true,
            SortOrder = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        var homeOffice = new Category
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
            Name = "Home Office",
            Slug = "home-office",
            IsActive = true,
            SortOrder = 2,
            CreatedAt = now,
            UpdatedAt = now
        };

        var books = new Category
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
            Name = "Books",
            Slug = "books",
            IsActive = true,
            SortOrder = 3,
            CreatedAt = now,
            UpdatedAt = now
        };

        var accessories = new Category
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000004"),
            Name = "Accessories",
            Slug = "accessories",
            IsActive = true,
            SortOrder = 4,
            CreatedAt = now,
            UpdatedAt = now
        };

        var headphones = CreateProduct(
            id: "30000000-0000-0000-0000-000000000001",
            brand: coreline,
            name: "Wireless Headphones",
            slug: "wireless-headphones",
            description: "Compact wireless headphones for everyday work, calls and travel.",
            now: now);

        AddVariant(headphones, "40000000-0000-0000-0000-000000000001", "WH-BLK", "Black", 12900, "USD", now);
        AddVariant(headphones, "40000000-0000-0000-0000-000000000002", "WH-WHT", "White", 12900, "USD", now);
        AddImage(headphones, "50000000-0000-0000-0000-000000000001", "/images/catalog/wireless-headphones.png", "Wireless headphones", 1, true, now);
        AddCategory(headphones, electronics);
        AddCategory(headphones, accessories);

        var keyboard = CreateProduct(
            id: "30000000-0000-0000-0000-000000000002",
            brand: coreline,
            name: "Mechanical Keyboard",
            slug: "mechanical-keyboard",
            description: "Low-profile mechanical keyboard for focused desk work.",
            now: now);

        AddVariant(keyboard, "40000000-0000-0000-0000-000000000003", "KEY-US-BLK", "US Layout / Black", 9900, "USD", now);
        AddVariant(keyboard, "40000000-0000-0000-0000-000000000004", "KEY-EU-GRY", "EU Layout / Gray", 9900, "USD", now);
        AddImage(keyboard, "50000000-0000-0000-0000-000000000002", "/images/catalog/mechanical-keyboard.png", "Mechanical keyboard", 1, true, now);
        AddCategory(keyboard, electronics);
        AddCategory(keyboard, homeOffice);

        var dockingStation = CreateProduct(
            id: "30000000-0000-0000-0000-000000000003",
            brand: coreline,
            name: "USB-C Docking Station",
            slug: "usb-c-docking-station",
            description: "Multi-port USB-C dock for laptops, monitors and desk setups.",
            now: now);

        AddVariant(dockingStation, "40000000-0000-0000-0000-000000000005", "DOCK-8IN1", "8-in-1", 7900, "USD", now);
        AddImage(dockingStation, "50000000-0000-0000-0000-000000000003", "/images/catalog/usb-c-docking-station.png", "USB-C docking station", 1, true, now);
        AddCategory(dockingStation, electronics);
        AddCategory(dockingStation, homeOffice);

        var monitor = CreateProduct(
            id: "30000000-0000-0000-0000-000000000004",
            brand: coreline,
            name: "Portable Monitor",
            slug: "portable-monitor",
            description: "Lightweight external display for mobile productivity.",
            now: now);

        AddVariant(monitor, "40000000-0000-0000-0000-000000000006", "PMON-156-FHD", "15.6 inch / FHD", 18900, "USD", now);
        AddVariant(monitor, "40000000-0000-0000-0000-000000000007", "PMON-156-4K", "15.6 inch / 4K", 32900, "USD", now);
        AddImage(monitor, "50000000-0000-0000-0000-000000000004", "/images/catalog/portable-monitor.png", "Portable monitor", 1, true, now);
        AddCategory(monitor, electronics);
        AddCategory(monitor, homeOffice);

        var backpack = CreateProduct(
            id: "30000000-0000-0000-0000-000000000005",
            brand: coreline,
            name: "Technical Backpack",
            slug: "technical-backpack",
            description: "Everyday backpack with laptop compartment and organizer pockets.",
            now: now);

        AddVariant(backpack, "40000000-0000-0000-0000-000000000008", "BAG-20L-BLK", "20L / Black", 8900, "USD", now);
        AddVariant(backpack, "40000000-0000-0000-0000-000000000009", "BAG-25L-GRY", "25L / Gray", 10900, "USD", now);
        AddImage(backpack, "50000000-0000-0000-0000-000000000005", "/images/catalog/technical-backpack.png", "Technical backpack", 1, true, now);
        AddCategory(backpack, accessories);

        var notebook = CreateProduct(
            id: "30000000-0000-0000-0000-000000000006",
            brand: papertrail,
            name: "Paperback Notebook",
            slug: "paperback-notebook",
            description: "Minimal notebook for planning, notes and daily work.",
            now: now);

        AddVariant(notebook, "40000000-0000-0000-0000-000000000010", "NOTE-A5", "A5", 1200, "USD", now);
        AddVariant(notebook, "40000000-0000-0000-0000-000000000011", "NOTE-A4", "A4", 1800, "USD", now);
        AddImage(notebook, "50000000-0000-0000-0000-000000000006", "/images/catalog/paperback-notebook.png", "Paperback notebook", 1, true, now);
        AddCategory(notebook, books);
        AddCategory(notebook, homeOffice);

        await db.AddRangeAsync(
            coreline,
            papertrail,
            electronics,
            homeOffice,
            books,
            accessories,
            headphones,
            keyboard,
            dockingStation,
            monitor,
            backpack,
            notebook);

        await db.SaveChangesAsync();
    }

    private static Product CreateProduct(
        string id,
        Brand brand,
        string name,
        string slug,
        string description,
        DateTimeOffset now)
    {
        return new Product
        {
            Id = Guid.Parse(id),
            BrandId = brand.Id,
            Brand = brand,
            Name = name,
            Slug = slug,
            Description = description,
            Status = ProductStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static void AddVariant(
        Product product,
        string id,
        string sku,
        string name,
        long priceAmountMinor,
        string currency,
        DateTimeOffset now)
    {
        product.Variants.Add(new ProductVariant
        {
            Id = Guid.Parse(id),
            ProductId = product.Id,
            Product = product,
            Sku = sku,
            Name = name,
            PriceAmountMinor = priceAmountMinor,
            Currency = currency,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    private static void AddImage(
        Product product,
        string id,
        string url,
        string altText,
        int sortOrder,
        bool isPrimary,
        DateTimeOffset now)
    {
        product.Images.Add(new ProductImage
        {
            Id = Guid.Parse(id),
            ProductId = product.Id,
            Product = product,
            Url = url,
            AltText = altText,
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            CreatedAt = now
        });
    }

    private static void AddCategory(Product product, Category category)
    {
        product.ProductCategories.Add(new ProductCategory
        {
            ProductId = product.Id,
            Product = product,
            CategoryId = category.Id,
            Category = category
        });
    }
}