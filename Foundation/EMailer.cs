using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using CommonExtensions;
using Foundation.CustomConfig;
using System.Diagnostics;
using System.IO;

namespace Foundation {
  public abstract class EMailLogger<TLogger> : EventLogger<TLogger> where TLogger : EMailLogger<TLogger>, new() {
    static bool _tracerAdded = false;
    class EmailTraceListener : TracingExtensions.TraceListener2 {
      private MailMessageDelegate _send;
      public EmailTraceListener(MailMessageDelegate send) : this("EMailListener", send) { }
      public EmailTraceListener(string name, MailMessageDelegate send)
        : base(name) {
        this._send = send;
        //this.Filter = new SourceFilter(name);
      }

      protected override string[] GetSupportedAttributes() {
        return new string[0];
      }

      protected override void TraceEventCore(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message) {
        if (IsInTest > 0 && !_isInFullTest) return;
        TryCatchLog(() => _send(new { source, id, eventType } + "", message));
      }

    }
    static void AddListeners() {
      if (!_tracerAdded) {
        AddListener(EventLogSource, name => TraceListenerFactory(new EmailTraceListener(name, InitSend())));
        _tracerAdded = true;
      }
    }
    #region Config Properties
    [ConfigValue]
    protected virtual string EMailFrom { get { return KeyValue(); } }
    [ConfigValue]
    protected virtual string EMailTo { get { return KeyValue(); } }
    #endregion

    public delegate void MailMessageDelegate(string subject, string body, Action<Exception> onError = null);
    readonly static MailMessageDelegate _send = InitSend();
    public static MailMessageDelegate Send { get { return _send; } }

    static MailMessageDelegate InitSend() {
      var from = Instance.EMailFrom;
      var to = Instance.EMailTo;
      return (subject, body, onError) => {
        try {
          Core.EMailer.Send(from, to, subject, body, new Stream[0], onError);
        } catch (Exception exc) {
          throw new Exception(new { from, to, subject } + "", exc);
        }
      };
    }

    protected override void _LogEvent(object msg, EventLogEntryType eventType) {
      AddListeners();
      base._LogEvent(msg, eventType);
    }
    static bool _isInFullTest = false;

    protected override async Task<ExpandoObject> _RunTestAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
      _isInFullTest = true;
      try {
        var b = await base._RunTestAsync((parameters ?? new ExpandoObject()).Merge("runTrace", false), merge);
        LogEvent(GetTestSpace("EMailLogger"), EventLogEntryType.Information);
        return b.Merge("Mail Attributes", new { from = new MailAddress(Core.EMailer.SafeEMailAddress(EMailFrom)), to = new MailAddress(EMailTo) });
      } finally {
        _isInFullTest = false;
      }
    }

    //protected override async Task<ExpandoObject> _RunTestFastAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
    //  return await TestHostAsync(parameters, base._RunTestFastAsync, _LogError, merge);//.Merge(GetTestSpace<TLogger>("EMailLogger"), "Mail sent");
    //}
  }
}
