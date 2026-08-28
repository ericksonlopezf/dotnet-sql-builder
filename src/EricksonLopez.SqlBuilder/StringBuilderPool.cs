// Copyright © Erickson Lopez. MIT License.
using System;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace EricksonLopez.SqlBuilder;

internal static class StringBuilderPool
{
    private static readonly ObjectPool<StringBuilder> _pool = 
        new DefaultObjectPoolProvider().Create(new StringBuilderPooledObjectPolicy { InitialCapacity = 512, MaximumRetainedCapacity = 4096 });
    
    /// <summary>
    /// Gets a pooled <see cref="StringBuilder"/> instance.
    /// </summary>
    /// <returns>A clean string builder.</returns>
    public static StringBuilder Get() => _pool.Get();
    
    /// <summary>
    /// Returns a <see cref="StringBuilder"/> to the pool.
    /// </summary>
    /// <param name="sb">The string builder to return.</param>
    public static void Return(StringBuilder sb) => _pool.Return(sb);
}

