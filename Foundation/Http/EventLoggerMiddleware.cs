using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Foundation.OWIN {
  using Newtonsoft.Json.Linq;
  using System.Diagnostics;
  using AppFunc = Func<System.Collections.Generic.IDictionary<string, object>, Task>;
  public abstract class EventLoggerMiddleware<TLogger> : Foundation.EventLogger<TLogger> where TLogger : EventLoggerMiddleware<TLogger>, new() {
    private readonly AppFunc next;
    private string[] pathsToIntercept;
    private AppFunc debug;
    public AppFunc Always;
    protected EventLoggerMiddleware() : base() { }
    public EventLoggerMiddleware(AppFunc next, AppFunc debug, string[] pathsToIntercept) {
      this.next = next;
      this.pathsToIntercept = pathsToIntercept.Select(s => s.ToLower()).ToArray();
      this.debug = debug;
    }
    public EventLoggerMiddleware(AppFunc next, string[] pathsToIntercept) {
      this.next = next;
      this.pathsToIntercept = pathsToIntercept.Select(s => s.ToLower()).ToArray();
    }

    public async Task Invoke(IDictionary<string, object> env) {
      Func<string> path = () => ((string)env["owin.RequestPath"]).ToLower();
      if (Always != null)
        await Always(env);
      if (!pathsToIntercept.Contains(path())) {
        try {
          if (debug != null)
            await debug(env);
          await this.next(env);
        } catch (Exception exc) {
          LogError(exc);
        }
        return;
      }
      //IOwinContext context = new OwinContext(env);

      // Buffer the response
      var stream = (Stream)env["owin.ResponseBody"]; //context.Response.Body;
      var buffer = new MemoryStream();
      //context.Response.Body = buffer;
      env["owin.ResponseBody"] = buffer;
      Exception error = null;
      try {
        await this.next(env);
      } catch (Exception exc) {
        LogError(new Exception(new { path = path() } + "", exc));
        error = exc;
      }
      if (error != null) {
        using (var ms = new MemoryStream()) {
          using (var sw = new StreamWriter(ms)) {
            await sw.WriteAsync(error.Message);
            await sw.FlushAsync();
            ms.Position = 0;
            ////context.Response.ContentLength = ms.Length;
            //context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            env["owin.ResponseStatusCode"] = (int)HttpStatusCode.InternalServerError;
            await ms.CopyToAsync(stream);
          }
        }
      } else {
        if ((int)env["owin.ResponseStatusCode"] >= 400) {
          Func<string, JObject> tryParse = s => {
            try {
              return JObject.Parse(s);
            } catch {
              return JObject.FromObject(new { error = s });
            }
          };
          buffer.Seek(0, SeekOrigin.Begin);
          var reader = new StreamReader(buffer);
          string responseBody = await reader.ReadToEndAsync();
          var jError = tryParse(responseBody);
          jError["path"] = path();
          LogError(jError);
          // Now, you can access response body.
          //Debug.WriteLine(responseBody);

          // You need to do this so that the response we buffered
          // is flushed out to the client application.
        }
        buffer.Seek(0, SeekOrigin.Begin);
        await buffer.CopyToAsync(stream);
      }
    }
  }
}
