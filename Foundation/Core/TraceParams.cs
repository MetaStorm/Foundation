using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CommonExtensions;

namespace Foundation.Core {
  public class TraceParams {
    public static object[] CleanTraceData(object[] data) {
      data = data.Where(d => !(d is TraceParams)).ToArray();
      return data;
    }

    public static TraceParams Empty = new TraceParams();
    public ExpandoObject Value { get; set; }
    IDictionary<string, object> Dictionary { get { return (IDictionary<string, object>)Value; } }
    public TraceParams() : this(new ExpandoObject()) { }
    public TraceParams(ExpandoObject expando) {
      this.Value = expando;
    }
    public TraceParams(params object[] properties) {
      Value = new ExpandoObject();
      properties.Buffer(2)
        .Do(b => { if (b.Count == 1)throw new InvalidOperationException("Properties parameter must be an even length array"); })
        .ForEach(b => Dictionary.Add(b[0] + "", b[1]));
    }
    public T GetPropertyOrDefault<T>(string name, T def) {
      return GetPropertyOrDefault(name, () => def);
    }
    public IEnumerable<T> GetPropertyOrDefault<T>(string[] name, Func<T> getDefault) {
      foreach (var n in name)
        yield return GetPropertyOrDefault(n, getDefault);
    }
    public T GetPropertyOrDefault<T>(string name, Func<T> getDefault) {
      return Dictionary.ContainsKey(name) ? (T)Dictionary[name] : getDefault();
    }
    public void SetProperty<T>(string name, T value) {
      Dictionary.Add(name, value);
    }
    public override string ToString() {
      return Value.ToJson();
    }
  }
}
