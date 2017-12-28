using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;
namespace Foundation.Core.Tests {
  [TestClass()]
  public class AppConfigTests {
    [TestMethod()]
    public void LoadCustomConfigTest() {
      var section = Core.Configer.GetSection("custom.config", "TestSection",null,null);
      Assert.AreEqual("SectionValueInCustom",section["SectionKey"]);
    }
    public class CustomConfigAttribute : Foundation.CustomConfig.ConfigFileAttribute { }
    [TestSection]
    [CustomConfig]
    class CustomConfig : Config<CustomConfig> {
      public class TestSectionAttribute : Foundation.CustomConfig.ConfigSectionAttribute{ }
      public static string SectionKey { get { return KeyValue(); } }
      [Foundation.CustomConfig.appSettings]
      public static string CustomKey { get { return KeyValue(); } }
    }
    [TestMethod()]
    public void UsingSwitchConfigContextTest() {
      var value2 = ChangeAppConfig.Using(typeof(CustomConfigAttribute), true, () => CustomConfig.SectionKey);
      var value3 = CustomConfig.Using(() => CustomConfig.SectionKey);
      Assert.AreEqual(value3, value2);
      Assert.AreEqual("CustomValueApp", CustomConfig.CustomKey);
    }
  }
}
