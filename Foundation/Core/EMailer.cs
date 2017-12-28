using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.DirectoryServices.AccountManagement;
using CommonExtensions;
using System.ServiceModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Foundation.Core {
  public static class EMailer {
    static IEnumerable<string> FindExchange() {
      DirectoryEntry RootDSE = new DirectoryEntry(@"LDAP://RootDSE");
      string baseStr = @"LDAP://" + RootDSE.Properties["configurationNamingContext"].Value;

      DirectorySearcher searcher = new DirectorySearcher(new DirectoryEntry(baseStr));

      string classObj = "(&(objectClass=msExchExchangeServer)(msExchCurrentServerRoles:1.2.840.113556.1.4.803:=2))";
      searcher.Filter = string.Format("({0})", classObj);

      searcher.PropertiesToLoad.Add("name");
      searcher.PageSize = 1000;
      searcher.ServerTimeLimit = new TimeSpan(0, 1, 0);
      searcher.CacheResults = false;
      searcher.PropertiesToLoad.Add("cn");

      SearchResultCollection coll = searcher.FindAll();

      var res = coll.Cast<SearchResult>();
      Func<SearchResult, string> cn = r => {
        try { return r.Properties["cn"][0] + ""; } catch { return ""; }
      };
      return res.Select(cn).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
    }
    static IEnumerable<string> MailServers(string firstToTry = null) {
      if (!string.IsNullOrWhiteSpace(firstToTry))
        yield return firstToTry;
      foreach (var s in ExchangeServers)
        yield return s;
    }
    static IEnumerable<string> GetExchangeServers() {
      foreach (var server in FindExchange())
        yield return server;
    }

    static IList<string> ExchangeServers { get { return GetExchangeServers().ToArray(); } }

    public static bool Send(string from, string to, string subject, string body, IEnumerable<Stream> image = null, Action<Exception> onError = null) {
      return Send("", from, to, subject, body, image, onError);
    }
    public static bool Send(string from, string to, string subject, string body, IEnumerable<string> logoPath, Action<Exception> onError = null) {
      return Send("", from, to, subject, body, logoPath, onError);
    }
    public static bool Send(string mailSrver, string from, string to, string subject, string body, IEnumerable<Stream> image = null, Action<Exception> onError = null) {
      return SendMail(MailServers(mailSrver), from, to, subject, body, image ?? new Stream[0], onError);
    }
    public static bool Send(string mailSrver, string from, string to, string subject, string body, IEnumerable<string> logoPath, Action<Exception> onError = null) {
      return SendMail(MailServers(mailSrver), from, to, subject, body, logoPath.Select(lp => File.OpenRead(Helpers.EmailLogo(lp))), onError);
    }
    private static bool SendMail(IEnumerable<string> mailServers, string from, string to, string subject, string body, IEnumerable<Stream> image = null, Action<Exception> onError = null) {
      return mailServers
        .Where(ms => !string.IsNullOrWhiteSpace(ms))
        .Select(mailServer => SendMail(mailServer, from, to, subject, body, image, onError))
        .Any(res => res);
    }
    private static bool SendMail(string mailServer, string from, string to, string subject, string body, IEnumerable<Stream> image, Action<Exception> onError) {
      return SendMailSecure(mailServer, null, "", "", from, to, subject, body, image, onError);
    }
    private static bool SendMail(string mailServer, string from, string to, string subject, string body, IEnumerable<string> logoPath, Action<Exception> onError) {
      return SendMailSecure(mailServer, null, "", "", from, to, subject, body, logoPath, onError);
    }
    public static bool SendMailSecure(string mailServer, int? port, string userName, string password, string from, string to, string subject, string body, IEnumerable<string> logoPath, Action<Exception> onError) {
      return SendMailSecure(mailServer, port, userName, password, from, to, subject, body, (logoPath ?? new string[0]).Select(lp => File.OpenRead(Helpers.EmailLogo(lp))), onError);
    }
    public static bool SendMailSecure(string mailServer, int? port, string userName, string password, string from, string to, string subject, string body, IEnumerable<Stream> image, Action<Exception> onError) {
      return SendMailSecure(mailServer, port, userName, password, from, to, subject, body
        , image == null ? (Action<MailMessage, string>)null : (m, b) => AddLickedResource(m, b, image)
        , onError);
    }
    public static bool SendMailSecure(string mailServer, int? port, string userName, string password, string from, string to, string subject, string body, Action<Exception> onError) {
      return SendMailSecure(mailServer, port, userName, password, from, to, subject, body, (Action<MailMessage, string>)null, onError);
    }
    public static bool SendMailSecure(string mailServer, int? port, string userName, string password, string from, string to, string subject, string body, Action<MailMessage, string> processor, Action<Exception> onError) {
      try {
        from = SafeEMailAddress(from);
        to = Regex.Replace(to, @"[(\{].*[\})]@", "@");
        to = to.Replace("___", "+");
        var mm = new MailMessage(from, to, subject, body) {
          IsBodyHtml = true,
          BodyEncoding = Encoding.UTF8,
          SubjectEncoding = Encoding.UTF8
        };
        using (var mc = SmtpClientFactory(mailServer, port, userName, password)) {
          if (processor != null) processor(mm, body);
          mc.Send(mm);
          var un = new[] { mc.Credentials as System.Net.NetworkCredential }
          .Where(nc => nc != null)
          .Select(nc => nc.UserName)
          .DefaultIfEmpty(userName)
          .Single();
          EventLogger.LogApplication(new { from, to, mc.Host, mc.Port, userName = un, mc.UseDefaultCredentials, status = "Success" }.ToJson(), 1, EventLogEntryType.Information);
          return true;
        }
      } catch (Exception exc) {
        exc = new Exception(new { mailServer, userName, from, to } + "", exc);
        if (onError != null) {
          onError(exc);
          return false;
        } else throw exc;
      }
    }
    static void AddLickedResource(MailMessage message, string body, IEnumerable<Stream> resources) {
      Func<Stream, string> ext = stream => new[] { stream as FileStream }.Where(fs => fs != null).Select(fs => Path.GetExtension(fs.Name)).DefaultIfEmpty("").Single();
      var linkedReses = (resources ?? new Stream[0])
        .Select(resource => new { resource, linkedRes = new LinkedResource(resource) })
        .Do(x => x.linkedRes.ContentId = (Guid.NewGuid() + "").Split('-').Last() + ext(x.resource))
        .Select(x => x.linkedRes)
        .ToArray();
      if (linkedReses.Any()) {
        var view = AlternateView.CreateAlternateViewFromString(body.Formatter(linkedReses.Select(lr => lr.ContentId).ToArray()), null, "text/html");
        linkedReses.ForEach(lr => view.LinkedResources.Add(lr));
        message.AlternateViews.Add(view);
      }
    }
    private static SmtpClient SmtpClientFactory(string mailServer, int? port, string userName = "", string password = "") {
      Passager.ThrowIf(() => string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password));
      Passager.ThrowIf(() => !string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(password));
      var sc = new SmtpClient(mailServer);
      if (!string.IsNullOrWhiteSpace(userName)) {
        sc.Credentials = new System.Net.NetworkCredential(userName, password);
        Debug.WriteLine(new { userName, password });
      } else
        sc.UseDefaultCredentials = true;
      if (port.HasValue && port.Value > 0)
        sc.Port = port.Value;
      return sc;
    }

    /// <summary>
    /// Get provided user. In case it's empty try to get user name from .Net
    /// </summary>
    /// <param name="user">Can be empty</param>
    /// <returns></returns>
    public static string SafeEMailAddress(string user) {
      if ((user ?? "").Contains("@")) return user;
      Func<Func<string>, string> getUser = f => { try { return f(); } catch { return null; } };
      Func<string, string> getEmail = u => GetEmail(u).FirstOrDefault();
      var getUsers = new Func<string>[] {
        () => user,
        ()=>UserPrincipal.Current.EmailAddress,
        ()=>getEmail(HttpContext.Current.YieldNoNull(c => c.User.Identity.Name).FirstOrDefault()),
        ()=>getEmail(System.Security.Principal.WindowsIdentity.GetCurrent().Name),
        ()=>getEmail(OperationContext.Current.YieldNoNull(c=>c.ServiceSecurityContext.PrimaryIdentity.Name).FirstOrDefault()),
        ()=>getEmail(OperationContext.Current.YieldNoNull(c=>c.ServiceSecurityContext.WindowsIdentity.Name).FirstOrDefault())
      };
      return getUsers.Select(getUser)
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .DefaultIfEmpty("Unknown@User")
        .First();
    }
    public static IList<string> GetEmail(string userID) {
      List<string> emailAddresses = new List<string>();
      if (!string.IsNullOrWhiteSpace(userID)) {
        string username = userID.Split('\\')[1];
        string domain = userID.Split('\\')[0];

        try {
          PrincipalContext domainContext = new PrincipalContext(ContextType.Domain, domain);
          UserPrincipal user = UserPrincipal.FindByIdentity(domainContext, username);
          // Add the "mail" entry
          emailAddresses.Add(user.EmailAddress);
          try {
            // Add the "proxyaddresses" entries.
            PropertyCollection properties = ((DirectoryEntry)user.GetUnderlyingObject()).Properties;
            foreach (object property in properties["proxyaddresses"]) {
              emailAddresses.Add(property.ToString());
            }
          } catch { }
        } catch (Exception exc) {
          EventLogger.LogApplication(new { username, domain, exception = exc });
        }
      }
      return emailAddresses;
    }
    /// <summary>
    /// Send exception information to email
    /// </summary>
    /// <param name="argsException"></param>
    /// <param name="to"></param>
    /// <param name="prefix"></param>
    /// <param name="onError"></param>
    /// <returns></returns>
    public static Exception LogWebException(this Exception argsException, string to, string prefix, Action<Exception> onError = null) {
      try {
        var messageBody = new List<string>();
        var innerException = argsException;
        while (innerException != null && innerException.InnerException != null) {
          innerException = innerException.InnerException;
          if (innerException is SqlException) {
            messageBody.Add((innerException as SqlException).ToMessage() + Environment.NewLine + argsException);
            argsException = new Exception(innerException.Message);
            break;
          }
          messageBody.Add(innerException.Message);
        }
        if (!messageBody.Any()) messageBody.Add(argsException.ToString());
        string from = Security.Who() + "@Error";
        MailServers()
          .Where(ms => !string.IsNullOrWhiteSpace(ms))
          .Take(1)
          .DefaultIfEmpty(() => { throw new Exception("No mail server found."); })
          .ForEach(mailServer => {
            var body = string.Join(Environment.NewLine, messageBody);
            var path = HttpContext.Current.YieldNoNull(c => c.Request).Select(r => r.Path).SingleOrDefault();
            SendMail(mailServer, from, to, prefix + " " + path + " Error", body, new Stream[0], onError);
          });
      } catch (Exception exc) {
        onError.YieldNoNull().ForEach(a => a(exc));
      }
      return argsException;
    }
  }
}
