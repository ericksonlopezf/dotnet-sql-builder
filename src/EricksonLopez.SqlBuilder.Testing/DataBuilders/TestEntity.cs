// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.SqlBuilder.Testing.DataBuilders;

/// <summary>
/// Represents a simple test entity designed for generic query tests.
/// </summary>
[EricksonLopez.SqlBuilder.Annotations.SqlEntity("testentitys")]
public partial class TestEntity
{
    /// <summary>
    /// Gets or sets the primary key identifier of the test entity.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the test entity.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets a value indicating whether the test entity is active.
    /// </summary>
    public bool IsActive { get; set; }
}
