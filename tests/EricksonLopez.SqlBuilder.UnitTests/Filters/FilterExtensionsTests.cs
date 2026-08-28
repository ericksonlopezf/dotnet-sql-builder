// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Filters;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Filters;

public class FilterExtensionsTests
{

    private class MockFilter : ISqlFilter<DummyEntity>
    {
        public bool Applied { get; private set; }
        
        public SelectQuery<DummyEntity> Apply(SelectQuery<DummyEntity> query)
        {
            Applied = true;
            return query;
        }
    }

    [Fact]
    public void ApplyFilter_NullFilter_ReturnsOriginalQuery()
    {
        var query = new SelectQuery<DummyEntity>();
        var result = query.ApplyFilter(null);
        result.Should().BeSameAs(query);
    }

    [Fact]
    public void ApplyFilter_ValidFilter_CallsApply()
    {
        var query = new SelectQuery<DummyEntity>();
        var filter = new MockFilter();
        var result = query.ApplyFilter(filter);
        
        filter.Applied.Should().BeTrue();
        result.Should().BeSameAs(query);
    }

    [Fact]
    public void ApplyFilters_NullArray_ReturnsOriginalQuery()
    {
        var query = new SelectQuery<DummyEntity>();
        var result = query.ApplyFilters(null!);
        result.Should().BeSameAs(query);
    }
    
    [Fact]
    public void ApplyFilters_EmptyArray_ReturnsOriginalQuery()
    {
        var query = new SelectQuery<DummyEntity>();
        var result = query.ApplyFilters(System.Array.Empty<ISqlFilter<DummyEntity>>());
        result.Should().BeSameAs(query);
    }

    [Fact]
    public void ApplyFilters_ValidFilters_AppliesAll()
    {
        var query = new SelectQuery<DummyEntity>();
        var filter1 = new MockFilter();
        var filter2 = new MockFilter();
        
        var result = query.ApplyFilters(filter1, filter2);
        
        filter1.Applied.Should().BeTrue();
        filter2.Applied.Should().BeTrue();
        result.Should().BeSameAs(query);
    }
    
    [Fact]
    public void ApplyFilters_WithNullElements_SkipsNulls()
    {
        var query = new SelectQuery<DummyEntity>();
        var filter1 = new MockFilter();
        
        var result = query.ApplyFilters(filter1, null!);
        
        filter1.Applied.Should().BeTrue();
        result.Should().BeSameAs(query);
    }
}




