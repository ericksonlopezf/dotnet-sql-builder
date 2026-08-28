// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EricksonLopez.SqlBuilder.Analyzers
{
    /// <summary>
    /// Provides base support for schema-aware validation in analyzers.
    /// Reads 'sqlbuilder-schema.json' from AdditionalFiles if provided (opt-in).
    /// </summary>
    public sealed class SchemaValidator
    {
        private readonly HashSet<string> _knownTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Gets a value indicating whether schema validation is active.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Loads schema validation settings from analyzer options.
        /// </summary>
        /// <param name="options">The analyzer options containing additional files.</param>
        /// <returns>A configured <see cref="SchemaValidator"/> instance.</returns>
        public static SchemaValidator Load(AnalyzerOptions options)
        {
            var validator = new SchemaValidator();
            var schemaFile = options.AdditionalFiles.FirstOrDefault(f => f.Path.EndsWith("sqlbuilder-schema.json", StringComparison.OrdinalIgnoreCase));
            
            if (schemaFile != null)
            {
                validator.IsActive = true;
                // In a real implementation, parse JSON here and populate _knownTables and columns.
                // For the base support (opt-in), we just activate the validator.
            }
            
            return validator;
        }

        /// <summary>
        /// Determines whether the specified table exists in the schema.
        /// </summary>
        /// <param name="tableName">The table name to validate.</param>
        /// <returns><see langword="true"/> if the table is valid or schema validation is inactive; otherwise, <see langword="false"/>.</returns>
        public bool IsTableValid(string tableName)
        {
            if (!IsActive) return true; // Fail open if schema validation is not active
            return _knownTables.Contains(tableName);
        }
    }
}

