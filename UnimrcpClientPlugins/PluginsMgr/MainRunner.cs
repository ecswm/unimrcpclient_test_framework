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

namespace PluginsMgr
{
    public static class MainRunner
    {
        static int _tcaseindex = 0;
        static ITestApp _app;
        static volatile bool _quit = false;
        static IMrcpChannelMgr _channelMgr;
        static CodeDomProvider _engine = CSharpCodeProvider.CreateProvider("csharp");

        public static void Quit()
        {
            _quit = true;
        }

        public static ITestApp Init(IMrcpChannelMgr mgr, Object param)
        {
            _channelMgr = mgr;
            string appSrc = param as string;
            _app = LoadTestApp(appSrc);
            return _app;
        }

        static Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();
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

        private static ITestApp LoadTestApp(string appSrc)
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
            CompilerResults results = _engine.CompileAssemblyFromFile(opts, new string[] { appSrc });
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
                    app = asb.CreateInstance("Tests.TestApp") as ITestApp;
                    //app = new PluginTemplates.TestApp();
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
            while (!_quit)
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
                    tc.SetRunningState();
                    tc.OnPostCmdRun();
                }
                //TODO:: wait with locker
                Thread.Sleep(100);
            }
            return false;
        }

        private static ITestCase GetNextReadyCase()
        {
            try
            {
                if (_tcaseindex >= _app.Cases.Length)
                    return null;
                ITestCase tcase = _app.Cases[_tcaseindex];
                tcase.OnCreate(_app);
                if (tcase.OnCondition())
                {
                    _tcaseindex++;
                    return tcase;
                }
                return null;
            }
            catch (IndexOutOfRangeException ex)
            {
                return null;
            }
            
        }
    }
    public interface ICmdRunner
    {
        bool Run(ITestCase tc);
    }
}
