using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.CompilerServices;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using CommonExtensions;
using Foundation;
using Foundation.Core;
using Foundation.CustomConfig;
namespace System.Tests {
  // Example of a classes that use attributes and property in order to access appSettings/suctomSection value in [app | web | custom].config
  #region Config Attributes
  class TestSectionAttribute : Foundation.CustomConfig.ConfigSectionAttribute { }
  class MissingSectionAttribute : Foundation.CustomConfig.ConfigSectionAttribute { }
  #endregion
  #region Condif Classes
  [TestSection]
  [Custom2Config]
  sealed class Custom2Config : Config<Custom2Config> {
    class Custom2ConfigAttribute : Foundation.CustomConfig.ConfigFileAttribute { }
    public static string SectionKeyCustomClassLevel { get { return KeyValue(); } }
    [MissingSection]
    public static string SectionKeyCustomClassLevelMissing { get { return KeyValue(); } }
  }
  [CustomConfig]
  class CustomConfig : Config<CustomConfig> {
    public enum TestEnum { Test, QA, Prod };
    class CustomConfigAttribute : Foundation.CustomConfig.ConfigFileAttribute { }
    [ConfigValue]
    public static string CustomKeyCustom { get { return KeyValue(); } }
    [ConfigValue]
    public static string CustomKey { get { return KeyValue(); } }
    [ConfigValue]
    public static int CustomKeyInt { get { return KeyValue<int>(); } }
    [ConfigValue]
    public static TestEnum CustomKeyEnum { get { return KeyValue<TestEnum>(); } }
    [TestSection]
    public static string SectionKey { get { return KeyValue(); } }
    [TestSection]
    public static string SectionKeyCustom { get { return KeyValue(); } }
    [TestSection]
    public static string SectionKeyMethodCustom(string s = null) { return KeyValue(); }
  }
  [TestParentSection]
  class ParentConfig {
    class TestParentSectionAttribute : Foundation.CustomConfig.ConfigSectionAttribute { }
    class ChildConfig : Config<ChildConfig> {
      public static int ParentKeyInt { get { return KeyValue<int>(); } }
    }
    public static int ParentKeyInt { get { return ChildConfig.ParentKeyInt; } }
  }
  [CustomConfig]
  sealed class CustomBadIntConfig : Config<CustomBadIntConfig> {
    class CustomConfigAttribute : Foundation.CustomConfig.ConfigFileAttribute { }
    [ConfigValue]
    public static string CustomKeyCustomMising { get { return KeyValue(); } }
    [ConfigValue]
    public static string CustomKeyCustomDefault { get { return KeyValue(new[] { "default" }); } }
    [ConfigValue]
    public static int CustomKeyIntBad { get { return KeyValue<int>(); } }
    [ConfigValue]
    public static int CustomKeyCustomIntBad { get { return KeyValue<int>(); } }
  }
  [CustomMissingConfig]
  class CustomMissingConfig : Config<CustomMissingConfig> {
    class CustomMissingConfigAttribute : Foundation.CustomConfig.ConfigFileAttribute { }
    public static string CustomKeyCustom { get { return KeyValue(); } }
    public static string CustomKeyCustomWithDefault { get { return KeyValue(new[] { "DEFAULT_VALUE" }); } }
  }
  #endregion
  [TestClass()]
  public class AppConfigTests {
    #region ConfigSection
    [TestMethod()]
    public async Task RunTest_Config() {
      CustomConfig.UnTest();
      var test = await CustomConfig.RunTestAsync(null);
      dynamic trace = ((dynamic)((dynamic)test.First().Value).Trace["CustomKey"]);
      var configFile = trace.configFile;
      Assert.AreEqual(Path.GetFileNameWithoutExtension(configFile), "FoundationTests.dll");
      Assert.AreEqual(trace.sectionName, "appSettings");
      Console.WriteLine(test.ToJson());
    }
    [TestMethod()]
    public void GetSectionKeyValue2() {
      Assert.AreEqual("SectionValueCustomClassLevel", Custom2Config.SectionKeyCustomClassLevel);
    }
    [TestMethod()]
    public void GetSectionKeyValue() {
      Assert.AreEqual("SectionValueCustom", CustomConfig.SectionKeyCustom);
      Assert.AreEqual("SectionValue", CustomConfig.SectionKey);
    }
    [TestMethod()]
    public void MissingSectionKeyValue() {
      //try {
      //  Custom2Config.SectionKeyCustomClassLevelMissing.ToString();
      //} catch (ConfigurationSectionMissingException exc) {
      //  Assert.AreEqual("Custom2.config", Path.GetFileName(exc.Filename));
      //}
      //throw new Exception(typeof(ConfigurationSectionMissingException).Name + " was not thrown.");
    }
    #endregion
    [TestMethod()]
    public void KeyValueWithParent() {
      Assert.AreEqual(49, ParentConfig.ParentKeyInt);
    }
    [TestMethod()]
    public void KeyValueFromCustomConfig() {
      // Get value from Custom.config
      Assert.AreEqual("CustomValueCustom", CustomConfig.CustomKeyCustom);
      // Make sure app.config goes back to default *.config
      Assert.AreEqual("CustomValueApp", CustomConfig.CustomKey);
    }
    [TestMethod()]
    public void GetKeyValueInt() {
      Assert.AreEqual(48, CustomConfig.CustomKeyInt);
    }
    [TestMethod()]
    public void GetKeyValueIntBad() {
      try {
        Assert.AreEqual(48, CustomBadIntConfig.CustomKeyIntBad);
      } catch (ConfigurationErrorException exc) {
        Assert.AreNotEqual("custom.config", Path.GetFileName(exc.FileName).ToLower());
        return;
      }
      throw new Exception();
    }
    [TestMethod()]
    public void GetCustomKeyValueIntBad() {
      try {
        CustomBadIntConfig.CustomKeyCustomIntBad.ToString();
      } catch (Exception exc) {
        var ce = exc.Find<ConfigurationErrorException>();
        Assert.AreEqual("custom.config", Path.GetFileName(ce.FileName).ToLower());
        return;
      }
      throw new Exception();
    }
    [TestMethod()]
    public void GetDefaultKeyValue() {
      Assert.AreEqual("default", CustomBadIntConfig.CustomKeyCustomDefault);
    }
    [TestMethod()]
    public void GetMissingKeyValue() {
      try {
        string.IsNullOrEmpty(CustomBadIntConfig.CustomKeyCustomMising);
      } catch (AggregatedException axc) {
        Assert.AreEqual(2, axc.InnerExceptions.Count());
        Assert.AreEqual("custom.config",
          axc.InnerExceptions.Skip(1).OfType<ConfigurationErrorException>().Select(ce => Path.GetFileName(ce.FileName.ToLower())).Single());
        return;
      }
      throw new Exception(typeof(AggregateException).Name + " was not thrown.");
    }
    [TestMethod()]
    public void KeyValueFromCustomConfigMissing() {
      try {
        string.IsNullOrWhiteSpace(CustomMissingConfig.CustomKeyCustom);
      } catch (ConfigurationFileIsMissingException exc) {
        Assert.AreEqual("CustomMissing.config", Path.GetFileName(exc.FileName));
      }
      Assert.AreEqual("DEFAULT_VALUE", CustomMissingConfig.CustomKeyCustomWithDefault);
    }
    /*
    [TestMethod]
    [CustomSection]
    public void TraceSectionName() {
      Assert.AreEqual("CustomSection", TraceSectionName2());
    }
    string TraceSectionName2() {
      return AppConfig.TraceSectionName().SingleOrDefault();
    }
    string CustomKeyProp {
      get { return AppConfig.GetKeyValue(GetType()); }
    }
    int CustomKeyInt {
      get { return AppConfig.GetKeyValue<int>(GetType()); }
    }
    int CustomKeyIntBad {
      get { return AppConfig.GetKeyValue<int>(GetType()); }
    }
    string CustomKeyPropMissing {
      get { return AppConfig.GetKeyValue(GetType()); }
    }

    string CustomKey() { return AppConfig.GetKeyValue(GetType()); }
    [CustomSection]
    string SectionKey() { return AppConfig.GetKeyValue(GetType()); }
    */
  }
}
