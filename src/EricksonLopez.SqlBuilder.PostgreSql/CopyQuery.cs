// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.PostgreSql
{
    /// <summary>
    /// Represents a PostgreSQL COPY FROM STDIN statement for high-performance bulk inserts.
    /// </summary>
    public sealed record CopyQuery<T> : IAstQuery where T : class, new()
    {
        /// <inheritdoc />
        public ImmutableList<ISqlNode> Nodes { get; init; } = ImmutableList<ISqlNode>.Empty;
        IReadOnlyList<ISqlNode> IAstQuery.Nodes => Nodes;
        /// <inheritdoc />
        public string? Tag => null;

        /// <summary>
        /// Initializes a new instance of <see cref="CopyQuery{T}"/> using all mapped columns of the entity.
        /// </summary>
        public CopyQuery()
        {
            var tableName = SqlEntityCache<T>.TableName;
            var properties = SqlEntityCache<T>.ColumnNames;

            Nodes = Nodes.Add(new CopyNode(tableName, properties, "STDIN", "BINARY"));
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CopyQuery{T}"/> with specific target columns.
        /// </summary>
        /// <param name="columns">The specific columns to copy into.</param>
        public CopyQuery(IEnumerable<string> columns)
        {
            var tableName = SqlEntityCache<T>.TableName;
            Nodes = Nodes.Add(new CopyNode(tableName, columns.ToArray(), "STDIN", "BINARY"));
        }

        /// <inheritdoc />
        [RequiresDynamicCode("PostgreSQL COPY query compilation uses dynamic code generation. Use Sql.Raw() for strict NativeAOT paths.")]
        [RequiresUnreferencedCode("PostgreSQL COPY query compilation accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
        public SqlResult Build(ISqlCompiler compiler) => compiler.Compile(this);

        [ExcludeFromCodeCoverage]
        private CopyQuery<T> AddNode(ISqlNode node)
        {
            return this with { Nodes = Nodes.Add(node) };
        }
    }
}
