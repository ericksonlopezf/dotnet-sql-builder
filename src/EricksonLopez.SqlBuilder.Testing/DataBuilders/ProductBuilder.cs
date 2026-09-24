// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.DataBuilders;

/// <summary>
/// Provides a fluent test data builder for creating <see cref="Product"/> instances.
/// </summary>
public sealed class ProductBuilder
{
    private int _id = 1;
    private int _categoryId = 1;
    private string _name = "Laptop";
    private string _sku = "SKU-00001";
    private string? _description = "High-performance laptop";
    private decimal _price = 999.99m;
    private decimal _costPrice = 699.99m;
    private int _stock = 50;
    private int _minStock = 5;
    private bool _isActive = true;
    private DateTime _createdAt = new(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Creates a new instance of the <see cref="ProductBuilder"/> class.
    /// </summary>
    /// <returns>A new <see cref="ProductBuilder"/> instance.</returns>
    public static ProductBuilder Create() => new();

    /// <summary>
    /// Sets the identifier for the product being built.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public ProductBuilder WithId(int id) { _id = id; return this; }

    /// <summary>
    /// Sets the category identifier for the product being built.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public ProductBuilder WithCategoryId(int categoryId) { _categoryId = categoryId; return this; }

    /// <summary>
    /// Sets the name for the product being built.
    /// </summary>
    /// <param name="name">The product name.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public ProductBuilder WithName(string name) { _name = name; return this; }

    /// <summary>
    /// Sets the SKU code for the product being built.
    /// </summary>
    /// <param name="sku">The stock keeping unit identifier.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public ProductBuilder WithSku(string sku) { _sku = sku; return this; }

    /// <summary>
    /// Sets the selling price for the product being built.
    /// </summary>
    /// <param name="price">The retail selling price.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public ProductBuilder WithPrice(decimal price) { _price = price; return this; }

    /// <summary>
    /// Sets the cost price for the product being built.
    /// </summary>
    /// <param name="costPrice">The wholesale or cost price.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public ProductBuilder WithCostPrice(decimal costPrice) { _costPrice = costPrice; return this; }

    /// <summary>
    /// Sets the available stock count for the product being built.
    /// </summary>
    /// <param name="stock">The quantity available in stock.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public ProductBuilder WithStock(int stock) { _stock = stock; return this; }

    /// <summary>
    /// Sets a value indicating whether the product being built is active.
    /// </summary>
    /// <param name="isActive"><see langword="true"/> if the product is active; otherwise, <see langword="false"/>.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public ProductBuilder WithActive(bool isActive) { _isActive = isActive; return this; }

    /// <summary>
    /// Builds and returns the configured <see cref="Product"/> instance.
    /// </summary>
    /// <returns>A new <see cref="Product"/> instance initialized with configured properties.</returns>
    public Product Build() => new()
    {
        Id = _id,
        CategoryId = _categoryId,
        Name = _name,
        Sku = _sku,
        Description = _description,
        Price = _price,
        CostPrice = _costPrice,
        Stock = _stock,
        MinStock = _minStock,
        IsActive = _isActive,
        CreatedAt = _createdAt
    };
}
