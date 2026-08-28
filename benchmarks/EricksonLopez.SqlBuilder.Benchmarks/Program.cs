// Copyright © Erickson Lopez. MIT License.
using System;
using BenchmarkDotNet.Running;

namespace EricksonLopez.SqlBuilder.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}

