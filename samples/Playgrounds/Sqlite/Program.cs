// Copyright © Erickson Lopez. MIT License.
using System;
using System.Reflection;
using EricksonLopez.Pagination;

internal sealed class Program
{
    static void Main()
    {
        foreach (var m in typeof(PagedList<int>).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            Console.WriteLine(m);
        }
    }
}


