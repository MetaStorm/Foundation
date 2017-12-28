using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Foundation.Tests {
  [TestClass()]
  public class EventLogSourceFilterTests {
    [TestMethod()]
    public void ShouldTrace() {
      var traceParamsYes = new Core.TraceParams("eventSource", "dimokSource");
      var traceParamsNo = new Core.TraceParams("eventSource", "dimonSource");
      var elsf = new Core.EventLogSourceFilter("dimokSource");

      Assert.IsTrue(elsf.ShouldTrace(null, "traceSource", System.Diagnostics.TraceEventType.Critical, 0, null, null, traceParamsYes, null));
      Assert.IsFalse(elsf.ShouldTrace(null, "traceSource", System.Diagnostics.TraceEventType.Critical, 0, null, null, traceParamsNo, null));

      Assert.IsTrue(elsf.ShouldTrace(null, "traceSource", System.Diagnostics.TraceEventType.Critical, 0, null, null, null, new[] { null, traceParamsYes }));
      Assert.IsFalse(elsf.ShouldTrace(null, "traceSource", System.Diagnostics.TraceEventType.Critical, 0, null, null, null, new[] { null, traceParamsNo }));
    }

  }
}
