// Copyright © Erickson Lopez. MIT License.
using Xunit;

namespace EricksonLopez.SqlBuilder.SqlServer.IntegrationTests;

[CollectionDefinition("SqlServerCollection")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
}
