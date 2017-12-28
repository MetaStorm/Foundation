using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommonExtensions;
using System.Reflection;
using System.Dynamic;
using Foundation;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.Entity;

namespace Foundation.Tests {
  [TestClass()]
  public class TestableTests {
    class TestConfig : Foundation.Testable<TestConfig> {
      protected override async Task<ExpandoObject> _RunTestFastAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
        Func<ExpandoObject, ExpandoObject> f = p => p.Merge("Merged", "Test");
        var m = merge.Concat(new[] { f }).ToArray();
        return await base._RunTestFastAsync(parameters, m);
      }
    }
    [TestMethod]
    public async Task Merge() {
      var mt = await TestConfig.RunTestFastAsync(null);
      Console.WriteLine(mt.ToJson());
      Assert.AreEqual(((dynamic)mt.First().Value).Merged, "Test");
    }
    public async Task RunAllTests() {
      var expected = new[]{
          "Section = MissingSection, Key = SectionKeyCustomClassLevelMissing, Error = Section is missing",
          "z48z is not a valid value for Int32",
          "Test Exceptipon",
          "No connection string named 'tSQLtEntities' could be found in the application config file."
        };
      var exceptionTypes = new List<Type>();
      Func<Exception, bool> eh = exc => {
        exceptionTypes.Add(((TestableException)exc).TestedType);
        Console.WriteLine("**************** Exception Will Robenson!!! ***************\n" + exc.Format());
        return exc.Messages().Any(msg => expected.Any(tm => msg.Contains(tm)));
      };
      // Run tests
      Assert.IsTrue(Testable.UnTest().Any());
      var testOutput = await Testable.RunTestsAsync(false, eh);
      // Process results
      var header = "\n*************** Tests Outputs *******************\n";
      Console.WriteLine(header + string.Join(header, testOutput.Values.Select(e => e.ToJson())));
      var typeaAll = Testable.GetAllTypes(true)
        .Except(exceptionTypes)
        .Select(t => t.Name).OrderBy(s => s)
        .ToArray();
      Console.WriteLine("********** Testable Types ************" + typeaAll.ToJson());
      Console.WriteLine("********** Tested Types ************" + testOutput.Keys.Select(t => t.Name).OrderBy(s => s).ToJson());
      Assert.AreEqual(typeaAll.Length, testOutput.Count);
    }

    public partial class tSQLtEntities : DbContext {
      public tSQLtEntities()
          : base("name=tSQLtEntities") {
      }
    }

    class DBLogger2 : DBLogger<tSQLtEntities>{ }
    [TestMethod]
    public void CleanConnectionString() {
      var cs = "Data Source=dataSource;Initial Catalog=catalog;User ID=merauser";
      var app= ";APP=app";
      var pwd = ";Password=dontshowme";
      Assert.AreEqual(cs + app, DBLogger2.CleanConnectionString(cs + pwd + app));
    }
    [TestMethod()]
    public async Task ShouldRunTests() {
      //Testable.Verbose = true;
      if (true) {
        Testable.ShouldRunTests = true;
        await RunAllTests();
      } else Assert.Inconclusive("Run this test as stand-along.");
    }
    [TestMethod]
    public void GetAllTestTypes() {
      var types = Testable.GetAllTypes(true);
      Assert.IsTrue(types.Count() > 0);
    }
    [TestMethod]
    public void GetAllTypes() {
      var types = Testable.GetAllTypes()
        .Select(t => new { Module = t.Module.FullyQualifiedName, t.FullName, MemberType = t.MemberType + "" })
        .Where(x=>x.FullName.StartsWith("Foundation.Core."));
     types  .ForEach(x=>Console.WriteLine(x.ToJson()));
     Assert.IsTrue(types.Count() > 0);
    }
    [TestClass]
    public class UnitTest1 {
      class TestableTest : Testable<TestableTest> {
        protected override async Task<ExpandoObject> _RunTestFastAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
          return await TestHostAsync(parameters, async (p, m) => {
            return await base._RunTestFastAsync(p, m);
          }, null, merge);
        }
      }
      class TestableTestThrow : Testable<TestableTestThrow> {
        protected override async Task<ExpandoObject> _RunTestFastAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
          return await TestHostAsync(parameters, (p, m) => {
            throw new Exception("Test Exceptipon");
          }, null, merge);
        }
      }
      [TestMethod]
      public async Task TestableHost_RunTest() {
        var e = await TestableTest.RunTestAsync(null);
        Assert.IsTrue(((dynamic)e.First().Value).IsTested);
      }
      [TestMethod]
      [ExpectedException(typeof(Exception))]
      public async Task TestableHostThrow() {
        try {
          dynamic e = await TestableTestThrow.RunTestAsync(null);
          Assert.IsNull(e);
          Assert.Fail("Exception was not thrown");
        } catch (TypeInitializationException exc) {
          ExceptionDispatchInfo.Capture(exc.GetBaseException()).Throw();
        }
      }
      static void WriteLine(string text) {
        if (!System.Console.IsOutputRedirected)
          Console.WriteLine(text);
      }
    }
  }
}
