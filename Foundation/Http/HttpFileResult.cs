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
  public class HttpFileResult : IHttpActionResult {
    private readonly string filePath;
    private readonly string contentType;
    private readonly bool delete;

    public HttpFileResult(string filePath, bool delete = false, string contentType = null) {
      this.filePath = filePath;
      this.contentType = contentType;
      this.delete = delete;
    }

    public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken) {
      return Task.Run(() => {
        var file = File.OpenRead(filePath);
        var stream = new MemoryStream();
        file.CopyTo(stream);
        stream.Position = 0;
        file.Dispose();
        var response = new HttpResponseMessage(HttpStatusCode.OK) {
          Content = new StreamContent(stream)
        };

        var contentType = this.contentType ?? MimeMapping.GetMimeMapping(Path.GetExtension(filePath));
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        try {
          return response;
        } finally {
          if (delete) {
            file.Dispose();
            File.Delete(filePath);
          }
        }
      }, cancellationToken);
    }
  }
}
