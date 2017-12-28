using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions;
using System.Dynamic;
using Foundation.CustomConfig;
using System.Diagnostics;
using System.Reflection;
using System.Data.SqlClient;
using System.Runtime.ExceptionServices;

namespace Foundation {
  public abstract class EventLogger<TLogger> : Logger<TLogger> where TLogger : EventLogger<TLogger>, new() {
    #region fields
    static bool _traceInitialized = false;
    static Lazy<Core.TraceParams> _traceParams = new Lazy<Core.TraceParams>(() => new Core.TraceParams("eventSource", EventLogSource));
    #endregion

    static void AddListeners() {
      if (!_traceInitialized) {
        AddListener(EventLogSource, name => TraceListenerFactory(new Core.EventLogTraceListener2(name)));
        _traceInitialized = true;
      }
    }
    protected static T TraceListenerFactory<T>(T traceListener) where T : TraceListener {
      traceListener.Filter = new Core.EventLogSourceFilter(EventLogSource);
      return traceListener;
    }
    protected void _LogError(object msg) { _LogEvent(msg, EventLogEntryType.Error); }
    override protected void _LogEvent(object msg,EventLogEntryType entryType) {
      try {
        AddListeners();
        TraceData(entryType, msg);
      } catch (Exception exc) {
        Core.EventLogger.LogApplication(exc, 0);
        throw;
      }
    }
    public static void LogVerbose(object data,bool autoFormat) {
      if (TraceSwitchLevel == SourceLevels.Verbose)
        AddListeners();
      TraceData(TraceEventType.Verbose, EventLogEventId, data, autoFormat, _traceParams.Value);
    }
    protected void TraceData(EventLogEntryType eventType, object data) {
      TraceData(eventType, EventLogEventId, data, true, _traceParams.Value);
    }
    #region RunTest
    protected override async Task<ExpandoObject> _RunTestFastAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
      return await TestHostAsync(parameters, async(p, m) => {
        try {
          var res = await base._RunTestFastAsync(p, m);
          var msgLogged = "[" + typeof(TLogger).FullName + "]\nEventLogger Test";
          object runTrace = (object)true;
          ((IDictionary<string, object>)(parameters ?? new ExpandoObject()))
            .TryGetValue("runTrace", out runTrace);
          if ((bool)(runTrace ?? true)) {
            LogEvent(msgLogged, EventLogEntryType.Information);
            res.AddOrUpdate("msgLogged", msgLogged);
          }
          return new ExpandoObject().Merge(GetTestSpace("EventLogger"), res);
        } catch (TargetInvocationException exc) {
          Core.EventLogger.LogApplication(exc.InnerException);
          ExceptionDispatchInfo.Capture(exc.InnerException).Throw();
          return null;
        } catch (Exception exc) {
          Core.EventLogger.LogApplication(exc);
          throw;
        }
      }, _LogError, merge);
    }
    #endregion

    #region Config Values
    [ConfigValue]
    public static string EventLogSource { get { return KeyValue(); ; } }

    [ConfigValue]
    public static int EventLogEventId { get { return KeyValue<int>(); } }
    #endregion
  }
}
