// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.SqlBuilder;

#pragma warning disable CA1050
#pragma warning disable CA1303

namespace EricksonLopez.SqlBuilder.AotSmokeTest;

internal static class Program
{
    private static int _passedTests;

    private static void Assert([DoesNotReturnIf(false)] bool condition, string testName, string? extra = null)
    {
        if (!condition)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {testName} {(extra != null ? "--> " + extra : "")}");
            Console.ResetColor();
            Environment.Exit(1);
        }
        _passedTests++;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[PASS] {testName}");
        Console.ResetColor();
    }

    public static void Main()
    {
        Console.WriteLine("=================================================");
        Console.WriteLine(" EricksonLopez.SqlBuilder NativeAOT Suite        ");
        Console.WriteLine("=================================================");

        // ── 1. RawQuery Parameterized Construction ─────────────────────────────
        Console.WriteLine("\n--- 1. RawQuery Parameterization ---");

        int minAge = 18;
        string status = "Active";
        FormattableString f = $"SELECT id, name FROM users WHERE age >= {minAge} AND status = {status}";
        var rawQuery = new RawQuery(f);

        Assert(rawQuery.RawSql.Contains("@p0", StringComparison.Ordinal) && rawQuery.RawSql.Contains("@p1", StringComparison.Ordinal), "RawSql parameters replaced correctly", rawQuery.RawSql);
        Assert(rawQuery.Parameters is Dictionary<string, object?>, "Parameters dictionary created");

        var dict = (Dictionary<string, object?>)rawQuery.Parameters!;
        Assert((int)dict["@p0"]! == 18, "@p0 parameter value matches");
        Assert((string)dict["@p1"]! == "Active", "@p1 parameter value matches");

        // ── 2. Tag Support ────────────────────────────────────────────────────
        Console.WriteLine("\n--- 2. Query Tagging ---");

        var taggedQuery = rawQuery.WithTag("GetUserList");
        Assert(taggedQuery.Tag == "GetUserList", "Tagged query has tag");

        Console.WriteLine("\n=================================================");
        Console.WriteLine($" ALL {_passedTests} NATIVE AOT SUITE TESTS PASSED SUCCESSFULLY! ");
        Console.WriteLine("=== AOT Validator: OK ===");
        Console.WriteLine("=================================================");
    }
}
