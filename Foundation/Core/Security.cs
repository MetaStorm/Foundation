using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CommonExtensions;

namespace Foundation.Core {
  public class Security {
    public static string Who() {
      string from = null;
      try { from = System.DirectoryServices.AccountManagement.UserPrincipal.Current.EmailAddress; } catch { }
      from = from ?? HttpContext.Current
        .YieldNoNull(c => c.User)
        .NoNull(u => u.Identity)
        .Select(i => i.Name).SingleOrDefault();
      return from;
    }
  }
}
