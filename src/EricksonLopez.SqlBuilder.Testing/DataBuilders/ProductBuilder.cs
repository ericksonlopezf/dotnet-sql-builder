// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Testing.Domain;

namespace EricksonLopez.SqlBuilder.Testing.DataBuilders;

/// <summary>
/// Fluent test data builder for <see cref="Product"/>.
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

    public static ProductBuilder Create() => new();

    public ProductBuilder WithId(int id) { _id = id; return this; }
    public ProductBuilder WithCategoryId(int categoryId) { _categoryId = categoryId; return this; }
    public ProductBuilder WithName(string name) { _name = name; return this; }
    public ProductBuilder WithSku(string sku) { _sku = sku; return this; }
    public ProductBuilder WithPrice(decimal price) { _price = price; return this; }
    public ProductBuilder WithCostPrice(decimal costPrice) { _costPrice = costPrice; return this; }
    public ProductBuilder WithStock(int stock) { _stock = stock; return this; }
    public ProductBuilder WithActive(bool isActive) { _isActive = isActive; return this; }

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
