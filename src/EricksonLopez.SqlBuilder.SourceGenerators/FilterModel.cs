// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.SourceGenerators
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal record FilterModel(string ClassName, string NamespaceName, List<PropertyModel> Properties) : IEquatable<FilterModel>
    {
        public virtual bool Equals(FilterModel? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (ClassName != other.ClassName || NamespaceName != other.NamespaceName || Properties.Count != other.Properties.Count)
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

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + ClassName.GetHashCode();
                hash = hash * 31 + NamespaceName.GetHashCode();
                foreach (var p in Properties)
                {
                    hash = hash * 31 + p.GetHashCode();
                }

                return hash;
            }
        }
    }
}
