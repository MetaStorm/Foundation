using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation.Core.Microsoft;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Foundation.Core.Microsoft.Tests {
  [TestClass()]
  public class Traceable {
    [TestMethod()]
    public void EventLogTraceListener2() {
      new EventLogTraceListener2("dimok").TraceData(null, "dimon", System.Diagnostics.TraceEventType.Critical, 49, "Missing event source test");
    }
  }
}
