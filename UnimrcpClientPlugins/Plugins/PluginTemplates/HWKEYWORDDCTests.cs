using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Timers;
using ucf;

namespace HWKEYWORDDCTests
{
    public class KeywordDcTestCase : HWTestCase
    {
        public KeywordDcTestCase(String name)
            : base(name)
        {

        }

        public KeywordDcTestCase(String name, StreamWriter logStream)
            : base(name, logStream)
        {

        }

        public override void ProcessRecognizeResult(IMrcpChannel channel, IMrcpMessage msg)
        {
            d("channel id: " + channel.GetChannelId() + " recv recognizer_recognition_complete");
            ParseNLResult(msg);
            resultflag = true;  
        }

        public override void ParseNLResult(IMrcpMessage msg)
        {
            d("completion_cause: " + msg.GetHeader((int)MrcpConst.RECOGNIZER_HEADER_COMPLETION_CAUSE));
            d(msg.GetBody());
            caseresult = HWCaseResult.KEYWORD_DC_NORMAL;
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
                    caseresult = HWCaseResult.KEYWORD_DC_TIMEOUT;
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
            caseresult = HWCaseResult.KEYWORD_DC_EXCEPTION;
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
            KeywordDcTestCase tcase = new KeywordDcTestCase(casename, logStream);
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
                KeywordDcTestCase[] tcases = new KeywordDcTestCase[files.Length];
                int caseindex = 0;
                string casename = "HWTEST";
                foreach (string file in files)
                {
                    StreamWriter logStream = new StreamWriter(casename + Convert.ToString(caseindex));
                    tcases[caseindex] = new KeywordDcTestCase(casename + Convert.ToString(caseindex), logStream);
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
        static volatile int runingcasecount;
        static TextWriter LOG_TW;
        static Dictionary<string, object> config;

        static TestApp(){
            LOG_TW = new StreamWriter(new FileStream("testapp.log", FileMode.Create, FileAccess.Write));
            config = new Dictionary<string, object>();
            config["pbx"] = "HUAWEI";
            config["mode"] = "keyword_dc";
            config["filelist"] = @"D:\liukaijin_70\config-asr\input_wav\keyword_lx.list";
            config["grxml"] = "http://192.168.5.72:8080/asr_gram/keyword_dc.grxml";
            config["maxruncase"] = 1;
        }

        public TestApp():base(LOG_TW)
        {
          
        }


        public override ITestCase Case
        {
            get
            {
                if (config["mode"].Equals("keyword_dc"))
                {
                    if(!IsRuningCaseLimit())
                        return HWSingleTestFactory.GetInstance(config["filelist"].ToString(), config["grxml"].ToString(), 20).GetNextCase("keyword_dc_test");
                }
                return null;
            }
           
        }

        public override Int32 CurCaseCount
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

        public override  bool IsRuningCaseLimit()
        {
            return runingcasecount >= Convert.ToInt32(config["maxruncase"].ToString()) ? true : false;
        }

    }
}