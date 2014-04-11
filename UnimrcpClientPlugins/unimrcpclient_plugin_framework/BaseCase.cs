using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using ucf;

namespace ucf
{
    public class BaseCase : ITestCase
    {
        String _name;
        Logger _logger;
        public ITestApp _app;
        Boolean    _streaming;
        volatile Boolean _state;
        DateTime begintime;
        TimeSpan costtime;
        String _caseresult;

        public virtual bool Streaming
        {
            get { return _streaming;  }
            set { _streaming = value; }
        }

        public virtual bool State
        {
            get { return _state; }
            set { _state = value; }
        }

        public virtual string Name
        {
            get { return _name; }
        }

        public virtual String CaseResult
        {
            get { return _caseresult; }
            set { _caseresult = value; }
        }

        public virtual ITestCase[] Cases
        {
            get { return new ITestCase[0]; }
        }

        public BaseCase(string name)
        {
            _logger = new Logger(name, Console.Out);
            _name = name;
        }
        public BaseCase(string name, StreamWriter logStream)
        {
            _logger = new Logger(name, logStream);
            _name = name;
        }

        public virtual void OnCreate(ITestApp app)
        {
            _app = app;
            d(String.Format("OnCreate({0})", _app));
        }

        public virtual void OnDestory()
        {
            d("OnDestory()");
            _logger.Finish();
        }

        public void e(string tag, string msg, Exception e)
        {
            _logger.e(tag, msg, e);
        }

        public void e(string tag, string msg)
        {
            _logger.e(tag, msg);
        }

        public void w(string tag, string msg)
        {
            _logger.w(tag, msg);
        }

        public void d(string tag, string msg)
        {
            _logger.d(tag, msg);
        }

        public void i(string tag, string msg)
        {
            _logger.i(tag, msg);
        }

        public void v(string tag, string msg)
        {
            _logger.v(tag, msg);
        }

        public void e(string msg, Exception e)
        {
            _logger.e(msg, e);
        }

        public void e(string msg)
        {
            _logger.e(msg);
        }

        public void w(string msg)
        {
            _logger.w(msg);
        }

        public void d(string msg)
        {
            _logger.d(msg);
        }

        public void i(string msg)
        {
            _logger.i(msg);
        }

        public void v(string msg)
        {
            _logger.v(msg);
        }


        public virtual string Cmd
        {
            get { return ""; }
        }

        public bool OnCondition()
        {
            if (!_app.IsRuningCaseLimit())
            {
                return true;
            }
            return false;
        }

        public void OnPreCmdRun()
        {
            i("OnPreCmdRun()");
        }

        public void OnPostCmdRun()
        {
            i("OnPostCmdRun()");
        }

        public virtual void OnChannelAdd(IMrcpChannel channel)
        {
            i(String.Format("OnChannelAdd({0})", channel));
        }

        public virtual void OnChannelRemove(IMrcpChannel channel)
        {
            i(String.Format("OnChannelRemove({0})", channel));
        }

        public virtual void OnMessageReceive(IMrcpChannel channel, IMrcpMessage msg)
        {
            i(String.Format("OnMessageRecive({0})", channel));
        }

        public virtual byte[] OnStreamRead(IMrcpChannel channel, int size,out int read)
        {
            read = 0;
            return new byte[0];
        }

        public virtual void OnStreamOut(IMrcpChannel channel)
        {
            i(String.Format("OnStreamOut({0})", channel));
        }

        public virtual void SetRunningState()
        {
            State = true;
            _app.IncreaseCaseCount();
        }

        public virtual void setUp()
        {
            i(String.Format("case name {0} setup", Name));
            begintime = DateTime.Now;
        }

        public virtual void tearDown()
        {
            i(String.Format("case name {0} teardown", Name));
            costtime = DateTime.Now - begintime;
        }

        public virtual Double CostTime
        {
            get { return costtime.TotalMilliseconds/1000; }
        }

        protected void AssertEquals(String message, Object expected, Object actual)
        {
            if (!expected.Equals(actual))
            {
                String errorMessage = String.Format("{0} \n excpet <{1}> but <{2}>", message, ((MrcpReqState)expected).ToString("G"), ((MrcpReqState)actual).ToString("G"));
                throw new CaseFailedException(_app, this, errorMessage);
            }
        }

        protected void AssertTrue(String message, Boolean condition)
        {
            if (!condition)
            {
                String errorMessage = String.Format("{0} \n except <True> but <false>",message);
                throw new CaseFailedException(_app, this, errorMessage);
            }
        }
    }

    public class CaseFailedException : Exception
    {
        public ITestApp _app;
        public ITestCase _tcase;
        public String _msg;

        public CaseFailedException(ITestApp app, ITestCase tcase, String message)
        {
            _app = app;
            _tcase = tcase;
            _msg = message;
        }
    }

    public class TestResultRep : ITestResultRep
    {
        TextWriter _logger;

        ITestCase[] successcases;
        ITestCase[] failcases;
        ITestCase[] skippedcases;

        static Int32 totalcount;
        static Int32 successcount;
        static Int32 failcount;
        static Int32 skippedcount;

        public ITestCase[] SuccessCases
        {
            get { return successcases; }
            set { successcases = value; }
        }

        public ITestCase[] FailCases
        {
            get { return failcases; }
            set { failcases = value; }
        }

        public ITestCase[] SkippedCases
        {
            get { return skippedcases; }
            set { skippedcases = value; }
        }

        public Int32 Total
        {
            get { return totalcount; }
            set { totalcount = value; }
        }

        public Int32 Failures
        {
            get { return failcount; }
            set { failcount = value; }
        }

        public Int32 Skipped
        {
            get { return skippedcount; }
            set { skippedcount = value; }
        }

        public Int32 Success
        {
            get { return successcount; }
            set { successcount = value; }
        }

        public TestResultRep(String resultname)
        {
             string directoryName = Path.GetDirectoryName(resultname);
             if ((directoryName.Length > 0) && (!Directory.Exists(directoryName)))
             {
                Directory.CreateDirectory(directoryName);
             }
            _logger = new StreamWriter(new FileStream(resultname, FileMode.Create, FileAccess.Write));
        }

        public TestResultRep(StreamWriter logstream)
        {
            _logger = logstream;
        }

        public virtual void PrintRep()
        {
            //print report to text
            string timestamp = DateTime.Now.ToString("MM-dd HH:mm:ss.fff",
                                           CultureInfo.InvariantCulture);
            String reportstring = String.Format("Total TestCases is {0},SuccessCases is {1}, FailCaes is {2}", totalcount, successcount, failcount);
            StringBuilder fb = new StringBuilder();
            StringBuilder sb = new StringBuilder();
            if (failcount > 0)
            {
                fb.Append("FailCases Info As Flows: \n");
                foreach (ITestCase tcase in FailCases)
                {
                    fb.Append(String.Format("CaseName: <{0}> \n Message: <{1}> \n Time: <{2}> \n", tcase.Name, tcase.CaseResult, tcase.CostTime));
                }
                fb.Append("---------------------------------\n");
            }
            if (successcount > 0)
            {
                sb.Append("SuccessCases Info As Flows: \n");
                foreach (ITestCase tcase in SuccessCases)
                {
                    sb.Append(String.Format("CaseName: <{0}> \n Message: <{1}> \n Time: <{2}> \n", tcase.Name, tcase.CaseResult, tcase.CostTime));
                }
                sb.Append("---------------------------------\n");
            }
            _logger.WriteLine(timestamp + "\t" + reportstring);
            _logger.WriteLine(fb.ToString()+sb.ToString());
            _logger.Close();
        }
    }
}
