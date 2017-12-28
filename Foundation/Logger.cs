using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions;

namespace Foundation {
  public abstract class Logger<T> : Tracer<T> where T : Logger<T>, new() {
    public static Action<object> Trace = o => { };
    public static void LogApplicationError(object o,int eventId = 0) {
      Core.EventLogger.LogApplicationError(o, eventId);
    }
    public delegate void LogErrorDelegate(object error);
    public delegate void LogEventDelegate(object error,EventLogEntryType entryType);
    public static LogErrorDelegate LogError { get { return o => Instance._LogEvent(o, EventLogEntryType.Error); } }
    public static LogEventDelegate LogEvent { get { return Instance._LogEvent; } }
    //protected abstract void _LogError(object msg);
    protected abstract void _LogEvent(object msg, EventLogEntryType eventType);
  }
}
