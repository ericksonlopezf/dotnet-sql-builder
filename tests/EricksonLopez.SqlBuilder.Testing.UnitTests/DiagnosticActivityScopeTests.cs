// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing.UnitTests;

public class DiagnosticActivityScopeTests
{
    [Fact]
    public void Start_DefaultSourceName_CapturesActivities()
    {
        using (var scope = DiagnosticActivityScope.Start())
        {
            var source = new ActivitySource("EricksonLopez.SqlBuilder");
            using (var activity = source.StartActivity("ExecuteQuery"))
            {
                activity?.SetTag("db.system", "postgresql");
            }

            Assert.NotEmpty(scope.Activities);
            Assert.Contains(scope.Activities, a => a.OperationName == "ExecuteQuery");
        }
    }

    [Fact]
    public void Start_CustomSourceName_FiltersActivities()
    {
        using (var scope = DiagnosticActivityScope.Start("CustomTestScope"))
        {
            var matchingSource = new ActivitySource("CustomTestScope");
            using (var activity = matchingSource.StartActivity("CustomOp"))
            {
                activity?.SetTag("test", "true");
            }

            var otherSource = new ActivitySource("OtherScope");
            using (var otherActivity = otherSource.StartActivity("IgnoredOp"))
            {
            }

            Assert.Single(scope.Activities);
            Assert.Contains(scope.Activities, a => a.OperationName == "CustomOp");
            Assert.DoesNotContain(scope.Activities, a => a.OperationName == "IgnoredOp");
        }
    }

    [Fact]
    public void Dispose_UnregistersListener()
    {
        var scope = DiagnosticActivityScope.Start("DisposalTestScope");
        scope.Dispose();

        var source = new ActivitySource("DisposalTestScope");
        using (var activity = source.StartActivity("AfterDisposeOp"))
        {
        }

        Assert.Empty(scope.Activities);
    }
}
