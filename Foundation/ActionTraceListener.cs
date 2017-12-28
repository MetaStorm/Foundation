using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foundation {
  public delegate void ActionTraceListenerDelegate(string source, TraceEventType eventType, int id, string message);
  public class ActionTraceListener : Core.TraceListener2 {
    private ActionTraceListenerDelegate _traceAction;
    public ActionTraceListener(string name,ActionTraceListenerDelegate traceAction) : base(name) {
      _traceAction = traceAction;
    }
    protected override void TraceEventCore( TraceEventCache eventCache,string source, TraceEventType eventType, int id, string message) {
      _traceAction(source, eventType, id, message);
    }
  }
}
