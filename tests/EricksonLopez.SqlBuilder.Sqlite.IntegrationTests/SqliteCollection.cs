// Copyright © Erickson Lopez. MIT License.
using Xunit;

namespace EricksonLopez.SqlBuilder.Sqlite.IntegrationTests;

[CollectionDefinition("SqliteCollection")]
public class SqliteCollection : ICollectionFixture<SqliteFixture>
{
}
