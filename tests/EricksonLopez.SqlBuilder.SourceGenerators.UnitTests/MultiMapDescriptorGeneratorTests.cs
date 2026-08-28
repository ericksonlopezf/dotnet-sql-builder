// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace EricksonLopez.SqlBuilder.SourceGenerators.Tests
{
    
    public class MultiMapDescriptorGeneratorTests : GeneratorTestBase
    {
        [Fact]
        public Task Generator_CreatesMultiMapDescriptor_ForAnnotatedClass()
        {
            var source = @"
using EricksonLopez.SqlBuilder.Annotations;

namespace TestNamespace
{
    [SqlEntity(""users"")]
    public partial class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        [SqlEntityReference(""role_id"", ""id"")]
        public Role Role { get; set; }
    }

    [SqlEntity(""roles"")]
    public partial class Role
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<MultiMapDescriptorGenerator>(source);
        }

        [Fact]
        public Task Generator_Handles_MultipleReferences()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity(""users"")]
    public partial class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        [SqlEntityReference(""role_id"", ""id"")]
        public Role Role { get; set; }
        
        [SqlEntityReference(""company_id"", ""id"")]
        public Company Company { get; set; }
    }

    [SqlEntity(""roles"")]
    public partial class Role { public int Id { get; set; } }
    
    [SqlEntity(""companies"")]
    public partial class Company { public int Id { get; set; } }
}";
            return VerifyGeneratedSourceAsync<MultiMapDescriptorGenerator>(source);
        }
        
        [Fact]
        public Task Generator_Handles_NoReferences()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity(""users"")]
    public partial class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<MultiMapDescriptorGenerator>(source);
        }
        [Fact]
        public Task Generator_Ignores_InvalidMembers()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity(""invalid_table"")]
    public partial class InvalidEntity
    {
        public int Id { get; set; }
        public static string StaticProp { get; set; }
        public string PrivateSet { get; private set; }
        public string NoGet { set { } }
        private string PrivateProp { get; set; }
        protected string ProtectedProp { get; set; }
        
        public void SomeMethod() { }
        public string SomeField = ""test"";
    }
}";
            return VerifyGeneratedSourceAsync<MultiMapDescriptorGenerator>(source);
        }
        [Fact]
        public Task Generator_Handles_InvalidSyntax()
        {
            var source = @"

namespace TestNamespace
{
    [SqlEntity]
    public partial class // syntax error here missing class name!
    {
        public int Id { get; set; }
    }
}";
            return VerifyGeneratedSourceAsync<MultiMapDescriptorGenerator>(source);
        }
    }
}



