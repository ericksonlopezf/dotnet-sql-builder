// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing.UnitTests;

public class GoldenFileAssertTests
{
    [Fact]
    public void MatchesGoldenFile_CreatesDirectoryAndFile_WhenNotExisting()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Select("id");
        var tempBase = Path.Combine(Path.GetTempPath(), "golden_suite_" + Guid.NewGuid().ToString("N"));
        var subDir = Path.Combine(tempBase, "deep", "nested");
        var goldenFile = Path.Combine(subDir, "query.sql");

        try
        {
            // Directory does not exist yet. updateGoldenFiles is false, but file doesn't exist -> creates it
            GoldenFileAssert.MatchesGoldenFile(query, compiler, goldenFile, updateGoldenFiles: false);
            Assert.True(Directory.Exists(subDir));
            Assert.True(File.Exists(goldenFile));
            var content = File.ReadAllText(goldenFile);
            Assert.Equal("SELECT \"id\" FROM \"testingusers\"", content);
        }
        finally
        {
            if (Directory.Exists(tempBase)) Directory.Delete(tempBase, true);
        }
    }

    [Fact]
    public void MatchesGoldenFile_OverwritesExistingFile_WhenUpdateGoldenFilesIsTrue()
    {
        var compiler = new PostgreSqlCompiler();
        var tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempFile, "OLD SQL");
            var query = Sql.From<TestingUser>().Select("id");

            GoldenFileAssert.MatchesGoldenFile(query, compiler, tempFile, updateGoldenFiles: true);

            var updated = File.ReadAllText(tempFile);
            Assert.Equal("SELECT \"id\" FROM \"testingusers\"", updated);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MatchesGoldenFile_RespectsNormalizeWhitespace()
    {
        var compiler = new PostgreSqlCompiler();
        var query = new RawQuery("SELECT   id   FROM    t");
        var tempFile = Path.GetTempFileName();

        try
        {
            // Write un-normalized text in golden file
            File.WriteAllText(tempFile, "SELECT \r\n  id \t FROM   t");

            // With normalizeWhitespace = true (default), matches
            GoldenFileAssert.MatchesGoldenFile(query, compiler, tempFile, updateGoldenFiles: false, normalizeWhitespace: true);

            // With normalizeWhitespace = false, throws because strings differ
            var ex = Assert.Throws<InvalidOperationException>(() =>
                GoldenFileAssert.MatchesGoldenFile(query, compiler, tempFile, updateGoldenFiles: false, normalizeWhitespace: false));

            Assert.Contains("Golden File Mismatch for file", ex.Message);
            Assert.Contains("Expected:\n", ex.Message);
            Assert.Contains("Actual:\n", ex.Message);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MatchesGoldenFile_WhenSqlDiffers_ThrowsWithExactMessage()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Select("name");
        var tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempFile, "SELECT \"id\" FROM \"testingusers\"");

            var ex = Assert.Throws<InvalidOperationException>(() =>
                GoldenFileAssert.MatchesGoldenFile(query, compiler, tempFile, updateGoldenFiles: false));

            Assert.Contains($"Golden File Mismatch for file '{tempFile}'.", ex.Message);
            Assert.Contains("Expected:\nSELECT \"id\" FROM \"testingusers\"", ex.Message);
            Assert.Contains("Actual:\nSELECT \"name\" FROM \"testingusers\"", ex.Message);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MatchesGoldenFile_WhenNormalizeWhitespaceFalse_AndExactRawSqlMatches_Succeeds()
    {
        var compiler = new PostgreSqlCompiler();
        var rawSql = "SELECT   id   \n   FROM   t";
        var query = new RawQuery(rawSql);
        var tempFile = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempFile, rawSql);
            // With normalizeWhitespace = false, actualSql must remain un-normalized rawSql to match expectedSql.
            GoldenFileAssert.MatchesGoldenFile(query, compiler, tempFile, updateGoldenFiles: false, normalizeWhitespace: false);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MatchesGoldenFile_WhenFilePathHasNoDirectory_DoesNotThrowArgumentException()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Select("id");
        var fileName = "temp_local_golden_" + Guid.NewGuid().ToString("N") + ".sql";

        try
        {
            // goldenFilePath has no directory part (dir is empty string).
            // With || mutation, it attempts Directory.CreateDirectory("") which throws ArgumentException.
            GoldenFileAssert.MatchesGoldenFile(query, compiler, fileName, updateGoldenFiles: true);
            Assert.True(File.Exists(fileName));
        }
        finally
        {
            if (File.Exists(fileName)) File.Delete(fileName);
        }
    }

    [Fact]
    public void MatchesGoldenFile_UpdateGoldenFiles_ReturnsEarlyWithoutReading()
    {
        var compiler = new PostgreSqlCompiler();
        var query = Sql.From<TestingUser>().Select("id");
        var tempFile = Path.GetTempFileName();

        try
        {
            if (OperatingSystem.IsWindows())
            {
                var fileInfo = new FileInfo(tempFile);
                var acl = fileInfo.GetAccessControl();
                var user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                acl.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                    user,
                    System.Security.AccessControl.FileSystemRights.Read,
                    System.Security.AccessControl.AccessControlType.Deny));
                fileInfo.SetAccessControl(acl);
            }
            else
            {
                File.SetUnixFileMode(tempFile, UnixFileMode.UserWrite);
            }

            // When updateGoldenFiles is true, it writes the file and returns immediately.
            // If return; is removed, it attempts to execute File.ReadAllText(goldenFilePath),
            // which throws UnauthorizedAccessException because Read permission is denied.
            GoldenFileAssert.MatchesGoldenFile(query, compiler, tempFile, updateGoldenFiles: true);
        }
        finally
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var fileInfo = new FileInfo(tempFile);
                    var acl = fileInfo.GetAccessControl();
                    var user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    acl.RemoveAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                        user,
                        System.Security.AccessControl.FileSystemRights.Read,
                        System.Security.AccessControl.AccessControlType.Deny));
                    fileInfo.SetAccessControl(acl);
                }
                catch {}
            }
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MatchesGoldenFile_EmptyQuery_NormalizedToEmptyString()
    {
        var compiler = new PostgreSqlCompiler();
        var emptyQuery = new StubQuery("   \t  \r\n ");
        var tempFile = Path.GetTempFileName();

        try
        {
            // Updating golden file with empty query verifies that actualSql written to disk is string.Empty, not mutated string
            GoldenFileAssert.MatchesGoldenFile(emptyQuery, compiler, tempFile, updateGoldenFiles: true);
            var content = File.ReadAllText(tempFile);
            Assert.Equal(string.Empty, content);

            // Asserting against empty golden file passes
            GoldenFileAssert.MatchesGoldenFile(emptyQuery, compiler, tempFile, updateGoldenFiles: false);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
