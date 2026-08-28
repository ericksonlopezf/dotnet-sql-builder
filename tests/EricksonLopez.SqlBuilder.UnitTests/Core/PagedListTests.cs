// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class PagedListTests
{
    [Fact]
    public void Create_ValidParameters_CreatesPagedList()
    {
        var items = new[] { 1, 2, 3 };
        var list = PagedList<int>.WithCount(items, PaginationParameters.Create(1, 3), 10);
        
        list.TotalCount.Should().Be(10);
        list.TotalPages.Should().Be(4); // 10 / 3 = 3.33 -> 4
        list.Page.Should().Be(1);
        list.PageSize.Should().Be(3);
        list.HasNextPage.Should().BeTrue();
        list.HasPreviousPage.Should().BeFalse();
        list.Should().BeEquivalentTo(items);
    }

    [Fact]
    public void Empty_ReturnsEmptyPagedList()
    {
        var list = PagedList<int>.Empty(PaginationParameters.Create(2, 5));
        
        list.TotalCount.Should().Be(0);
        list.TotalPages.Should().Be(0);
        list.Page.Should().Be(2);
        list.PageSize.Should().Be(5);
        list.HasNextPage.Should().BeFalse();
        list.HasPreviousPage.Should().BeTrue();
        list.Should().BeEmpty();
    }
}


