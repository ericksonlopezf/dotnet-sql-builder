// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes
{
    /// <summary>
    /// Represents a specialized node that uses window functions to perform offset-based pagination.
    /// </summary>
    /// <param name="PageNumber">The 1-based page number.</param>
    /// <param name="PageSize">The maximum number of rows per page.</param>
    /// <param name="OrderByColumn">The column used to determine the sort order for pagination.</param>
    /// <param name="Descending">If <see langword="true"/>, sorts in descending order; otherwise ascending.</param>
    public sealed record WindowPageNode(int PageNumber, int PageSize, string OrderByColumn, bool Descending) : ISqlNode
    {
        /// <inheritdoc />
        public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
    }
}








