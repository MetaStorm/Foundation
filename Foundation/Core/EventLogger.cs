using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions;

namespace Foundation.Core {
  public static class EventLogger {
    static Func<object, object> Format = CommonExceptionExtentions.Format;

    public static void LogApplicationError(object error, int eventID = 0) { LogApplication(error, eventID, EventLogEntryType.Error); }
    public static void LogApplication(object error, int eventID = 0, EventLogEntryType type = EventLogEntryType.Error) {
      LogMe(error, "Application", EventLogEntryType.Error, eventID);
    }
    public static void LogMe(object error, string eventSource, EventLogEntryType eventType, int eventID) {
      LogMe(Format(error) + "", eventSource, eventType, eventID);
    }
    public static void LogMe(string eventMessage, string eventSource, EventLogEntryType eventType, int eventID, string machineName = ".") {
      //if (Debugger.IsAttached) Debugger.Break();
      eventMessage = TrancateMessage(eventMessage, 31839);
      using (var el = EventLogFactory(eventSource, machineName))
        el.WriteEntry(eventMessage, eventType, eventID);
    }
    public static void LogMe(string eventMessage, string eventSource, EventLogEntryType eventType, int eventID,short category, string machineName = ".") {
      //if (Debugger.IsAttached) Debugger.Break();
      eventMessage = TrancateMessage(eventMessage, 31839);
      using (var el = EventLogFactory(eventSource, machineName))
        el.WriteEntry(eventMessage, eventType, eventID, category);
    }

    public static string TrancateMessage(string eventMessage,int maxSize) {
      var tranceted = "\n... trancated";
      if (eventMessage.Length > maxSize) {
        return eventMessage.Substring(0, maxSize - tranceted.Length) + tranceted;
      }
      return eventMessage;
    }

    public static IList<EventLogEntry> Read(string eventSource, EventLogEntryType entryType, int eventId, string machineName = ".") {
      return Read(DateTime.MinValue, eventSource, entryType, eventId, machineName);
    }
    public static IList<EventLogEntry> Read(DateTime timeGenerated, string eventSource, EventLogEntryType entryType, int eventId, string machineName = ".") {
      using (var el = EventLogFactory(eventSource, machineName))
        return el.Entries.Cast<EventLogEntry>()
          .Where(entry =>
            entry.TimeGenerated > timeGenerated &&
            entry.Source == eventSource &&
            entry.EntryType == entryType &&
            entry.InstanceId == eventId
          ).ToArray();
    }
    public static EventLogEntry[] ReadLast(string eventSource, EventLogEntryType entryType, int eventId, string machineName = ".") {
      using (var el = EventLogFactory(eventSource, machineName))
        return new[]{
          el.Entries.Cast<EventLogEntry>()
          .LastOrDefault(entry =>
            entry.Source == eventSource &&
            entry.EntryType == entryType &&
            entry.InstanceId == eventId
          )}
          .Where(e => e != null)
          .ToArray();
    }
    public static int ReadLastIndex(string eventSource, int eventLogEventId, EventLogEntryType entryType, string machineName = ".") {
      return ReadLast(eventSource, entryType, eventLogEventId, machineName).Select(e => e.Index).LastOrDefault();
    }
    public static string[] ReadContent(int startIndex, string eventSource, int eventLogEventId, EventLogEntryType entryType, string machineName = ".") {
      using (var el = EventLogFactory(eventSource, machineName))
        return el.Entries.Cast<EventLogEntry>()
          .Where(entry =>
            entry.Index > startIndex &&
            entry.Source == eventSource &&
            entry.EntryType == entryType &&
            entry.InstanceId == eventLogEventId
          )
        .SelectMany(e => e.ReplacementStrings)
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .ToArray();
    }
    public static bool RegisterSource(string eventSource, string logFileName, string machineName, bool dontThrow = false) {
      machineName = SafeMachineName(machineName);
      if (string.IsNullOrWhiteSpace(eventSource))
        throw new ArgumentException("EventSource must not be an empty string.");
      if (string.IsNullOrWhiteSpace(logFileName))
        throw new ArgumentException("LogFile must not be an empty string.");
      var logFileNameCurr = GetLogFileBySourceName(eventSource, machineName).ToLower();
      if (logFileNameCurr == logFileName.ToLower()) return false;
      if (!string.IsNullOrWhiteSpace(logFileNameCurr))
        EventLog.DeleteEventSource(eventSource, machineName);
      var esd = new EventSourceCreationData(eventSource, logFileName) { MachineName = machineName };
      EventLog.CreateEventSource(esd);
      if (!dontThrow)
        throw new NewEventSourceException(eventSource, logFileName);
      return true;
    }

    static EventLog EventLogFactory(string eventSource, string machineName) {
      try {
        return new EventLog(sureLogFile("", eventSource, machineName), SafeMachineName(machineName), eventSource);
      } catch (Exception exc) {
        throw new Exception(new { eventSource } + "", exc);
      }
    }

    private static string sureLogFile(string logFile, string eventSource, string machineName) {
      return string.IsNullOrWhiteSpace(logFile) ? GetLogFileBySourceName(eventSource, SafeMachineName(machineName)) : logFile;
    }
    private static string GetLogFileBySourceName(string eventSource, string machineName) {
      try {
        return EventLog.LogNameFromSourceName(eventSource, SafeMachineName(machineName));
      } catch (Exception exc) {
        var exc2 = new Exception(new { eventSource, machineName } + "", exc);
        LogApplicationError(exc2);
        throw exc2;
      }
    }

    private static string SafeMachineName(string machineName) {
      return string.IsNullOrWhiteSpace(machineName) ? "." : machineName;
    }
  }

  public class NewEventSourceException : ApplicationException {
    public string EventSource { get; set; }
    public string LogFile { get; set; }
    public override string Message {
      get {
        return string.Format(@"Event Source:[{0}] was created in {1} log file.
Application must restart in order to use it.
", EventSource, LogFile);
      }
    }
    public NewEventSourceException(string eventSource, string logFile) {
      this.EventSource = eventSource;
      this.LogFile = LogFile;
    }
  }
}
