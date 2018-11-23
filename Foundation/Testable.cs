using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonExtensions;
using System.Dynamic;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.ComponentModel;

namespace Foundation {
  public interface ITestable {
    ExpandoObject RunTestFast(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge);
    ExpandoObject RunTest(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge);
  }
  public abstract class Testable<TTest> where TTest : Testable<TTest>, new() {
    private static bool _isTested = false;

    protected static bool IsTested {
      get { return Testable<TTest>._isTested; }
      set {
        //if (typeof(Testable<TTest>).GenericTypeArguments.Select(gt => gt.Name).FirstOrDefault() == "EventLoggerCustom")
        //  Debugger.Break();
        Testable<TTest>._isTested = value;
      }
    }
    /// <summary>
    /// Reset IsTested to false.
    /// </summary>
    public static void UnTest() { IsTested = false; }
    protected static int IsInTest = 0;

    private static readonly TTest instance = new TTest();
    public static TTest Instance { get { return instance; } }

    static Task _ShouldRunTask = Testable.CompletedTask;

    public static Task ShouldRunTask {
      get { return _ShouldRunTask; }
      private set { _ShouldRunTask = value; }
    }
    static Testable() {
      //if (Debugger.IsAttached) Debugger.Break();
      if(Testable.ShouldRunTests)
        ShouldRunTask = Task.Run(async () => {
          try {
            if(IsInTest == 0)
              Testable.WriteLine((await RunTestFastAsync(null)).ToJson());
          } catch(Exception exc) {
            Core.EventLogger.LogApplicationError(exc);
          }
        });
    }
    protected string GetTestSpace(string className) {
      return GetTestSpace(GetType(), className);
    }
    protected static string GetTestSpace<T>(string className) { return GetTestSpace(typeof(T), className); }
    static string GetTestSpace(Type type, string className) {
      return type.GenericTypeArguments.DefaultIfEmpty(type).Select(t => t.FullName).Flatten() + "{" + className + "}";
    }
    protected async virtual Task<ExpandoObject> _RunTestAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
      return await _RunTestFastAsync(parameters, merge);
    }
    protected async virtual Task<ExpandoObject> _RunTestFastAsync(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
      return await _RunTestFastImpl(parameters, merge);
    }
    async Task<ExpandoObject> _RunTestFastImpl(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge) {
      var exceptions = new List<Exception>();
      Action<Exception> error = exc => exceptions.Add(exc);
      var res = await (from identity in RunWithException(Helpers.Identity, error)
                       let ass = GetType().Assembly
                       from location in RunWithException(() => ass.Location, error)
                       from fullName in RunWithException(() => ass.GetName().Name + ":" + ass.GetName().Version, error)
                       select new { identity, location, fullName });
      if(exceptions.Count > 0)
        throw new AggregatedException(exceptions);
      var expMerge = merge.Aggregate(new ExpandoObject(), (exp, test) => exp.Merge(test(parameters ?? new ExpandoObject())));
      IsTested = true;
      return await Task.FromResult((parameters ?? new ExpandoObject()).Merge(GetTestSpace("Testable"), new ExpandoObject().Merge(res.ToExpando().Merge(new { IsTested })).Merge(expMerge)));
    }
    static async Task<ExpandoObject> HasBeenTestedAsync(ExpandoObject parameters, Testable.RunTestAsyncDelegate test, params Func<ExpandoObject, ExpandoObject>[] merge) {
      try {
        return
        IsTested
          ? new ExpandoObject().Merge(GetTestSpace<TTest>("Testable"), new { IsTested }.ToExpando())
          : await test(parameters, merge);
      } catch {
        IsTested = false;
        throw;
      }
    }
    async static Task<T> RunWithException<T>(Func<T> test, Action<Exception> error) {
      try {
        return await Task.FromResult(test());
      } catch(Exception exc) {
        error(exc);
        return default(T);
      }
    }

    public static Testable.RunTestAsyncDelegate RunTestAsync { get { return Instance._RunTestAsync; } }
    public static Testable.RunTestAsyncDelegate RunTestFastAsync { get { return Instance._RunTestFastAsync; } }


    /// <summary>
    /// Will run test in try/catch block.
    /// Exceptions will be logged as "Application Error" EventLog entry and re-thrown back to execution stack.
    /// </summary>
    /// <param name="test"></param>
    /// <returns></returns>
    static async Task<ExpandoObject> TestHost(ExpandoObject parameters, Testable.RunTestAsyncDelegate test, params Func<ExpandoObject, ExpandoObject>[] merge) {
      return await TestHostAsync(parameters, test, null, merge);
    }
    //[MethodImpl(MethodImplOptions.Synchronized)]
    private static SemaphoreSlim m_lock = new SemaphoreSlim(initialCount: 1);
    protected static async Task<ExpandoObject> TestHostAsync(ExpandoObject parameters, Testable.RunTestAsyncDelegate test, Action<object> logger, params Func<ExpandoObject, ExpandoObject>[] merge) {
      try {
        //await m_lock.WaitAsync(); 
        IsInTest++;
        return await HasBeenTestedAsync(parameters, test, merge);
      } catch(Exception exc) {
        if(logger != null) logger(exc);
        else Core.EventLogger.LogApplication(exc);
        throw;
      } finally {
        --IsInTest;
      }
    }
    /// <summary>
    /// For future development
    /// </summary>
  }
  [Serializable]
  public class TestableException :Exception {
    public Type TestedType { get; set; }
    public TestableException(Type type, Exception exc) : base("", exc) { this.TestedType = type; }
    protected TestableException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
  }
  public static class Testable {
    public readonly static Task CompletedTask = Task.Run(() => { });

    internal static void WriteLine(string text) {
      if(Verbose && !System.Console.IsOutputRedirected)
        Console.WriteLine(text);
    }
    public static bool Verbose = false;
    static bool _shouldRunTests = false;
    public static bool ShouldRunTests {
      get { return _shouldRunTests; }
      set { _shouldRunTests = value; }
    }
    public delegate Task<ExpandoObject> RunTestAsyncDelegate(ExpandoObject parameters, params Func<ExpandoObject, ExpandoObject>[] merge);
    public static Task<Dictionary<Type, ExpandoObject>> RunTestsAsync(bool runFast, Func<TestableException, bool> exceptionHandler, params string[] exceptTypeFullName)
      => RunTestsAsync(runFast, null, exceptionHandler, exceptTypeFullName);
    /// <summary>
    /// Call RunTest(Fast) in all "tesable" types in all assemblies.
    /// </summary>
    /// <param name="runFast">fasle:use RunTestFast,true: use RunTest</param>
    /// <param name="log"></param>
    /// <param name="exceptionHandler">Function that returns false in case of unhandled exception</param>
    /// <param name="exceptTypeFullName">Full names of types which shouldn't run</param>
    public static async Task<Dictionary<Type, ExpandoObject>> RunTestsAsync(bool runFast
    , Action<(Type t, PropertyInfo pi)> log
    , Func<TestableException, bool> exceptionHandler
    , params string[] exceptTypeFullName) {
      Dictionary<Type, ExpandoObject> tested = new Dictionary<Type, ExpandoObject>();
      var testMethod = "RunTest" + (runFast ? "Fast" : "") + "Async";
      var testsExceptions = (await
        (from t in GetAllTypes(true)
         where !exceptTypeFullName.Select(s => s.ToLower()).Contains(t.FullName.ToLower())
         orderby t.Name
         from f in t.GetPropertyForSure(true, testMethod)
         where f.GetMethod.ContainsGenericParameters == false
         select new { t, f }
         )
         .Distinct(a => a.t)
         .OrderBy(a => a.t.FullName)
         //.AsParallel()
         .Select(async a => {
           try {
             var d = a.f.GetValue(null) as RunTestAsyncDelegate;
             try { log?.Invoke((a.t, a.f));
             } catch(Exception exc) {
               if(Debugger.IsAttached)
                 Debug.WriteLine(exc);
             }
             if(Debugger.IsAttached)
               Debug.WriteLine($"+++{a.t.FullName}: start");
             var r = await d(null);
             if(Debugger.IsAttached)
               Debug.WriteLine($"+++{a.t.FullName}: end");
             if(tested != null) tested.Add(a.t, r);
             return null;
           } catch(TargetInvocationException exc) {
             return new TestableException(a.t, exc.InnerException);
           } catch(Exception exc) {
             return new TestableException(a.t, exc);
           }
         }).WhenAllSequiential())
         .Where(exc => exc != null);
      new AggregateException(testsExceptions)
        .Flatten()
        .Handle(exc => exceptionHandler((TestableException)exc));
      return tested;
    }


    public static IEnumerable<string> UnTest() {
      var testMethod = "UnTest";
      return (from t in GetAllTypes(true)
              from f in t.GetMethodForSure(true, testMethod)
              where f.ContainsGenericParameters == false
              select new { t, f }
       )
       .Distinct(a => a.t)
       .Select(a => {
         a.f.Invoke(a.t, null);
         return a.t.Name;
       });
    }

    static Type _testableType = typeof(Testable<>);
    private static bool IsTestable(Type t) {
      return !t.IsAbstract && t.IsClass && t.IsOfGenericType(_testableType) && ((TypeInfo)(t)).GenericTypeParameters.Length == 0;
    }
    public static IEnumerable<Type> GetAllTypes(bool isTestable) {
      return GetAllTypes()
        .Where(t => !isTestable || IsTestable(t));
    }

    public static IEnumerable<Type> GetAllTypes() {
      Func<Assembly, IEnumerable<Type>> getTypes = (a) => {
        try { return a.GetTypes(); } catch(ReflectionTypeLoadException) { return new Type[0]; }
      };
      return Reflection.LoadAssemblies()
        .Where(a => a.CodeBase?.ToLower().Contains("/obj/") == false)
        .SelectMany(getTypes)
        .SelectMany(GetAllTypes);
    }
    public static IEnumerable<Type> GetAllTypes(Type type) {
      return new[] { type }.Concat(type.GetNestedTypes().SelectMany(t => GetAllTypes(t)));

    }
    public static IList<Assembly> LoadAssemblies(Action<Exception> errorHandler) {
      var dlls = Helpers.FindFiles("*.dll");
      var codeBase = Helpers.ToFunc((Assembly a) => new Uri(a.CodeBase.IfEmpty(a.Location)));
      var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).ToArray();
      var dllsToLoad = (from dll in dlls
                        join a in assemblies.Select(a => new { a, cb = codeBase(a) }) on new Uri(dll) equals a.cb into g
                        where g.IsEmpty()
                        select dll
                        ).ToArray();
      var loadedAssembleys = assemblies.Concat(dllsToLoad.Select(dll => Assembly.LoadFile(dll)));
      Func<Assembly, Type[]> loadTypes = a => {
        try {
          return a.GetTypes();
        } catch(ReflectionTypeLoadException exc) {
          if(errorHandler != null) {
            exc.LoaderExceptions.ForEach(e => errorHandler(e));
            return new Type[0];
          }
          throw;
        }
      };
      return loadedAssembleys.ToList();
    }
  }
}
