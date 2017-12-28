using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Collections;
using CommonExtensions;
using System.Threading.Tasks;
using System.IO;
using Essential.Diagnostics;

namespace Foundation.Tests {
  [TestClass]
  public class Traceable {
    [TraceTesterSection]
    class TraceTester : Tracer<TraceTester> {
      class TraceTesterSectionAttribute : Foundation.CustomConfig.ConfigSectionAttribute { }
    }
    static TraceSource ts = new TraceSource("TraceTest1");
    static bool _noConfig = ts.Listeners[0] is DefaultTraceListener;
    [TestMethod]
    [Switch("SourceSwitch", typeof(SourceSwitch))]
    public void TraceSource_Props() {
      if (_noConfig) {
        ts.Listeners.Clear();
        //SourceSwitch sourceSwitch = new SourceSwitch("SourceSwitch", "Verbose");
        //ts.Switch = sourceSwitch;
        ts.Switch.Level = SourceLevels.Error;
        int idxConsole = ts.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
        ts.Listeners[idxConsole].Name = "console";
      }
      DisplayProperties(ts);
      Console.WriteLine("*".PadLeft(80,'*'));
      ts.Listeners["console"].TraceOutputOptions |= TraceOptions.Callstack;
      ts.TraceEvent(TraceEventType.Warning, 1);
      ts.Listeners["console"].TraceOutputOptions = TraceOptions.DateTime;
      // Issue file not found message as a warning.
      ts.TraceEvent(TraceEventType.Warning, 2, "File Test not found");
      // Issue file not found message as a verbose event using a formatted string.
      ts.TraceEvent(TraceEventType.Verbose, 3, "File {0} not found.", "test");
      // Issue file not found message as information.
      ts.TraceInformation("File {0} not found.", "test");
      ts.Listeners["console"].TraceOutputOptions |= TraceOptions.LogicalOperationStack;
      // Issue file not found message as an error event.
      ts.TraceEvent(TraceEventType.Error, 4, "File {0} not found.", "test");
      // Test the filter on the ConsoleTraceListener.
      ts.Listeners["console"].Filter = new SourceFilter("No match");
      ts.TraceData(TraceEventType.Error, 5,
          "SourceFilter should reject this message for the console trace listener.");
      ts.Listeners["console"].Filter = new SourceFilter("TraceTest");
      ts.TraceData(TraceEventType.Error, 6,
          "SourceFilter should let this message through on the console trace listener.");
      ts.Listeners["console"].Filter = null;
      // Use the TraceData method. 
      ts.TraceData(TraceEventType.Warning, 7, new object());
      ts.TraceData(TraceEventType.Warning, 8, new object[] { "Message 1", "Message 2" });
      // Activity tests.
      ts.TraceEvent(TraceEventType.Start, 9, "Will not appear until the switch is changed.");
      ts.Switch.Level = SourceLevels.ActivityTracing | SourceLevels.Critical;
      ts.TraceEvent(TraceEventType.Suspend, 10, "Switch includes ActivityTracing, this should appear");
      ts.TraceEvent(TraceEventType.Critical, 11, "Switch includes Critical, this should appear");
      ts.Flush();
      ts.Close();
    }
    public static void DisplayProperties(TraceSource ts) {
      Console.WriteLine("TraceSource name = " + ts.Name);
      Console.WriteLine("TraceSource switch level = " + ts.Switch.Level);
      Console.WriteLine("TraceSource switch = " + ts.Switch.DisplayName);
      SwitchAttribute[] switches = SwitchAttribute.GetAll(typeof(Traceable).Assembly);
      for (int i = 0; i < switches.Length; i++) {
        Console.WriteLine("Switch name = " + switches[i].SwitchName);
        Console.WriteLine("Switch type = " + switches[i].SwitchType);
      }
      if (!_noConfig) {
        // Get the custom attributes for the TraceSource.
        Console.WriteLine("Number of custom trace source attributes = "
            + ts.Attributes.Count);
        foreach (DictionaryEntry de in ts.Attributes)
          Console.WriteLine("Custom trace source attribute = "
              + de.Key + "  " + de.Value);
        // Get the custom attributes for the trace source switch. 
        foreach (DictionaryEntry de in ts.Switch.Attributes)
          Console.WriteLine("Custom switch attribute = "
              + de.Key + "  " + de.Value);
      }
      Console.WriteLine("Number of listeners = " + ts.Listeners.Count);
      foreach (TraceListener traceListener in ts.Listeners) {
        Console.Write("TraceListener: " + traceListener.Name + "\t");
        // The following output can be used to update the configuration file.
        Console.WriteLine("AssemblyQualifiedName = " +
            (traceListener.GetType().AssemblyQualifiedName));
      }
    }

    [TestMethod]
    public async Task TraceTester_RunTest() {
      using (new ActivityScope(TraceTester.ts,0,1,2,3)) {
        var test = await TraceTester.RunTestAsync(null);
        TraceTester.TraceInormation(test.ToJson(), null);
        await Task.Run(() => {
          using (new ActivityScope(TraceTester.ts, 0, 1, 2, 3)) {
            TraceTester.TraceVerbose(test.ToJson());
          }
        });
      }
    }
    [TestMethod]
    public void TraceLongString() {
      var longMessage = new string('*', 40000);
      TraceTester.TraceData(EventLogEntryType.Warning, 0, longMessage, false, Core.TraceParams.Empty);
    }
    [TestMethod]
    public void ActionTraceListener() {
      TraceTester.AddListener("action", name => new ActionTraceListener(name
        , (source, eventType, id, message) => {
          var trace = new { ActionTraceListener = new { source, eventType, message } }.ToJson();
          Core.EventLogger.LogApplicationError(trace, id);
          using (var f = File.CreateText("ActionTraceListener.txt"))
            f.Write(trace);
        }));
      TraceTester.TraceVerbose("Action Trace Listener Test Message");
      var json = File.ReadAllText("ActionTraceListener.txt");
      dynamic d = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
      Assert.IsTrue(d.ActionTraceListener.source == "TraceTester");
      File.Delete("ActionTraceListener.txt");
    }
  }
}
