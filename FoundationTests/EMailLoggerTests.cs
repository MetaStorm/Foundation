using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommonExtensions;
using System.Diagnostics;
using System.IO;

namespace Foundation.Tests {
  [TestClass()]
  public class EMailLoggerTests {
    [MailTestSettings]
    class MailTest : EMailLogger<MailTest> {
      class MailTestSettingsAttribute : Foundation.CustomConfig.ConfigSectionAttribute { }
    }
    [MailTestSettings]
    class MailTestFast : EMailLogger<MailTestFast> {
      class MailTestSettingsAttribute : Foundation.CustomConfig.ConfigSectionAttribute { }
    }
    [MailTestMissingFromSettings]
    class MailTestMissingFrom : EMailLogger<MailTestMissingFrom> {
      class MailTestMissingFromSettingsAttribute : Foundation.CustomConfig.ConfigSectionAttribute { }

    }

    [TestMethod()]
    public async Task EmailLogger_RunTestFast() {
      Console.WriteLine((await MailTestFast.RunTestFastAsync(null)).ToJson());
    }
    [TestMethod()]
    public async Task EmailLogger_RunTest() {
      Console.WriteLine((await MailTest.RunTestAsync(null)).ToJson());
    }
    [TestMethod()]
    public async Task EmailLoggerMissingFrom_RunTest() {
      Console.WriteLine((await MailTestMissingFrom.RunTestAsync(null)).ToJson());
    }
    [TestMethod()]
    public void EmailLogger_LogEvent() {
      MailTest.LogEvent("EmailLogger_LogError", EventLogEntryType.Information);
    }
    static string EMAIL_HEADER = @"
<link href='https://maxcdn.bootstrapcdn.com/bootstrap/3.3.6/css/bootstrap.min.css' rel='stylesheet'>
<div style='white-space: pre-line' class='container'>
  <table cellpadding='0' style='border-collapse: collapse;'>
    <tr>
      <td style='width:1px'><img src='cid:{0}' /></td>
      <td style='width: 100%;height:78px'><img src='cid:{1}' style='width: 100%; height: 78px;max-height: 78px;' /></td>
      <td style = 'width:1px' ><img src='cid:{2}' /></td>
      <td></td>
    </tr>
  </table>
  <br />
  <br />
  <p>";

    [TestMethod]
    public void SendEmailImage() {
      Foundation.Core.EMailer.Send("Dimokdimon@gmail.com", "Dimokdimon@gmail.com", "Test logo"
        , EMAIL_HEADER+ "<br/>Some body</p>", new[] { "Header_Itau_Left.png", "Header_Itau_Pixel2.png","Header_Itau_Right.png" });
    }

  }
}
