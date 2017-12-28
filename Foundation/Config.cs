using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using CommonExtensions;
using Foundation.Core;
using System.Dynamic;
using System.Threading;

namespace Foundation.CustomConfig {
  public class ConfigValueAttribute : Attribute { }
  public class ConfigSectionAttribute : ConfigValueAttribute { }
  public class ConfigFileAttribute : ConfigValueAttribute { }
  public class appSettingsAttribute : ConfigSectionAttribute { }
}
namespace Foundation {
  public abstract class Config<TConfig> : Testable<TConfig> where TConfig : Config<TConfig>, new() {

    /// <summary>
    /// Makes custom config a "transparent default" for the func
    /// </summary>
    /// <typeparam name="T">Return Type</typeparam>
    /// <param name="func">Function to execute inside custom config file</param>
    /// <returns></returns>
    public T Using<T>(Func<TConfig, T> func) {
      return Using(() => (TConfig)this, func);
    }

    /// <summary>
    /// Makes custom config a "transparent default" for the func
    /// </summary>
    /// <typeparam name="T">Return Type</typeparam>
    /// <param name="instanceFactory">Object construction factory to be executed using custom config</param>
    /// <param name="func">Function to execute inside custom config file</param>
    /// <returns></returns>
    public static T Using<T>(Func<TConfig> instanceFactory, Func<TConfig, T> func) {
      var configAttrType = typeof(TConfig).GetCustomAttributes<CustomConfig.ConfigFileAttribute>().Select(a => a.GetType()).SingleOrDefault();
      return configAttrType == null ? func(instanceFactory()) : ChangeAppConfig.Using(configAttrType, true, instanceFactory, func);
    }
    /// <summary>
    /// Makes custom config a "transparent default" for the func
    /// </summary>
    /// <typeparam name="T">Return Type</typeparam>
    /// <param name="func">Function to execute inside custom config file (without reference to an instance)</param>
    /// <returns></returns>
    public static T Using<T>(Func<T> func) {
      var configAttrType = typeof(TConfig).GetCustomAttributes<CustomConfig.ConfigFileAttribute>().Select(a => a.GetType()).SingleOrDefault();
      return configAttrType == null ? func() : ChangeAppConfig.Using(configAttrType, func);
    }

    #region Helpers
    #region Circumcise
    private static string Circumcise(Type type, string suffix) {
      return Circumcise(type.Name, suffix);
    }
    private static string Circumcise(string text, string suffix) {
      return Regex.Replace(text, suffix + "$", "", RegexOptions.IgnoreCase);
    }
    #endregion
    static IEnumerable<string> GetCustomAttribute<TAttribute>(bool inherit) where TAttribute : Attribute {
      var myType = typeof(TConfig);
      return new[] { myType }.Concat(myType.DeclaringTypes()).Reverse().SelectMany(t => GetCustomAttribute<TAttribute>(inherit, t)).Take(1);
    }

    private static IEnumerable<string> GetCustomAttribute<TAttribute>(bool inherit, Type myType) where TAttribute : Attribute {
      return (
        from attr in myType.GetCustomAttributes<TAttribute>(inherit)
        select Circumcise(attr.GetType(), "attribute")
        )
        .Take(1);
    }
    #endregion

    #region Get Section
    static IEnumerable<string> GetSectionAttribute(string memberName) {
      return GetMemberSectionAttribute(memberName)
        .Concat(GetSectionAttribute(true))
        .Take(1);
    }
    static IEnumerable<string> GetMemberSectionAttribute(string memberName) {
      var myType = typeof(TConfig);
      return new[] { myType }.Concat(myType.DeclaringTypes()).Reverse().SelectMany(t => GetMemberSectionAttribute(memberName, t)).Take(1);
    }

    private static IEnumerable<string> GetMemberSectionAttribute(string memberName, Type myType) {
      return (
        from m in myType.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        let attrs = m.GetCustomAttributes<CustomConfig.ConfigSectionAttribute>(false)
        from a in attrs
        select Circumcise(a.GetType(), "attribute")
        )
        .Take(1);
    }
    static IEnumerable<string> GetSectionAttribute(bool inherit) {
      return GetCustomAttribute<CustomConfig.ConfigSectionAttribute>(inherit);
    }
    #endregion

    #region Get Config File
    static IEnumerable<string> GetConfigFileAttribute() {
      return GetCustomAttribute<CustomConfig.ConfigFileAttribute>(true).Select(cf => Circumcise(cf, "config"));
    }
    #endregion

    public static string KeyValue(string[] fallBack = null, [CallerMemberName]string keyName = "") {
      return KeyValue<string>(fallBack, keyName);
    }
    /// <summary>
    /// Locate key value in current .config.
    /// If absent locate in Fallback .config 
    /// </summary>
    /// <param name="keyName"></param>
    /// <param name="getSection">Config section factory</param>
    /// <returns></returns>
    public static T KeyValue<T>(T[] fallBack = null, [CallerMemberName]string keyName = "") {
      var safeFallback = fallBack ?? new T[] { };
      var sectionName = GetSectionAttribute(keyName).DefaultIfEmpty("appSettings").Single();

      Func<string> errorMessage = () => new { sectionName, keyName } + "";
      var exceptions = new List<Exception>();
      var throw_ = Helpers.ToFunc<T[]>(() => { throw new AggregatedException(exceptions); });
      Func<Func<string>, NameValueCollection> getSection = cf => (NameValueCollection)ConfigurationManager.GetSection(sectionName);
      Func<string> configFile = () => GetConfigFileAttribute().Select(cf => cf + ".config").SingleOrDefault();
      Configer.OnMissing throwMissingFile = (customConfig, sn) => { if (safeFallback.IsEmpty())  throw new ConfigurationFileIsMissingException(sectionName, keyName, customConfig); };
      Configer.OnMissing throwMissingSection = (customConfig, sn) => { throw new ConfigurationErrorException("Section is missing", sectionName, keyName, customConfig); };
      Func<Func<string>, NameValueCollection> getSectionCustom = getConfigFile => Configer.GetSection(getConfigFile(), sectionName, throwMissingFile, throwMissingSection);
      var get = Helpers.ToFunc(getSection, configFile, (nvc, cf) => {
        var section = nvc(cf);
        if (section == null) {
          exceptions.Add(new ConfigurationErrorException("Missing Section", sectionName, keyName, cf()));
          return new T[0];
        }
        var s = section == null ? "" : section[keyName];
        if (s == null) {
          exceptions.Add(new ConfigurationErrorException("Missing Key", sectionName, keyName, cf()));
          return new T[0];
        }
        try {
          var v = string.IsNullOrWhiteSpace(s) ? default(T) : (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(s);
          if (_isInTest) _keyTrace[keyName] = new { sectionName, configFile = cf() }.ToExpando();
          return new[] { v };
        } catch (ConfigurationFileIsMissingException exc) {
          throw new AggregateException(exc);
        } catch (Exception exc) {
          throw new ConfigurationErrorException(keyName, sectionName, cf(), exc);
        }
      });
      var foos = new Func<T[]>[] { () => get(getSection, () => Configer.ConfigurationFile) }
        .Concat(new Func<T[]>[] { () => get(getSectionCustom, configFile) }.Where(_ => !string.IsNullOrWhiteSpace(configFile())))
        .Concat(new Func<T[]>[] { () => safeFallback });
      return foos.Concat(new[] { throw_ })
        .SelectMany(f => f())
        //.SkipWhile(s => s.IsEmpty())
        //.Select(s => s.Single())
        .First();
    }

    #region Context switching
    /// <summary>
    /// Execute some code in configuration context switched to provided configFile
    /// </summary>
    #endregion

    protected override async Task<ExpandoObject> _RunTestFastAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
      return await TestHostAsync(parameters, (p, m) => RunTestImpl(p, m), null, merge);
    }
    static SemaphoreSlim _lockKeyTrace = new SemaphoreSlim(initialCount: 1);
    static Dictionary<string, ExpandoObject> _keyTrace = new Dictionary<string, ExpandoObject>();
    static bool _isInTest;
    private async Task<ExpandoObject> RunTestImpl(ExpandoObject parametrs, params Func<ExpandoObject, ExpandoObject>[] merge) {
      //await _lockKeyTrace.WaitAsync();
      Func<object, object> converter = o => o!= null && o.GetType().IsEnum ? o + "" : o;
      try {
        _keyTrace.Clear();
        _isInTest = true;
        var e = (await base._RunTestFastAsync(null, merge)).Merge("_defaultPath", Configer.GetConfigDirectory());
        typeof(TConfig).GetMethods().Where(m => m.GetCustomAttributes<CustomConfig.ConfigValueAttribute>().Any())
          .Select(m => new { m.Name, Invoke = new Func<object>(() => m.Invoke(this, new object[m.GetParameters().Length])) })
          .Concat(Reflection.GetPropertiesWithAttribute<TConfig, CustomConfig.ConfigValueAttribute>(true)
          .Select(p => new { p.Name, Invoke = new Func<object>(() => p.GetValue(this)) }))
          .Where(p => !p.Name.ToLower().Contains("password"))
          .OrderBy(p => p.Name)
          .ForEach(p => e.AddOrUpdate(p.Name, converter(p.Invoke())));
        return new ExpandoObject().Merge(GetTestSpace("Config"), e.Merge("Trace", _keyTrace));
      } finally {
        _isInTest = false;
      }
    }
  }
}
