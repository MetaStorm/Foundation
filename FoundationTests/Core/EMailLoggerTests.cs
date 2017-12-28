using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
namespace Foundation.Core.Tests {
  [TestClass()]
  public class EMailLoggerTests {
    [TestMethod()]
    public void SafeEMailAddress() {
      Assert.AreEqual("email@address", EMailer.SafeEMailAddress("email@address"));
    }
    [TestMethod()]
    public void SendTest() {
      var emails = EMailer.GetEmail(System.Security.Principal.WindowsIdentity.GetCurrent().Name);
      Assert.IsTrue(emails.Any());
      Assert.IsTrue(EMailer.Send(emails[0], emails[0], "Subject", "Body"));
    }
  }
}
