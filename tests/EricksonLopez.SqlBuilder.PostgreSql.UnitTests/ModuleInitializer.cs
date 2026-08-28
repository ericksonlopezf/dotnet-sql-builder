// Copyright © Erickson Lopez. MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using VerifyXunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Verifier.DerivePathInfo(
            (sourceFile, projectDirectory, type, method) => new(
                directory: Path.Combine(projectDirectory, "__snapshots__"),
                typeName: type.Name,
                methodName: method.Name));
    }
}
