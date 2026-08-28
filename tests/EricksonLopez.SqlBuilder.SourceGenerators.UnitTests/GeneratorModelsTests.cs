// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.SourceGenerators;
using Xunit;

namespace EricksonLopez.SqlBuilder.SourceGenerators.Tests
{
    public class GeneratorModelsTests
    {
        [Fact]
        public void PropertyModel_Equals_WorksCorrectly()
        {
            var p1 = new PropertyModel("Id", "int", true, false);
            var p2 = new PropertyModel("Id", "int", true, false);
            var p3 = new PropertyModel("Name", "string", false, true);
            var p4 = new PropertyModel("Id", "long", true, false);
            var p5 = new PropertyModel("Id", "int", false, false);
            var p6 = new PropertyModel("Id", "int", true, true);
            var p7 = new PropertyModel("Id", "string", true, false);

            Assert.True(p1.Equals(p2));
            Assert.True(p1 == p2);
            Assert.False(p1 != p2);
            Assert.True(p1.Equals((object)p2));
            Assert.False(p1.Equals(null));
            Assert.False(p1.Equals(p3));
            Assert.False(p1.Equals(p4));
            Assert.False(p1.Equals(p5));
            Assert.False(p1.Equals(p6));
            Assert.False(p1.Equals(p7));

            Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
            Assert.NotEqual(p1.GetHashCode(), p3.GetHashCode());
        }
        
        [Fact]
        public void SqlEntityPropertyModel_Equals_WorksCorrectly()
        {
            var p1 = new SqlEntityPropertyModel("Id", "int", true, false, false, "GetInt32", "");
            var p2 = new SqlEntityPropertyModel("Id", "int", true, false, false, "GetInt32", "");
            var p3 = new SqlEntityPropertyModel("Name", "string", true, false, false, "GetString", "");
            var p4 = new SqlEntityPropertyModel("Id", "int", false, false, false, "GetInt32", "");
            var p5 = new SqlEntityPropertyModel("Id", "int", true, true, false, "GetInt32", "");
            var p6 = new SqlEntityPropertyModel("Id", "int", true, false, true, "GetInt32", "");
            var p7 = new SqlEntityPropertyModel("Id", "int", true, false, false, "GetInt64", "");
            var p8 = new SqlEntityPropertyModel("Id", "int", true, false, false, "GetInt32", "long");
            var p9 = new SqlEntityPropertyModel("CustomId", "int", true, false, false, "GetInt32", "");

            Assert.True(p1.Equals(p2));
            Assert.True(p1 == p2);
            Assert.False(p1 != p2);
            Assert.True(p1.Equals((object)p2));
            Assert.False(p1.Equals(null));
            Assert.False(p1.Equals(p3));
            Assert.False(p1.Equals(p4));
            Assert.False(p1.Equals(p5));
            Assert.False(p1.Equals(p6));
            Assert.False(p1.Equals(p7));
            Assert.False(p1.Equals(p8));
            Assert.False(p1.Equals(p9));

            Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
            Assert.NotEqual(p1.GetHashCode(), p3.GetHashCode());
        }

        [Fact]
        public void FilterModel_Equals_WorksCorrectly()
        {
            var p1 = new PropertyModel("Id", "int", true, false);
            var p2 = new PropertyModel("Name", "string", false, true);

            var m1 = new FilterModel("User", "Ns", new List<PropertyModel> { p1 });
            var m2 = new FilterModel("User", "Ns", new List<PropertyModel> { p1 });
            var m3 = new FilterModel("User2", "Ns", new List<PropertyModel> { p1 });
            var m4 = new FilterModel("User", "Ns2", new List<PropertyModel> { p1 });
            var m5 = new FilterModel("User", "Ns", new List<PropertyModel> { p2 });
            var m6 = new FilterModel("User", "Ns", new List<PropertyModel> { p1, p2 });

            Assert.True(m1.Equals(m2));
            Assert.True(m1 == m2);
            Assert.False(m1 != m2);
            Assert.True(m1.Equals((object)m2));
            Assert.False(m1.Equals(null));
            Assert.False(m1.Equals(m3));
            Assert.False(m1.Equals(m4));
            Assert.False(m1.Equals(m5));
            Assert.False(m1.Equals(m6));

            Assert.Equal(m1.GetHashCode(), m2.GetHashCode());
            Assert.NotEqual(m1.GetHashCode(), m3.GetHashCode());
        }

        [Fact]
        public void SqlEntityModel_Equals_WorksCorrectly()
        {
            var p1 = new SqlEntityPropertyModel("Id", "int", true, false, false, "GetInt32", "");
            var p2 = new SqlEntityPropertyModel("Name", "string", true, false, false, "GetString", "");

            var m1 = new SqlEntityModel("users", "User", "Ns", false, false, true, new List<SqlEntityPropertyModel> { p1 });
            var m2 = new SqlEntityModel("users", "User", "Ns", false, false, true, new List<SqlEntityPropertyModel> { p1 });
            var m3 = new SqlEntityModel("users2", "User", "Ns", false, false, true, new List<SqlEntityPropertyModel> { p1 });
            var m4 = new SqlEntityModel("users", "User2", "Ns", false, false, true, new List<SqlEntityPropertyModel> { p1 });
            var m5 = new SqlEntityModel("users", "User", "Ns2", false, false, true, new List<SqlEntityPropertyModel> { p1 });
            var m6 = new SqlEntityModel("users", "User", "Ns", true, false, true, new List<SqlEntityPropertyModel> { p1 });
            var m7 = new SqlEntityModel("users", "User", "Ns", false, true, true, new List<SqlEntityPropertyModel> { p1 });
            var m8 = new SqlEntityModel("users", "User", "Ns", false, false, false, new List<SqlEntityPropertyModel> { p1 });
            var m9 = new SqlEntityModel("users", "User", "Ns", false, false, true, new List<SqlEntityPropertyModel> { p2 });
            var m10 = new SqlEntityModel("users", "User", "Ns", false, false, true, new List<SqlEntityPropertyModel> { p1, p2 });

            Assert.True(m1.Equals(m2));
            Assert.True(m1 == m2);
            Assert.False(m1 != m2);
            Assert.True(m1.Equals((object)m2));
            Assert.False(m1.Equals(null));
            Assert.False(m1.Equals(m3));
            Assert.False(m1.Equals(m4));
            Assert.False(m1.Equals(m5));
            Assert.False(m1.Equals(m6));
            Assert.False(m1.Equals(m7));
            Assert.False(m1.Equals(m8));
            Assert.False(m1.Equals(m9));
            Assert.False(m1.Equals(m10));

            Assert.Equal(m1.GetHashCode(), m2.GetHashCode());
            Assert.NotEqual(m1.GetHashCode(), m3.GetHashCode());
        }

        [Fact]
        public void SqlEntityModel_ObjectMethods()
        {
            var p1 = new SqlEntityPropertyModel("Id", "int", true, false, false, "GetInt32", "");
            var m1 = new SqlEntityModel("users", "User", "Ns", false, false, true, new List<SqlEntityPropertyModel> { p1 });
            var m2 = new SqlEntityModel("users", "User", "Ns", false, false, true, new List<SqlEntityPropertyModel> { p1 });

            // Test object.Equals
            Assert.True(m1.Equals((object)m2));
            Assert.False(m1.Equals(new object()));
            Assert.False(m1.Equals((object?)null));

            // Test operator overloads
            Assert.True(m1 == m2);
            Assert.False(m1 != m2);

            SqlEntityModel? m3 = null;
            SqlEntityModel? m4 = null;
            Assert.True(m3 == m4);
            Assert.False(m3 == m1);
            Assert.True(m3 != m1);
        }

        [Fact]
        public void FilterModel_ObjectMethods()
        {
            var p1 = new PropertyModel("Id", "int", true, false);
            var m1 = new FilterModel("User", "Ns", new List<PropertyModel> { p1 });
            var m2 = new FilterModel("User", "Ns", new List<PropertyModel> { p1 });

            Assert.True(m1.Equals((object)m2));
            Assert.False(m1.Equals(new object()));
            Assert.False(m1.Equals((object?)null));

            Assert.True(m1 == m2);
            Assert.False(m1 != m2);

            FilterModel? m3 = null;
            FilterModel? m4 = null;
            Assert.True(m3 == m4);
            Assert.False(m3 == m1);
            Assert.True(m3 != m1);
        }

        [Fact]
        public void PropertyModel_ObjectMethods()
        {
            var p1 = new PropertyModel("Id", "int", true, false);
            var p2 = new PropertyModel("Id", "int", true, false);

            Assert.True(p1.Equals((object)p2));
            Assert.False(p1.Equals(new object()));
            Assert.False(p1.Equals((object?)null));

            Assert.True(p1 == p2);
            Assert.False(p1 != p2);

            PropertyModel? p3 = null;
            PropertyModel? p4 = null;
            Assert.True(p3 == p4);
            Assert.False(p3 == p1);
            Assert.True(p3 != p1);
        }
        
        [Fact]
        public void SqlEntityPropertyModel_ObjectMethods()
        {
            var p1 = new SqlEntityPropertyModel("Id", "int", true, false, false, "GetInt32", "");
            var p2 = new SqlEntityPropertyModel("Id", "int", true, false, false, "GetInt32", "");

            Assert.True(p1.Equals((object)p2));
            Assert.False(p1.Equals(new object()));
            Assert.False(p1.Equals((object?)null));

            Assert.True(p1 == p2);
            Assert.False(p1 != p2);

            SqlEntityPropertyModel? p3 = null;
            SqlEntityPropertyModel? p4 = null;
            Assert.True(p3 == p4);
            Assert.False(p3 == p1);
            Assert.True(p3 != p1);
        }
    }
}

