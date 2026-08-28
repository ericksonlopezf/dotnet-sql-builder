// Copyright © Erickson Lopez. MIT License.
using Xunit;

namespace EricksonLopez.SqlBuilder.MySql.IntegrationTests;

[CollectionDefinition("MySqlCollection")]
public class MySqlCollection : ICollectionFixture<MySqlFixture>
{
}
