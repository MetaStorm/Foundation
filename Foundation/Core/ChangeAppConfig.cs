using CommonExtensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Foundation.Core {
  #region ChangeAppConfig
  public class ChangeAppConfig : IDisposable {
    private readonly string oldConfig =
        AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE").ToString();

    private bool disposedValue;

    public ChangeAppConfig() {

    }
    public ChangeAppConfig(string path, Action<string> fileMissing) {
      if (!File.Exists(path)) fileMissing(path);
      AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", path);
      ResetConfigMechanism();
    }

    public void Dispose() {
      if (!disposedValue) {
        AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", oldConfig);
        ResetConfigMechanism();


        disposedValue = true;
      }
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configAttributeType"></param>
    /// <param name="func"></param>
    /// <returns></returns>
    public static T Using<T>(Type configAttributeType, Func<T> func) {
      return Using(configAttributeType, true, func);
    }
    /// <summary>
    /// Swithc contecxt of func to custom config file
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configAttributeType"></param>
    /// <param name="useExecPath">default id true</param>
    /// <param name="func"></param>
    /// <returns></returns>
    public static T Using<T>(Type configAttributeType, bool useExecPath, Func<T> func) {
      return Using(configAttributeType, useExecPath, null, func);
    }
    public static T Using<T>(Type configAttributeType, bool useExecPath,Action<string>fileMissing, Func<T> func) {
      return Using( configAttributeType.CircumciseConfigAttribute(), useExecPath, func);
    }
    public static T Using<T>(string configFile, bool useExecPath, Func<T> func) {
      return Using(configFile, useExecPath, null, func);
    }
    static T Using<T>(string configFile, bool useExecPath, Action<string> fileMissing, Func<T> func) {
      if (Path.GetExtension(configFile).ToLower() != ".config")
        configFile += ".config";
      using (Change(configFile, useExecPath, fileMissing))
        return func();
    }
    public static void Using(string configFile, bool useExecPath, Action action) {
      if (Path.GetExtension(configFile).ToLower() != ".config")
        configFile += ".config";
      using (Change(configFile, useExecPath, null))
        action();
    }

    public static T Using<U, T>(Type configAttributeType,  Func<U> instanceFuctory, Func<U, T> func) {
      return Using(configAttributeType, true, instanceFuctory, func);
    }
    public static T Using<U, T>(Type configAttributeType, bool useExecPath, Func<U> instanceFactory, Func<U, T> func) {
      return Using(configAttributeType, useExecPath, null, instanceFactory, func);
    }
    public static T Using<U, T>(Type configAttributeType, bool useExecPath, Action<string> fileMissing, Func<U> instanceFuctory, Func<U, T> func) {
      return Using(configAttributeType.CircumciseConfigAttribute(), useExecPath, fileMissing, instanceFuctory, func);
    }
    static T Using<U, T>(string configFile, bool useExecPath, Action<string> fileMissing, Func<U> instanceFuctory, Func<U, T> func) {
      if (Path.GetExtension(configFile).ToLower() != ".config")
        configFile += ".config";
      using (Change(configFile, useExecPath, fileMissing))
        return func(instanceFuctory());
    }

    /// <summary>
    /// Switch configuration context to provided configFile
    /// </summary>
    /// <param name="path">path to .config file</param>
    /// <param name="useExecPath">Use current executable path to locate .config file</param>
    /// <returns></returns>
    static ChangeAppConfig Change(string path, bool useExecPath, Action<string> fileMissing) {
      var getPath = (from context in new[] { HttpContext.Current }
                     where context != null
                     select Path.Combine(context.Server.MapPath("~"), path)
                  )
                  .DefaultIfEmpty(Helpers.GetExecPath(path));
      return new ChangeAppConfig(Path.IsPathRooted(path) || !useExecPath ? path : getPath.Single(), fileMissing);
    }

    private static void ResetConfigMechanism() {
      typeof(ConfigurationManager)
          .GetField("s_initState", BindingFlags.NonPublic |
                                   BindingFlags.Static)
          .SetValue(null, 0);

      typeof(ConfigurationManager)
          .GetField("s_configSystem", BindingFlags.NonPublic |
                                      BindingFlags.Static)
          .SetValue(null, null);

      typeof(ConfigurationManager)
          .Assembly.GetTypes()
          .Where(x => x.FullName ==
                      "System.Configuration.ClientConfigPaths")
          .First()
          .GetField("s_current", BindingFlags.NonPublic |
                                 BindingFlags.Static)
          .SetValue(null, null);
    }
  }
  #endregion
}
