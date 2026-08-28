// Copyright © Erickson Lopez. MIT License.
using Xunit;

namespace EricksonLopez.SqlBuilder.MariaDb.IntegrationTests;

[CollectionDefinition("MariaDbCollection")]
public class MariaDbCollection : ICollectionFixture<MariaDbFixture>
{
}
