// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Components.TestServer.RazorComponents.Pages.Forms;

/// <summary>
/// The product filter model used by the GET-form binding sample page.
/// The shape and <see cref="ToString"/> format are deliberately matched by the
/// E2E test in <c>FormWithParentBindingContextTest.CanBindParameterFromGetFormQueryString</c>.
/// </summary>
public class ProductFilter
{
    /// <summary>
    /// Free-text search applied to product names.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Optional category filter.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Inclusive price range.
    /// </summary>
    public PriceRange Price { get; set; } = new();

    /// <summary>
    /// How to order the results.
    /// </summary>
    public ProductSortOrder SortBy { get; set; } = ProductSortOrder.Name;

    /// <summary>
    /// Whether results should be limited to items that are in stock.
    /// </summary>
    public bool InStockOnly { get; set; }

    /// <summary>
    /// Tags to filter on.
    /// </summary>
    public IList<string> Tags { get; set; } = new List<string>();

    // The format here is intentionally distinct from the default Object.ToString.
    // The E2E test asserts the exact rendered string, so changing this format
    // requires updating the test in lockstep.
    public override string ToString() =>
        $"Filter[Query={Query}, Category={Category ?? string.Empty}, " +
        $"Price=[Min={Price.Min}, Max={Price.Max}], SortBy={SortBy}, " +
        $"InStockOnly={InStockOnly}, Tags=[{string.Join(",", Tags)}]]";
}

public class PriceRange
{
    public decimal Min { get; set; } = 0m;

    public decimal Max { get; set; } = 0m;
}

public enum ProductSortOrder
{
    Name = 0,
    Price = 1,
}
