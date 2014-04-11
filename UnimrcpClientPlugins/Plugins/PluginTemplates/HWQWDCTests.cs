using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Timers;
using ucf;

namespace HWQWDCTests
{
    public class QwDcTestCase : HWTestCase
    {
        public QwDcTestCase(String name)
            : base(name)
        {

        }

        public QwDcTestCase(String name, StreamWriter logStream)
            : base(name, logStream)
        {

        }

        public override void ProcessRecognizeResult(IMrcpChannel channel, IMrcpMessage msg)
        {
            try
            {
                d("channel id: " + channel.GetChannelId() + " recv recognizer_recognition_complete");
                ParseNLResult(msg);
                resultflag = true;
            }
            catch (CaseFailedException cfe)
            {
                _app.OnCaseFailed(this, cfe._msg);
            }
        }

        public override void ParseNLResult(IMrcpMessage msg)
        {  
                CaseResult = msg.GetBody();
                String completion_cause = msg.GetHeader((int)MrcpConst.RECOGNIZER_HEADER_COMPLETION_CAUSE);
                d("completion_cause: " + completion_cause);
                d(CaseResult);
                AssertEquals("need a success completion cause", "success", completion_cause);  
        }
		
		public override void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_stoptimer)
            {
                //RECOGNIZER_RECOGNITION_COMPLETE result
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

    public class HWSingleTestFactory : HWBaseFactory
    {
        private static HWSingleTestFactory instance = null;
        private static Int32  curcaseindex = 0;
        private static Int32  casecount = 0;

        private HWSingleTestFactory(String filepath, String grxml, Int32 casecount)
            : base(filepath, grxml, casecount)
        {

        }

        public static HWSingleTestFactory GetInstance(String filepath, String grxml, Int32 casecount)
        {
            if (instance == null)
            {
                return new HWSingleTestFactory(filepath, grxml, casecount);
            }
            return instance;
        }

        public virtual ITestCase GetNextCase(String casemode)
        {
            if (curcaseindex == Files.Length)
                curcaseindex = 0;
            String casename = casemode + Convert.ToString(casecount);
            StreamWriter logStream = new StreamWriter(casename, false);
            QwDcTestCase tcase = new QwDcTestCase(casename, logStream);
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
                QwDcTestCase[] tcases = new QwDcTestCase[files.Length];
                int caseindex = 0;
                string casename = "HWTEST";
                foreach (string file in files)
                {
                    StreamWriter logStream = new StreamWriter(casename + Convert.ToString(caseindex));
                    tcases[caseindex] = new QwDcTestCase(casename + Convert.ToString(caseindex), logStream);
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

        static TestApp(){
            LOG_TW = new StreamWriter(new FileStream("testapp.log", FileMode.Create, FileAccess.Write));
            config = new Dictionary<string, object>();
            config["pbx"] = "HUAWEI";
            config["mode"] = "qw_dc";
            config["filelist"] = @"D:\liukaijin_70\config-asr\input_wav\keyword_lx.list";
            config["grxml"] = "http://192.168.5.72:8080/asr_gram/qw_dc.grxml";
            config["maxruncase"] = 1;
            config["report"] = "./report/report1.log";

            totalcaselist = new List<ITestCase>();
            successcaselist = new List<ITestCase>();
            failcaselist = new List<ITestCase>();
            skippedcaselist = new List<ITestCase>();
        }

        public TestApp():base(LOG_TW)
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
                     tcase = HWSingleTestFactory.GetInstance(config["filelist"].ToString(), config["grxml"].ToString(), 20).GetNextCase("qw_dc_test");
                     totalcaselist.Add(tcase);
                }         
                return tcase;
            }
           
        }

        public override  bool IsRuningCaseLimit()
        {
            return CurCaseCount >= Convert.ToInt32(config["maxruncase"].ToString()) ? true : false;
        }

        public override void OnCaseFailed(ITestCase tcase,String failmsg)
        {
            String errmsg = String.Format("case failed,reason:{0},case name: {1}",failmsg,tcase.Name);
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
            i(String.Format("case complete,total : {0}, success: {0}, fail: {1}", resultrep.Total,resultrep.Success, resultrep.Failures));
        }
    }
}