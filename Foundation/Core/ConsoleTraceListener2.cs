using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foundation.Core {
  class ConsoleTraceListener2 : System.Diagnostics.ConsoleTraceListener {
    public override void TraceData(System.Diagnostics.TraceEventCache eventCache, string source, System.Diagnostics.TraceEventType eventType, int id, params object[] data) {
      base.TraceData(eventCache, source, eventType, id, TraceParams.CleanTraceData(data));
    }
  }
}
