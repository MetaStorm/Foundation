using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions.TSql;
using static CommonExtensions.TSql.EntityFrameworkLib;
using CommonExtensions;
using System.Data.Entity.Infrastructure;
using System.Text.RegularExpressions;

namespace Foundation {
  public class DBLogger<TContext> : DbConfiger<DBLogger<TContext>, TContext> where TContext : DbContext, new() { }
  public class DbConfiger<T, TContext> : EventLogger<DbConfiger<T, TContext>> where T : DbConfiger<T, TContext>, new() where TContext : DbContext, new() {
    string testRoot { get { return GetType().FullName; } }
    protected override async Task<ExpandoObject> _RunTestFastAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
      return await TestHostAsync(parameters, async (p, m) => {
        var b = await base._RunTestFastAsync(parameters, merge);
        Func<DbContext, Exception, Exception> dbExcFactory = (c, exc) => new Exception(new {
          connection = CleanConnectionString(c.Database.Connection.ConnectionString) } + "", exc);
        var dbTestFast = UsingDbContext(() => new TContext(), context => {
          var connection = CleanConnectionString(context.Database.Connection.ConnectionString);
          return new { connection };
        }, false, dbExcFactory);
        return new ExpandoObject().Merge(testRoot, new { dbTestFast }.ToExpando().Merge(b));
      }, _LogError, merge);
    }
    protected override async Task<ExpandoObject> _RunTestAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
      return await TestHostAsync(parameters, async (p, m) => {
        var b = await base._RunTestAsync(parameters, merge);
        var dbTest = UsingDbContext(() => new TContext(), context => {
          context.Database.CommandTimeout = 60 * 3;
          var dbSets = context.VerifyEntities();
          var whoMe = context.Database.SqlQuery<string>("SELECT Foo+': '+[Nume] FROM [WhoMe]").ToArray();
          return new {
            whoMe,
            dbSets
          };
        }, false, null);
        return b.AddOrMerge(testRoot, new { dbTest });
      }, _LogError, merge);
    }

    public static string CleanConnectionString(string connection) {
      return Regex.Replace(connection, ";Password=[^;]+", "", RegexOptions.IgnoreCase);
    }
  }
}
