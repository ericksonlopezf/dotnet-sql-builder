// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Oracle;

/// <summary>
/// Specifies the target Oracle Database dialect version for SQL compilation.
/// </summary>
public enum OracleDialectVersion
{
    /// <summary>
    /// Oracle Database 12c Release 1 (12.1) and newer.
    /// Uses native ANSI SQL:2008 standard OFFSET / FETCH NEXT ROWS pagination syntax.
    /// </summary>
    Oracle12cPlus = 0,

    /// <summary>
    /// Oracle Database 11g Release 2 (11.2) and older.
    /// Emulates OFFSET and LIMIT pagination using ROWNUM nested subqueries.
    /// </summary>
    Oracle11g = 1
}
