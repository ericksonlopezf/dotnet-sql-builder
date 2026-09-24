// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing.Domain;

/// <summary>Represents a product category supporting hierarchical parent-child structure.</summary>
[SqlEntity("categories")]
public partial class Category
{
    /// <summary>Gets or sets the primary key identifier of the category.</summary>
    [DatabaseGenerated]
    public int Id { get; set; }

    /// <summary>Gets or sets the display name of the category.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL-friendly unique slug for the category.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional parent category identifier for hierarchical categorization.</summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>Gets or sets a value indicating whether the category is currently active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the sequential display ordering index for the category.</summary>
    public int SortOrder { get; set; } = 0;
}

