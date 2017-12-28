using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Threading;
using System.Collections.Specialized;
using Foundation;
using Foundation.Core;
using Foundation.CustomConfig;
using System.Dynamic;
using System.IO;
namespace CommonExtensions.Tests {
  [TestClass()]
  public class EventLoggerTests {
    [TestSection]
    class EventLoggerCustom : EventLogger<EventLoggerCustom> {
      class TestSectionAttribute : Foundation.CustomConfig.ConfigSectionAttribute { }
    }
    [TestSection]
    class EventLoggerWithError : EventLogger<EventLoggerWithError> {
      class TestSectionAttribute : Foundation.CustomConfig.ConfigSectionAttribute { }
    }
    class EventLoggerMissingSource : EventLogger<EventLoggerMissingSource> {
      protected new string EventLogSource {
        get {
          return "Missing Source";
        }
      }
    }
    class EventLogDerrived : EventLoggerCustom { }
    [TestSection2]
    class EventLoggerCustom2 : EventLogger<EventLoggerCustom2> {
      class TestSection2Attribute : Foundation.CustomConfig.ConfigSectionAttribute { }
    }
    [TestMethod()]
    public void EventLogger_Verbose() {
      EventLoggerCustom.LogVerbose("Verbose Test", false);
    }
    [TestMethod]
    public void EventLoggerLongString() {
      var longMessage = new string('*', 40000);
      Foundation.Core.EventLogger.LogApplicationError(longMessage);
    }
    public static dynamic GetDepth(ExpandoObject source, params object[] skips) {
      Func<ExpandoObject, int, object> getInt = (e, i) => i >= 0 ? e.Skip(i).First().Value : e.Last().Value;
      Func<ExpandoObject, object, ExpandoObject> get = (e, i)
        => (ExpandoObject)(i.GetType() == typeof(int) ? getInt(e, (int)i) : ((IDictionary<string, object>)e)[i + ""]);
      return skips.Aggregate(source, (e, skip) => get(e, skip));
    }
    [TestMethod()]
    public async Task EventLoggerMissingSource_RunTest() {
      await EventLoggerMissingSource.RunTestAsync(null);
    }
    [TestMethod()]
    public async Task EventLogger_RunTestError() {
      var lastIndex = EventLogger.Read(EventLoggerWithError.EventLogSource, EventLogEntryType.Error, EventLoggerWithError.EventLogEventId).Select(e => e.Index).LastOrDefault();
      try {
        await EventLoggerWithError.RunTestAsync(null, e => new { merged = new[] { 0 }[1] }.ToExpando());
      } catch (IndexOutOfRangeException) {
        var d = DateTime.Now.AddSeconds(-1);
        var entry = EventLogger.Read(EventLoggerWithError.EventLogSource, EventLogEntryType.Error, EventLoggerWithError.EventLogEventId)
          .Where(e => e.Index > lastIndex)
          .Where(e => e.ReplacementStrings[0].StartsWith("Index was outside the bounds of the array."))
          .Do(e => { Console.WriteLine(new { e.TimeGenerated, d } + ""); })
          .Single();
        return;
      }
      Assert.Fail("IndexOutOfRangeException was not thrown.");
    }
    [TestMethod()]
    public async Task EventLogger_RunTest() {
      var exp = await EventLoggerCustom.RunTestAsync(null, e => new { merged = true }.ToExpando());
      Assert.IsTrue(GetDepth(exp, 0, 0, 1, 0).merged);
      Console.WriteLine(exp.ToJson());

      var d = DateTime.Now.AddSeconds(-1);
      EventLogDerrived.UnTest();
      var lastIndex = EventLogger.ReadLastIndex(EventLogDerrived.EventLogSource, EventLogDerrived.EventLogEventId, EventLogEntryType.Information);
      var test = await  EventLogDerrived.RunTestAsync(null);
      var res = (dynamic)((IDictionary<string, object>)test).First().Value;
      var entry = EventLogger.ReadContent(lastIndex, EventLogDerrived.EventLogSource, EventLogDerrived.EventLogEventId, EventLogEntryType.Information)
        .Where(s => s.StartsWith(((dynamic)res).msgLogged))
        .SingleOrDefault();
      var trace = (dynamic)((IDictionary<string, ExpandoObject>)GetDepth(test, 0, 0, 1).Trace)["EventLogSource"];
      var configFile = trace.configFile;
      Assert.AreEqual(Path.GetFileNameWithoutExtension(configFile), "FoundationTests.dll");
      Assert.AreEqual(trace.sectionName, "TestSection");
      Console.WriteLine(test.ToJson());
      Assert.IsNotNull(entry);
      var test2 = await EventLogDerrived.RunTestAsync(null);
      Console.WriteLine(test2.ToJson());
      dynamic v = ((dynamic)((test2).ToArray()[0].Value));
      Assert.IsTrue(v.IsTested);
    }
    [TestMethod()]
    public async Task EventLogger2_RunTest() {
      var d = DateTime.Now.AddSeconds(-1);
      var lastIndex = EventLogger.Read(EventLoggerCustom2.EventLogSource, EventLogEntryType.Information, EventLoggerCustom2.EventLogEventId).Select(e => e.Index).LastOrDefault();
      var test = await EventLoggerCustom2.RunTestAsync(null);
      var res = (dynamic)((IDictionary<string, object>)test).First().Value;
      var entry = EventLogger.Read(EventLoggerCustom2.EventLogSource, EventLogEntryType.Information, EventLoggerCustom2.EventLogEventId)
        .Where(e => {
          return e.Index > lastIndex;
        })
        .Where(e => e.ReplacementStrings[0].StartsWith(((dynamic)res).msgLogged))
        .Do(e => { Console.WriteLine(new { e.TimeGenerated, d } + ""); })
        .SingleOrDefault();
      var trace = (dynamic)((IDictionary<string, ExpandoObject>)GetDepth(test, 0, 0, 1).Trace)["EventLogSource"];
      var configFile = trace.configFile;
      Assert.AreEqual(Path.GetFileNameWithoutExtension(configFile), "FoundationTests.dll");
      Assert.AreEqual(trace.sectionName, "TestSection2");
      Console.WriteLine(test.ToJson());
      Assert.IsNotNull(entry);
    }
  }
}
