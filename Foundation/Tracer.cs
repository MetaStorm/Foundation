using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions;
using Foundation.CustomConfig;
using System.Dynamic;
using System.Threading;

namespace Foundation {
  public abstract class Tracer<TTracer> : Config<TTracer> where TTracer : Tracer<TTracer>, new() {
    #region Config Values
    [ConfigValue]
    public static string TraceSource { get { return KeyValue(new[] { typeof(TTracer).Name }); } }
    [ConfigValue]
    public static SourceLevels TraceSwitchLevel { get { return (SourceLevels)Enum.Parse(typeof(SourceLevels), KeyValue(), true); } }
    //[ConfigValue]
    //public static bool TraceToXml { get { return KeyValue(new[] { false }); } }
    #endregion

    #region fields
    static bool _tsCheck = false;
    static TraceSource _ts;
    public static TraceSource ts {
      get {
        if (_ts == null) {
          if (_tsCheck)
            return Task.Run(() => {
              Testable.WriteLine("Wait for ts.");
              while (_tsCheck)
                Thread.Sleep(100);
              Testable.WriteLine("Wait for ts is done.");
              return ts;
            }).Result;
          _tsCheck = true;
          try {
            _ts = new TraceSource(TraceSource);
            bool noConfig = ts.Listeners.Count <= 1 && ts.Listeners[0] is DefaultTraceListener;
            if (noConfig) {
              _ts.Switch.Level = TraceSwitchLevel;
              //if (TraceToXml) {
              //  string path = "";
              //  AddListener("xml_" + _ts.Name + "", name => {
              //    path = XmlTracePathTemplate(name);
              //    var l = new Essential.Diagnostics.RollingXmlTraceListener(path);
              //    return l;
              //  });
              //  TraceVerbose("Start xml tracing to " + path);
              //}
              AddListener("console", name => new Core.ConsoleTraceListener2() { Name = name });
            }
          } finally {
            _tsCheck = false;
          }
        }
        return Tracer<TTracer>._ts;
      }
    }
    #endregion

    static string XmlTracePathTemplate(string name) { return Core.Configer.GetConfigDirectory() + name + ".xml"; }
    protected override async Task<ExpandoObject> _RunTestFastAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
      return await TestHostAsync(parameters, async (p, m) => {
        var b = await base._RunTestFastAsync(p, m);
        return new ExpandoObject().Merge(GetTestSpace("Tracer"), new { XmlTracePathTemplate = XmlTracePathTemplate("{Source}") }.ToExpando().Merge(b));
      }, data => TraceData(TraceEventType.Error, 0, data, false, Core.TraceParams.Empty), merge);
    }
    #region Pivate mehods
    static TraceEventType Convert(EventLogEntryType eventType) {
      try {
        switch (eventType) {
          case EventLogEntryType.Error: return TraceEventType.Error;
          case EventLogEntryType.Warning: return TraceEventType.Warning;
          case EventLogEntryType.Information: return TraceEventType.Information;
          default:
            var error = "Conversion from EventLogEntryType." + eventType + "to TraceEventType is not supported.";
            throw new NotSupportedException(error);
        }
      } catch (Exception exc) {
        Core.EventLogger.LogApplicationError(exc);
        return TraceEventType.Critical;
      }
    }
    protected static void TryCatchLog(Action action) {
      try {
        action();
      } catch (Exception exc) {
        Core.EventLogger.LogApplicationError(exc);
      }
    }
    #endregion

    #region Protected methods
    public static void RemoveListener(string name) {
      ts.Listeners.Remove(name);
    }
    public static void AddListener<T>(string name, Func<string, T> listenerFactory) where T : TraceListener {
      ts.Listeners
        .OfType<T>()
        .ToList()
        .Where(l => l.Name == name)
        .DefaultIfEmpty(listenerFactory(name))
        .Where(l => !ts.Listeners.Contains(l))
        .ForEach(l => ts.Listeners.Add(l));
    }
    static Func<object, object> Format = CommonExceptionExtentions.Format;
    #endregion

    #region Public methods
    public static void TraceData(EventLogEntryType eventType, int id, object data, bool autoFormat, Core.TraceParams traceParams) {
      TraceData(Convert(eventType), id, data, autoFormat, traceParams);
    }
    public static void TraceData(TraceEventType eventType, int id, object data, bool autoFormat, Core.TraceParams traceParams) {
      var eventMessage = ((autoFormat ? Format(data) : data) ?? "").ToString();
      TryCatchLog(() => ts.TraceData(eventType, id, Core.EventLogger.TrancateMessage(eventMessage,32766), traceParams));
    }

    public static void TraceInormation(string message, Core.TraceParams traceParams) {
      TraceData(TraceEventType.Information, 0, message, false, traceParams);
    }
    public static void TraceInormation(string format, Core.TraceParams traceParams, params object[] args) {
      TraceData(TraceEventType.Information, 0, string.Format(format, args), false, traceParams);
    }

    public static void TraceVerbose(string message) { TraceVerbose(message, (Core.TraceParams)null); }
    public static void TraceVerbose(string message, Core.TraceParams traceParams) {
      TraceData(TraceEventType.Verbose, 0, message, false, traceParams);
    }
    public static void TraceVerbose(string format, params object[] args) {
      TraceVerbose(format, (Core.TraceParams)null, args);
    }
    public static void TraceVerbose(string format, Core.TraceParams traceParams, params object[] args) {
      TraceData(TraceEventType.Verbose, 0, string.Format(format, args), false, traceParams);
    }

    #endregion
  }
}
