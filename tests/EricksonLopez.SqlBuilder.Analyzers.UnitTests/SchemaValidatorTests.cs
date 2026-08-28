// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace EricksonLopez.SqlBuilder.Analyzers.Tests
{
    public class SchemaValidatorTests
    {
        private class TestAdditionalText : AdditionalText
        {
            private readonly string _path;
            private readonly string _text;

            public TestAdditionalText(string path, string text)
            {
                _path = path;
                _text = text;
            }

            public override string Path => _path;

            public override SourceText GetText(CancellationToken cancellationToken = default)
            {
                return SourceText.From(_text);
            }
        }

        [Fact]
        public void SchemaValidator_WithoutSchemaFile_IsNotActive_AndAcceptsAnyTable()
        {
            var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
            var validator = SchemaValidator.Load(options);

            Assert.False(validator.IsActive);
            Assert.True(validator.IsTableValid("Users"));
            Assert.True(validator.IsTableValid("NonExistentTable"));
        }

        [Fact]
        public void SchemaValidator_WithSchemaFile_IsActive()
        {
            var file = new TestAdditionalText("C:\\Project\\sqlbuilder-schema.json", "{}");
            var options = new AnalyzerOptions(ImmutableArray.Create<AdditionalText>(file));
            var validator = SchemaValidator.Load(options);

            Assert.True(validator.IsActive);
            Assert.False(validator.IsTableValid("RandomTable"));
        }

        [Fact]
        public void SchemaValidator_WithOtherFile_IsNotActive()
        {
            var file = new TestAdditionalText("C:\\Project\\other-config.json", "{}");
            var options = new AnalyzerOptions(ImmutableArray.Create<AdditionalText>(file));
            var validator = SchemaValidator.Load(options);

            Assert.False(validator.IsActive);
            Assert.True(validator.IsTableValid("RandomTable"));
        }
    }
}

