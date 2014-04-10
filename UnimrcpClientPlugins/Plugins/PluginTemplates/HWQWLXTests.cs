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
            d(msg.GetBody());
            caseresult = HWCaseResult.QW_LX_NORMAL;
        }
		
		public override void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_stoptimer)
            {
                //RECOGNIZER_RECOGNITION_COMPLETE result
                if (!resultflag)
                {
                    //Timeout log
                    String channelid = _mrcp.GetChannelId();
                    string filename = string.Format("{0}timeout/{1}_{2}", AppDomain.CurrentDomain.BaseDirectory, channelid, Name);
                    caseresult = HWCaseResult.QW_LX_TIMEOUT;
                    StreamWriter timeoutwriter = new StreamWriter(filename);
                    
                    timeoutwriter.Write("channle id :" + channelid + " timeout!!!");
                    timeoutwriter.Close();
                }
                SendStopChannel(_mrcp);
                var timer = (Timer)sender;
                timer.Stop();
            }
        }

        public override void NotifyException()
        {
            caseresult = HWCaseResult.QW_LX_EXCEPTION;
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
        static String pbx;
        static String filelist;
        static String grxml;
        static String testmode;
        static Int32 maxruncase = 20;
        static volatile int runingcasecount;
        static TextWriter LOG_TW;
        static Dictionary<string, object> config;

        static TestApp()
        {
            LOG_TW = new StreamWriter(new FileStream("testapp.log", FileMode.Create, FileAccess.Write));
            config = new Dictionary<string, object>();
            config["pbx"] = "HUAWEI";
            config["mode"] = "continuous";
            config["filelist"] = @"D:\liukaijin_70\config-asr\input_wav\keyword_lx.list";
            config["grxml"] = "http://192.168.5.72:8080/asr_gram/qw_lx.grxml";
            config["maxruncase"] = 1;
        }

        public TestApp()
            : base(LOG_TW)
        {

        }


        public override ITestCase Case
        {
            get
            {
                if (config["mode"].Equals("continuous"))
                {
                    if (!IsRuningCaseLimit())
                        return HWContinuousTestFactory.GetInstance(config["filelist"].ToString(), config["grxml"].ToString(), 20).GetNextCase("qw_lx_test");
                }
                return null;
            }

        }

        public int RunningCaseCount
        {
            get { return runingcasecount; }
        }

        public override void IncreaseCaseCount()
        {
            runingcasecount += 1;
        }

        public override void DecreaseCaseCount()
        {
            runingcasecount -= 1;
        }

        public override bool IsRuningCaseLimit()
        {
            return runingcasecount >= Convert.ToInt32(config["maxruncase"].ToString()) ? true : false;
        }

    }
}