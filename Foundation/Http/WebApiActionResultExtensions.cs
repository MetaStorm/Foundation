using CommonExtensions;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;

namespace Foundation {
  public static class ApiControllerMixin{
    private const string _httpContext = "MS_HttpContext";
    private const string RemoteEndpointMessage =
        "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
    private const string OwinContext = "MS_OwinContext";

    public static bool IsPrivate(this ApiController ac,out string ipAddress) {
      return IsPrivate(ipAddress = GetClientIp(ac.Request));
    }
    public static bool IsPrivate(this ApiController ac) {
      return IsPrivate(ac.Request);
    }
    public static string ClientIp(this ApiController ac) {
      return GetClientIp(ac.Request);
    }

    public static string GetClientIp(HttpRequestMessage request) {
      // Web-hosting
      if (request.Properties.ContainsKey(_httpContext)) {
        HttpContextWrapper ctx =
            (HttpContextWrapper)request.Properties[_httpContext];
        if (ctx != null) {
          return ctx.Request.UserHostAddress;
        }
      }

      // Self-hosting
      if (request.Properties.ContainsKey(RemoteEndpointMessage)) {
        RemoteEndpointMessageProperty remoteEndpoint =
            (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessage];
        if (remoteEndpoint != null) {
          return remoteEndpoint.Address;
        }
      }

      // Self-hosting using Owin
      if (request.Properties.ContainsKey(OwinContext)) {
        OwinContext owinContext = (OwinContext)request.Properties[OwinContext];
        if (owinContext != null) {
          return owinContext.Request.RemoteIpAddress;
        }
      }

      return null;
    }
    public static bool IsPrivate(HttpRequestMessage request) {
      return IsPrivate(GetClientIp(request));
    }
    private static bool IsPrivate(string ipAddress) {
      var ipParsed = ipAddress?.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
      if (ipParsed.Length < 4) return true;
      int[] ipParts = ipParsed.Select(s => int.Parse(s)).ToArray();
      // in private ip range
      if (ipParts[0] == 10 ||
          (ipParts[0] == 192 && ipParts[1] == 168) ||
          (ipParts[0] == 172 && (ipParts[1] >= 16 && ipParts[1] <= 31))) {
        return true;
      }

      // IP Address is probably public.
      // This doesn't catch some VPN ranges like OpenVPN and Hamachi.
      return false;
    }

  }
  public class ApiControllerIE: ApiController {
    public IHttpActionResult ContentByBrowser<T>(HttpStatusCode statusCode, T value) {
      var ie = new[] { "InternetExplorer","IE" }.Select(s=>s.ToLower());
      var browser = HttpContext.Current.Request.Browser;
      if (ie.Any(b=>browser.Browser.ToLower().StartsWith(b)))
        return Foundation.HttpTextResult.Content(value.ToJson(), statusCode);
      return base.Content(statusCode, value);
    }
  }
}
