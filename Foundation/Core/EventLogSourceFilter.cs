using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foundation.Core {
  public class EventLogSourceFilter : TraceFilter {
    static string EVENT_SOURCE_PROP = "eventSource";
    static string[] EVENT_SOURCE_PROPS = new[] { EVENT_SOURCE_PROP, "{" + EVENT_SOURCE_PROP + "}" };
    string[] GetEventLogSource(object v) {
      return new[] { v }.OfType<TraceParams>()
        .Select(tp => tp.GetPropertyOrDefault(EVENT_SOURCE_PROPS, () => ""))
        .SelectMany(s => s)
        .Where(s => !string.IsNullOrEmpty(s))
        .ToArray();
    }
    override public bool ShouldTrace(
      TraceEventCache cache,
      string traceSource,
      TraceEventType eventType,
      int id,
      string formatOrMessage,
      object[] args,
      object data,
      object[] dataArray) {
      return
        GetEventLogSource(data)
        .Concat((dataArray ?? new object[0]).SelectMany(d => GetEventLogSource(d)))
        .Any(es => es == _eventLogSource);
        //.Any(es => eventType <= TraceEventType.Information && es == _eventLogSource);
    }
    string _eventLogSource;
    public EventLogSourceFilter(string eventLogSource) {
      _eventLogSource = eventLogSource;
    }
  }
}
