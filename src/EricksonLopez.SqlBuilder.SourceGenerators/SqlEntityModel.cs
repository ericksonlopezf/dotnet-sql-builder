// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.SourceGenerators;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed record SqlEntityModel(
    string ClassName, 
    string TableName, 
    string NamespaceName, 
    bool IsRecord, 
    bool IsStruct, 
    bool IsPartial,
    List<SqlEntityPropertyModel> Properties) : IEquatable<SqlEntityModel>
{
    private readonly int _hashCode = ComputeHashCode(ClassName, TableName, NamespaceName, IsRecord, IsStruct, IsPartial, Properties);

    private static int ComputeHashCode(string className, string tableName, string nsName, bool isRec, bool isStruct, bool isPartial, List<SqlEntityPropertyModel> props)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + className.GetHashCode();
            hash = hash * 31 + tableName.GetHashCode();
            hash = hash * 31 + nsName.GetHashCode();
            hash = hash * 31 + isRec.GetHashCode();
            hash = hash * 31 + isStruct.GetHashCode();
            hash = hash * 31 + isPartial.GetHashCode();
            foreach (var p in props)
            {
                hash = hash * 31 + p.GetHashCode();
            }

            return hash;
        }
    }

    public bool Equals(SqlEntityModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (ClassName != other.ClassName ||
            TableName != other.TableName ||
            NamespaceName != other.NamespaceName ||
            IsRecord != other.IsRecord ||
            IsStruct != other.IsStruct ||
            IsPartial != other.IsPartial ||
            Properties.Count != other.Properties.Count)
        {
            return false;
        }
        
        for (int i = 0; i < Properties.Count; i++)
        {
            if (Properties[i] != other.Properties[i])
            {
                return false;
            }
        }
        
        return true;
    }

    public override int GetHashCode() => _hashCode;
}
