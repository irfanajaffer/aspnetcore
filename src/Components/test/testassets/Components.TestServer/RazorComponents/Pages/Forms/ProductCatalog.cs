// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Components.TestServer.RazorComponents.Pages.Forms;

/// <summary>
/// A minimal in-memory product catalog used by the GET-form binding sample page.
/// </summary>
public class ProductCatalog
{
    private static readonly IReadOnlyList<Product> _all = new List<Product>
    {
        // A product with "Audio" in its name. The E2E test asserts the literal
        // string "Audio" appears in the rendered body, so at least one product
        // here must contain that substring.
        new() { Id = 1, Name = "Audio mixer", Category = "Electronics", Price = 199.99m, InStock = true,  Tags = new[] { "audio", "peripherals" } },
        new() { Id = 2, Name = "Standing desk",       Category = "Furniture",   Price = 499m,   InStock = true,  Tags = new[] { "office" } },
        new() { Id = 3, Name = "Noise-cancelling headphones", Category = "Electronics", Price = 249m, InStock = false, Tags = new[] { "audio", "peripherals" } },
        new() { Id = 4, Name = "Ergonomic chair",    Category = "Furniture",   Price = 350m,   InStock = true,  Tags = new[] { "office" } },
        new() { Id = 5, Name = "Webcam",             Category = "Electronics", Price = 49.99m, InStock = true,  Tags = new[] { "peripherals" } },
        new() { Id = 6, Name = "Coffee mug",         Category = "Kitchen",     Price = 12.50m, InStock = true,  Tags = new[] { "kitchen", "office" } },
    };

    public IReadOnlyList<Product> All => _all;

    public IReadOnlyList<Product> Apply(ProductFilter filter)
    {
        IEnumerable<Product> q = _all;

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var needle = filter.Query.Trim();
            q = q.Where(p => p.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            q = q.Where(p => string.Equals(p.Category, filter.Category, StringComparison.OrdinalIgnoreCase));
        }

        q = q.Where(p => p.Price >= filter.Price.Min && p.Price <= filter.Price.Max);

        if (filter.InStockOnly)
        {
            q = q.Where(p => p.InStock);
        }

        if (filter.Tags is { Count: > 0 })
        {
            q = q.Where(p => p.Tags.Any(t => filter.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)));
        }

        q = filter.SortBy switch
        {
            ProductSortOrder.Name  => q.OrderBy(p => p.Name),
            ProductSortOrder.Price => q.OrderBy(p => p.Price),
            _ => q,
        };

        return q.ToList();
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool InStock { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}
