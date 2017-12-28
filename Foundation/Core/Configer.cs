using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CommonExtensions;

namespace Foundation.Core {
  public static class Configer {
    public static string ConfigurationFile { get { return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile; } }
    public delegate void OnMissing(string configFile, string sectionName);
    /// <summary>
    /// Get section from custom config file
    /// </summary>
    /// <param name="fileName">Name of config file located in executed folder</param>
    /// <param name="sectionName">appSettings or other (case sensitive)</param>
    /// <param name="onConfigMissing"></param>
    /// <param name="onSectionMissing"></param>
    /// <returns></returns>
    public static NameValueCollection GetSection(string fileName, string sectionName, OnMissing onConfigMissing, OnMissing onSectionMissing) {
      try {
        var filePath = Helpers.WebServerPath(fileName) ?? Helpers.GetExecPath(fileName);
        Configuration config = OpenExeConfiguration(filePath);
        if (!File.Exists(config.FilePath)) throw new ConfigurationErrorsException("File not found", config.FilePath, 0);
        var section = config.GetSection(sectionName);
        if (section == null)
          if (onSectionMissing == null) throw new Exception("Session is missing");
          else {
            onSectionMissing(config.FilePath, sectionName);
            return new NameValueCollection();
          }
        if (config.AppSettings == config.GetSection(sectionName)) return config.AppSettings.GetNameValues();
        var sectionInformation = section.SectionInformation;
        var xml = sectionInformation.GetRawXml();
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        IConfigurationSectionHandler handler = (IConfigurationSectionHandler)typeof(System.Configuration.NameValueSectionHandler).GetConstructor(new Type[0]).Invoke(new object[0]);
        return (NameValueCollection)handler.Create(null, null, doc.DocumentElement);
      } catch (ConfigurationErrorsException exc) {
        if (onConfigMissing == null) throw;
        onConfigMissing(exc.Filename, sectionName);
        return new NameValueCollection();
      }
    }

    public static string GetConfigDirectory() { return Path.GetDirectoryName(Configer.OpenExeConfiguration("Some.config").FilePath) + "\\"; }
    public static Configuration OpenExeConfiguration(string fileName) {
      return ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = fileName }, ConfigurationUserLevel.None);
    }
    static NameValueCollection GetNameValues(this AppSettingsSection appSettings) {
      var nvc = new NameValueCollection();
      appSettings.Settings.OfType<KeyValueConfigurationElement>().ForEach(kv => nvc.Add(kv.Key, kv.Value));
      return nvc;
    }
    /// <summary>
    /// Example of loading file by assembley name
    /// </summary>
    /// <returns></returns>
    static Configuration OpenExeConfiguration() {
      string assemblyFolder;
      if (System.Web.HttpContext.Current != null) {
        assemblyFolder = Assembly.GetExecutingAssembly().CodeBase;
        assemblyFolder = new Uri(assemblyFolder).LocalPath;
      } else {
        assemblyFolder = Assembly.GetExecutingAssembly().Location;
      }
      return ConfigurationManager.OpenExeConfiguration(assemblyFolder);
    }
  }
  [Serializable]
  public class ConfigurationErrorException : Exception {
    public string FileName { get; set; }
    public string Section { get; set; }
    public string Key { get; set; }
    public ConfigurationErrorException(string key, string section, string configFIle, Exception exc)
      : base(exc.Message, exc) {
      this.FileName = configFIle;
      this.Section = section;
      this.Key = key;
    }
    public ConfigurationErrorException(string message, string section, string key, string configFIle)
      : base(message) {
      this.FileName = configFIle;
      this.Section = section;
      this.Key = key;
    }
    protected ConfigurationErrorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    public override string Message {
      get {
        return new { Section, Key, Error = base.Message, FileName } + "";
      }
    }
  }
  [Serializable]
  public class ConfigurationFileIsMissingException : ConfigurationErrorException {
    public ConfigurationFileIsMissingException(string section, string key, string path)
      : base("Config file is missing", section, key, path) {
    }
    protected ConfigurationFileIsMissingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
  }
}
