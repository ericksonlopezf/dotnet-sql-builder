// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Playgrounds.MySql;

[SqlEntity("customers")]
public partial class Customer
{
    public int      Id        { get; set; }
    public string   Name      { get; set; } = "";
    public string   Email     { get; set; } = "";
    public string?  Phone     { get; set; }
    public bool     IsActive  { get; set; }
    public DateTime CreatedAt { get; set; }
}

[SqlEntity("products")]
public partial class Product
{
    public int     Id         { get; set; }
    public int     CategoryId { get; set; }
    public string  Name       { get; set; } = "";
    public string  Sku        { get; set; } = "";
    public decimal Price      { get; set; }
    public int     Stock      { get; set; }
    public bool    IsActive   { get; set; }
}

[SqlEntity("orders")]
public partial class Order
{
    public int      Id          { get; set; }
    public int      CustomerId  { get; set; }
    public string   Status      { get; set; } = "";
    public decimal  TotalAmount { get; set; }
    public bool     IsDeleted   { get; set; }
    public DateTime CreatedAt   { get; set; }
}

