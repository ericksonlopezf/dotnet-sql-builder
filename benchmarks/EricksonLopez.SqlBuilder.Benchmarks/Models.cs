// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Benchmarks
{
    /// <summary>
    /// Represents a sample Customer entity for benchmarks.
    /// </summary>
    [SqlEntity("Customer")]
    public partial class Customer
    {
        /// <summary>Gets or sets the ID.</summary>
        public int Id { get; set; }
        /// <summary>Gets or sets the Name.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Gets or sets the Email.</summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>Gets or sets the CreatedAt timestamp.</summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>Gets or sets a value indicating whether the customer is active.</summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Represents a sample Order entity for benchmarks.
    /// </summary>
    [SqlEntity("Order")]
    public partial class Order
    {
        /// <summary>Gets or sets the ID.</summary>
        public int Id { get; set; }
        /// <summary>Gets or sets the Customer ID.</summary>
        public int CustomerId { get; set; }
        /// <summary>Gets or sets the total amount.</summary>
        public decimal TotalAmount { get; set; }
        /// <summary>Gets or sets the order date.</summary>
        public DateTime OrderDate { get; set; }
        /// <summary>Gets or sets the status.</summary>
        public string Status { get; set; } = string.Empty;
    }
}

