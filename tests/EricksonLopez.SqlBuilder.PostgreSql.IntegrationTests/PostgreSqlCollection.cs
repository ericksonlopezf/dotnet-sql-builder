// Copyright © Erickson Lopez. MIT License.
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.IntegrationTests;

[CollectionDefinition("PostgreSqlCollection")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
}
