using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foundation.Core {
  class EventLogTraceListener2 : Foundation.Core.Microsoft.EventLogTraceListener2 {
    public EventLogTraceListener2(string name) : base(name) { }
    public override void TraceData(TraceEventCache eventCache, string source, TraceEventType severity, int id, params object[] data) {
      if (Filter != null && !Filter.ShouldTrace(eventCache, source, severity, id, null, null, null, data))
        return;
      data = TraceParams.CleanTraceData(data);
      if (data == null || !data.Any()) return;
      EventInstance inst = CreateEventInstance(severity, id);
      WriteEventSafe(id, inst, string.Join(", ", data));
    }
  }
}
