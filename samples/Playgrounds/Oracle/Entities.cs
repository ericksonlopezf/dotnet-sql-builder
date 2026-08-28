// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Playgrounds.Oracle;

[SqlEntity("customers")]
public partial class Customer
{
    public long    Id        { get; set; }
    public string  Name      { get; set; } = "";
    public string  Email     { get; set; } = "";
    public string? Phone     { get; set; }
    /// <remarks>Oracle stores BIT as NUMBER(1,0) — mapped to int</remarks>
    public int     IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
}

[SqlEntity("products")]
public partial class Product
{
    public long    Id         { get; set; }
    public long    CategoryId { get; set; }
    public string  Name       { get; set; } = "";
    public string  Sku        { get; set; } = "";
    public decimal Price      { get; set; }
    public int     Stock      { get; set; }
    public int     IsActive   { get; set; }
}

[SqlEntity("orders")]
public partial class Order
{
    public long     Id          { get; set; }
    public long     CustomerId  { get; set; }
    public string   Status      { get; set; } = "";
    public decimal  TotalAmount { get; set; }
    public string   Currency    { get; set; } = "";
    /// <remarks>Oracle NUMBER(1,0) — mapped to int</remarks>
    public int      IsDeleted   { get; set; }
    public DateTime CreatedAt   { get; set; }
}

[SqlEntity("order_items")]
public partial class OrderItem
{
    public long    Id         { get; set; }
    public long    OrderId    { get; set; }
    public long    ProductId  { get; set; }
    public int     Quantity   { get; set; }
    public decimal UnitPrice  { get; set; }
    public decimal TotalPrice { get; set; }
}

