using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Foundation {
  /// <summary>
  /// Base class to use for Voice/Sms messaging
  /// Override _sendVoice and _sendSms methods to inplement provider specific functionality
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public abstract class Messenger<T>: LoggableComponent<T> where T : Messenger<T>, new() {
    [Foundation.CustomConfig.ConfigValue]
    public static bool IngoreCertificateErrors { get { return KeyValue<bool>(); } }
    public delegate bool SendVoiceDelegate(string destination, string text,Action<ExpandoObject> complete);
    public static SendVoiceDelegate SendVoice { get { return new T()._sendVoice; } }
    protected abstract bool _sendVoice(string destination, string text, Action<ExpandoObject> complete);

    public delegate bool SendSmsDelegate(string destination, string text, Action<ExpandoObject> complete);
    public static SendSmsDelegate SendSms { get { return new T()._sendSms; } }
    protected abstract bool _sendSms(string destination, string text, Action<ExpandoObject> complete);

    public delegate bool SendMessageDelegate(string destination, string text, Action<ExpandoObject> complete);
    public static SendMessageDelegate SendMessage { get { return new T()._sendMessage; } }
    protected abstract bool _sendMessage(string destination, string text, Action<ExpandoObject> complete);

    static Messenger() {
      if(IngoreCertificateErrors)
        ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
    }
  }
}
