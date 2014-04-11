using System;
using System.Collections.Generic;
using System.Text;
using ucf;
using System.Runtime.InteropServices;
using System.Threading;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.JScript.Vsa;
using Microsoft.JScript;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using Microsoft.CSharp;
using System.Windows.Forms;

namespace PluginsMgr
{
    public static class MainRunner
    {
        static ITestApp _app;
        static volatile bool _quit = false;
        static Int32 casecount = 0;
        static IMrcpChannelMgr _channelMgr;
        static CodeDomProvider _engine = CSharpCodeProvider.CreateProvider("csharp");

        public static void Quit()
        {
            _quit = true;
        }

        public static ITestApp Init(IMrcpChannelMgr mgr, Object param)
        {
            _channelMgr = mgr;
            String appSrc = param as String;
            _app = LoadTestApp(appSrc);
            //AppDomain currentDomain = AppDomain.CurrentDomain;
            //currentDomain.UnhandledException += UnhandledExceptionEventHandler;
            return _app;
        }
        /*
        private static void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs args)
        {
            if (args.ExceptionObject is CaseFailedException)
            {
                CaseFailedException cfe = (args.ExceptionObject as CaseFailedException);
                StackTrace st = new StackTrace(cfe);
                string stackIndent = "";
                for (int i = 0; i < st.FrameCount; i++)
                {
                    // Note that at this level, there are four 
                    // stack frames, one for each method invocation.
                    System.Diagnostics.StackFrame sf = st.GetFrame(i);
                    Console.WriteLine();
                    Console.WriteLine(stackIndent + " Method: {0}",
                        sf.GetMethod());
                    Console.WriteLine(stackIndent + " File: {0}",
                        sf.GetFileName());
                    Console.WriteLine(stackIndent + " Line Number: {0}",
                        sf.GetFileLineNumber());
                    stackIndent += "  ";
                }
                cfe._app.OnCaseFailed(cfe._tcase,cfe._msg + cfe.StackTrace);
            }
        }
         * */

        static Dictionary<String, Assembly> assemblies = new Dictionary<String, Assembly>();
        static MainRunner()
        {
            AppDomain.CurrentDomain.AssemblyLoad += (sender, e) =>
            {
                assemblies[e.LoadedAssembly.FullName] = e.LoadedAssembly;
            };
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                Assembly assembly = null;
                assemblies.TryGetValue(e.Name, out assembly);
                return assembly;
            };
        }

        private static ITestApp LoadTestApp(String appSrc)
        {
            ITestApp app = null;
            CompilerParameters opts = new CompilerParameters();
            opts.GenerateExecutable = false;
            opts.GenerateInMemory = true;
            opts.IncludeDebugInformation = true;
            //opts.OutputAssembly = "testapp.dll";
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Select(a => a.Location)
                .ToList().ForEach(l => opts.ReferencedAssemblies.Add(l));
            String[] appParams = appSrc.Split('|');
            CompilerResults results = _engine.CompileAssemblyFromFile(opts, new String[] { appParams[0] });
            if (results.Errors.Count > 0)
            {
                foreach (CompilerError err in results.Errors)
                {
                    Console.Error.WriteLine(
                        String.Format("{0}:{1}", err.Line, err.ToString())
                    );
                    Debug.WriteLine(String.Format("{0}:{1}", err.Line, err.ToString()));
                }
            }
            else
            {
                AppDomain.CurrentDomain.Load(results.CompiledAssembly.GetName());
                Assembly asb = results.CompiledAssembly;
                try
                {
                    if (appParams.Length == 2)
                    {
                        app = asb.CreateInstance(appParams[1]) as ITestApp;
                    }
                    else
                    {
                        app = asb.CreateInstance("Tests.TestApp") as ITestApp;
                    }
                }
                catch (Exception exp)
                {
                    Console.Error.WriteLine(exp.InnerException.ToString());
                    Debug.WriteLine(exp.InnerException.ToString());
                }
            }
            return app;
        }

        public static bool Run(ICmdRunner runner)
        {
            while (!_quit &!IsCaseCountLimit())
            {
                ITestCase tc = GetNextReadyCase();
                if (tc != null)
                {
                    tc.OnPreCmdRun();
                    if (!runner.Run(tc))
                    {
                        tc.OnPostCmdRun();
                        return false;
                    }
                    tc.OnPostCmdRun();
                }
                //TODO:: wait with locker
                Thread.Sleep(100);
            }
            while (_app.CurCaseCount != 0)
            {
                Thread.Sleep(100);
            }
            _app.GenerateRepo();
            return false;
        }

        private static bool IsCaseCountLimit()
        {
            if (casecount < _app.TotalCaseCount)
            {
                return false;
            }
            return true;
        }

        private static ITestCase GetNextReadyCase()
        {
            ITestCase tcase =  _app.Case;
            if(tcase!=null)
            {
                tcase.OnCreate(_app);
                tcase.setUp();
                _app.IncreaseCaseCount();
                casecount++;
                return tcase;
            }
            return null;
        }
    }
    public interface ICmdRunner
    {
        bool Run(ITestCase tc);
    }
}
