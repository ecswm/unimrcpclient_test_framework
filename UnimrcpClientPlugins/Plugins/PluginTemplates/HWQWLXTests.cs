using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Timers;
using ucf;

namespace HWQWLXTests
{
    public class QwLxTestCase : HWTestCase
    {
        public QwLxTestCase(String name)
            : base(name)
        {

        }

        public QwLxTestCase(String name, StreamWriter logStream)
            : base(name, logStream)
        {

        }

        public override void ProcessRecognizeResult(IMrcpChannel channel, IMrcpMessage msg)
        {
            d("channel id: " + channel.GetChannelId() + " recv recongizer_intermedia_result");
            ParseNLResult(msg);
            resultflag = true;
            lock (_stoptimer)
            {
                if (_stoptimer.Enabled)
                {
                    _stoptimer.Stop();
                    _stoptimer.Start();
                }
            }
        }

        public override void ParseNLResult(IMrcpMessage msg)
        {
            //CaseResult += msg.GetBody();
        }
		
		public override void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_stoptimer)
            {
                try
                {
                    var timer = (Timer)sender;
                    timer.Stop();
                    AssertTrue("need a resultflag true", resultflag);
                    _app.OnCaseSuccess(this);
                }
                catch (CaseFailedException cfe)
                {
                    _app.OnCaseFailed(this, cfe._msg);
                }
            }
        }
    }

    public class HWContinuousTestFactory : HWBaseFactory
    {
        private static HWContinuousTestFactory instance = null;
        private static Int32 curcaseindex = 0;
        private static Int32 casecount = 0;

        private HWContinuousTestFactory(String filepath, String grxml, Int32 casecount)
            : base(filepath, grxml, casecount)
        {
            
        }

        public static HWContinuousTestFactory GetInstance(String filepath,String grxml,Int32 casecount)
        {
            if (instance == null)
            {
                return new HWContinuousTestFactory(filepath, grxml, casecount);
            }
            return instance;
        }

        public virtual ITestCase GetNextCase(String casemode)
        {
            if (curcaseindex == Files.Length)
               curcaseindex = 0;
            String casename = casemode + Convert.ToString(casecount);
            StreamWriter logStream = new StreamWriter(casename, false);
            QwLxTestCase tcase = new QwLxTestCase(casename, logStream);
            tcase.filename = Files[curcaseindex];
            tcase.grxml = Grxml;
            curcaseindex++;
            casecount++;
            return tcase;
        }

        public override ITestCase[] CreateHWCase()
        {
            string[] files = GetCaseFileName();
            if (files.Length > 0)
            {
                QwLxTestCase[] tcases = new QwLxTestCase[files.Length];
                int caseindex = 0;
                string casename = "HWTEST";
                foreach (string file in files)
                {
                    StreamWriter logStream = new StreamWriter(casename +  Convert.ToString(caseindex));
                    tcases[caseindex] = new QwLxTestCase(casename + Convert.ToString(caseindex), logStream);
                    tcases[caseindex].filename = file;
                    tcases[caseindex].grxml = Grxml;
                    caseindex++;
                }
                return tcases;
            }
            return null;
        }
        
    }

    class TestApp : BaseApp
    {
        static TextWriter LOG_TW;
        static Dictionary<string, object> config;

        static TestResultRep resultrep;
        static List<ITestCase> totalcaselist;
        static List<ITestCase> successcaselist;
        static List<ITestCase> failcaselist;
        static List<ITestCase> skippedcaselist;

        static TestApp()
        {
            LOG_TW = new StreamWriter(new FileStream("testapp.log", FileMode.Create, FileAccess.Write));
            config = new Dictionary<string, object>();
            config["pbx"] = "HUAWEI";
            config["mode"] = "continuous";
            config["filelist"] = @"D:\liukaijin_70\config-asr\70.list";
            config["grxml"] = "http://192.168.5.72:8080/asr_gram/qw_lx.grxml";
            config["maxruncase"] = 20;
            config["report"] = "./report/report2.log";

            totalcaselist = new List<ITestCase>();
            successcaselist = new List<ITestCase>();
            failcaselist = new List<ITestCase>();
            skippedcaselist = new List<ITestCase>();
        }

        public TestApp()
            : base(LOG_TW)
        {
            resultrep = new TestResultRep(config["report"].ToString());
            TotalCaseCount = 100;
        }


        public override ITestCase Case
        {
            get
            {
                 ITestCase tcase = null;
                 if (!IsRuningCaseLimit())
                 {
                     tcase = HWContinuousTestFactory.GetInstance(config["filelist"].ToString(), config["grxml"].ToString(), 20).GetNextCase("qw_lx_test");
                     totalcaselist.Add(tcase);
                 }
                return tcase;
            }   
        }

        public override bool IsRuningCaseLimit()
        {
            return CurCaseCount >= (int)config["maxruncase"] ? true : false;
        }

        public override void OnCaseFailed(ITestCase tcase, String failmsg)
        {
            String errmsg = String.Format("case failed,reason:{0},case name: {1}", failmsg, tcase.Name);
            i(errmsg);
            tcase.CaseResult = errmsg;
            tcase.tearDown();
            failcaselist.Add(tcase);
            resultrep.Failures += 1;
        }

        public override void OnCaseSuccess(ITestCase tcase)
        {
            String successmsg = String.Format("case success,case name: {0}", tcase.Name);
            i(successmsg);
            tcase.tearDown();
            successcaselist.Add(tcase);
            resultrep.Success += 1;
        }

        public override void GenerateRepo()
        {
            resultrep.Total = TotalCaseCount;
            resultrep.FailCases = failcaselist.ToArray();
            resultrep.SuccessCases = successcaselist.ToArray();
            resultrep.SkippedCases = skippedcaselist.ToArray();

            resultrep.PrintRep();
            i(String.Format("case complete,total : {0}, success: {0}, fail: {1}", resultrep.Total, resultrep.Success, resultrep.Failures));
        }
    }
}