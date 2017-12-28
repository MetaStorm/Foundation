using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Foundation.Tests {
  [EMailSettings]
  class EMailLoggerTest : EMailLogger<EMailLoggerTest> {
    class EMailSettingsAttribute : Foundation.CustomConfig.ConfigSectionAttribute { }
  }
  [TestClass()]
  public class EMailerTests {
    [TestMethod()]
    public void EMailTest() {
      EMailLoggerTest.RunTest();
      EMailLoggerTest.RunTestFast();
    }
  }
}
