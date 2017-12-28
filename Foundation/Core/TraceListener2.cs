using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foundation.Core {
  public abstract class TraceListener2 : TraceListener {
    protected TraceListener2(string name) : base(name) { }

    protected abstract void TraceEventCore(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message);
    protected virtual string FormatData(object[] data) {
      return string.Join(Environment.NewLine, TraceParams.CleanTraceData(data)
        .Select(d => d + "")
        .Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    protected void TraceDataCore(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data) {
      if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
        return;
      TraceEventCore(eventCache, source, eventType, id, FormatData(data));
    }
    public sealed override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message) {
      if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
        return;
      TraceEventCore(eventCache, source, eventType, id, message);
    }
    public sealed override void Write(string message) {
      if (Filter != null && !Filter.ShouldTrace(null, "Trace", TraceEventType.Information, 0, message, null, null, null))
        return;
      TraceEventCore(null, "Trace", TraceEventType.Information, 0, message);
    }

    public sealed override int GetHashCode() {
      return base.GetHashCode();
    }
    public sealed override string Name {
      get {
        return base.Name;
      }
      set {
        base.Name = value;
      }
    }

    #region Other Members
    protected void TraceTransferCore(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId) {
      base.TraceTransfer(eventCache, source, id, message, relatedActivityId);
    }
    public sealed override void WriteLine(string message) {
      Write(message);
    }
    public sealed override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args) {
      TraceEventCore(eventCache, source, eventType, id, string.Format(format, args));
    }
    public sealed override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id) {
      TraceEventCore(eventCache, source, eventType, id, null); //TODO: is null valid?
    }
    public sealed override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data) {
      TraceDataCore(eventCache, source, eventType, id, data);
    }
    public sealed override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data) {
      TraceDataCore(eventCache, source, eventType, id, data);
    }
    public sealed override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId) {
      TraceTransferCore(eventCache, source, id, message, relatedActivityId);
    }
    public sealed override bool Equals(object obj) {
      return this == obj;
    }
    public sealed override object InitializeLifetimeService() {
      return base.InitializeLifetimeService();
    }
    public sealed override string ToString() {
      return base.ToString();
    }
    public sealed override System.Runtime.Remoting.ObjRef CreateObjRef(Type requestedType) {
      return base.CreateObjRef(requestedType);
    }
    public sealed override void Write(object o) {
      base.Write(o);
    }
    public sealed override void Write(object o, string category) {
      base.Write(o, category);
    }
    public sealed override void Write(string message, string category) {
      base.Write(message, category);
    }
    protected sealed override void WriteIndent() {
      base.WriteIndent();
    }
    public sealed override void WriteLine(object o) {
      base.WriteLine(o);
    }
    public sealed override void WriteLine(object o, string category) {
      base.WriteLine(o, category);
    }
    public sealed override void WriteLine(string message, string category) {
      base.WriteLine(message, category);
    }
    public sealed override void Fail(string message) {
      base.Fail(message);
    }
    public sealed override void Fail(string message, string detailMessage) {
      base.Fail(message, detailMessage);
    }
    public sealed override void Close() {
      Dispose(true);
    }
    #endregion
  }
}
