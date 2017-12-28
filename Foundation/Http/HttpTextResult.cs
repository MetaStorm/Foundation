using CommonExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Foundation {
  public class HttpTextResult : IHttpActionResult {
    public enum ContentType { plain, html };
    private string text;
    private string contentType;
    private string fileName;
    private HttpStatusCode httpStatus = HttpStatusCode.OK;

    public static HttpTextResult FromFileName(string fileName, string text) {
      return new HttpTextResult() { fileName = fileName, text = text };
    }
    public HttpTextResult() {
    }
    public HttpTextResult(IEnumerable<string> text, string contentType, HttpStatusCode httpStatus)
      : this(string.Join("\n", text), contentType, httpStatus) {
    }
    public HttpTextResult(string text, string contentType, HttpStatusCode httpStatus) {
      this.text = text;
      this.contentType = contentType;
      this.httpStatus = httpStatus;
    }
    static string ContentToString(ContentType contentType) { return "text/" + contentType; }

    public static HttpTextResult Content(string text, HttpStatusCode statusCode) {
      return Content(text, ContentType.plain, statusCode);
    }
    public static HttpTextResult Content(string text, ContentType contentType, HttpStatusCode statusCode) {
      return Content(text, ContentToString(contentType), statusCode);
    }
    public static HttpTextResult Content(string text, string contentType, HttpStatusCode statusCode) {
      return new HttpTextResult(text, contentType, statusCode);
    }

    public static HttpTextResult NotFound(string text) {
      return new HttpTextResult(text, ContentToString(ContentType.plain), HttpStatusCode.NotFound);
    }
    public static HttpTextResult Ok(string text, ContentType contentType) {
      return Ok(text, ContentToString(contentType));
    }
    public static HttpTextResult Ok(string text, string contentType) {
      return new HttpTextResult(text, contentType, HttpStatusCode.OK);
    }
    public static HttpTextResult Ok(IEnumerable<string> text, ContentType contentType) {
      return Ok(text, "text/" + contentType);
    }
    public static HttpTextResult Ok(IEnumerable<string> text, string contentType) {
      return new HttpTextResult(text, contentType, HttpStatusCode.OK);
    }

    public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken) {
      return Task.Run(() => {
        var response = new HttpResponseMessage(this.httpStatus) {
          Content = new StringContent(text)
        };
        var ct = contentType.IsNullOrWhiteSpace()
        ? MimeMapping.GetMimeMapping(Path.GetExtension(fileName))
        : contentType;
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(ct);
        if (!fileName.IsNullOrWhiteSpace())
          response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline") { FileName = "filename.xls" };
        return response;
      }, cancellationToken);
    }
  }
}
