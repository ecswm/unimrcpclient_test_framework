#define debug

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using ucf;
using System.IO;
using System.Configuration;
using System.Net;
using System.Net.Sockets;

namespace Tests_Total
{

    public class HWTestCase : BaseCase
    {
        public IMrcpChannel _mrcp;
        public Timer _stoptimer;
        
        String _filename;
        String _grxml;
        volatile Boolean _resultflag;
        FileStream _file;
        volatile Boolean _open;
        
        public String filename
        {
            get { return _filename; }
            set { _filename = value; }
        }

        public String grxml
        {
            get { return _grxml; }
            set { _grxml = value; }
        }

        public Boolean resultflag
        {
            get { return _resultflag; }
            set { _resultflag = value;}
        }

        public HWTestCase(String name)
            : base(name)
        {
            _open = false;
            Streaming = false;
            _stoptimer = new Timer(15 * 1000);
            _stoptimer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
        }

        public HWTestCase(String name, StreamWriter logStream)
            : base(name, logStream)
        {
            _open = false;
            Streaming = false;
            _stoptimer = new Timer(15 * 1000);
            _stoptimer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
        }

        //fill messagebody
        public String GetMessageBody()
        {
            return _grxml;
        }

        public override string Cmd
        {
            get { return "run recog"; }
        }

        public virtual void ProcessRecognizeResult(IMrcpChannel channel, IMrcpMessage msg) 
        {

        }

        //create stop message and send to server
        public void SendStopChannel(IMrcpChannel channel)
        {
            d("send stopchannel msg");
            _mrcp = channel;
            IMrcpMessage msg = _mrcp.CreateMessage((int)MrcpMethod.RECOGNIZER_STOP);
            _mrcp.SendMessage(msg);
        }

        public override void OnChannelAdd(IMrcpChannel channel)
        {
            d("current global channel count:" + _app.ChannelMgr.Count());
            d("current channel-id:" + channel.GetChannelId());
            _mrcp = channel;
            IMrcpMessage msg = _mrcp.CreateMessage((int)MrcpMethod.RECOGNIZER_RECOGNIZE);
            msg.SetHearder((int)MrcpConst.GENERIC_HEADER_CONTENT_TYPE, "text/uri-list");
            msg.SetHearder((int)MrcpConst.GENERIC_HEADER_CONTENT_ID, "request1@form-level.store");
            if (msg.GetFirstLine()[(int)MrcpConst.FIRST_LINE_version].Equals(2))
            {
                msg.SetHearder((int)MrcpConst.RECOGNIZER_HEADER_CANCEL_IF_QUEUE, "false");
            }
            msg.SetHearder((int)MrcpConst.RECOGNIZER_HEADER_NO_INPUT_TIMEOUT, "5000");
            msg.SetHearder((int)MrcpConst.RECOGNIZER_HEADER_RECOGNITION_TIMEOUT, "10000");
            msg.SetHearder((int)MrcpConst.RECOGNIZER_HEADER_START_INPUT_TIMERS, "true");
            msg.SetHearder((int)MrcpConst.RECOGNIZER_HEADER_CONFIDENCE_THRESHOLD, "0.87");
            msg.SetBody(this.GetMessageBody());
            _mrcp.SendMessage(msg);
        }

        public override void OnChannelRemove(IMrcpChannel channel)
        {
            //OnDestory();
            State = false;
            _app.DecreaseCaseCount();
        }

        public override void OnMessageReceive(IMrcpChannel channel, IMrcpMessage msg)
        {
            Dictionary<Int32, Object> ret  = msg.GetFirstLine();
            int _mrcptype = Convert.ToInt32(ret[(int)MrcpConst.FIRST_LINE_type]);
            int _mrcpmethod = Convert.ToInt32(ret[(int)MrcpConst.FIRST_LINE_method_id]);
            int _mrcpreqstate =Convert.ToInt32(ret[(int)MrcpConst.FIRST_LINE_request_state]);

            if (_mrcptype == (int)MrcpMsgType.MRCP_MESSAGE_TYPE_RESPONSE)
            {
                if (_mrcpmethod == (int)MrcpMethod.RECOGNIZER_RECOGNIZE)
                {
                    if (_mrcpreqstate == (int)MrcpReqState.MRCP_REQUEST_STATE_INPROGRESS)
                    {
                        //Set Streaming To True
                        d("channel id: " + channel.GetChannelId() + " recv mrcp_request_state_inprogress");
                        Streaming = true;
                    }
                    else
                    {
                        d("channel id: " + channel.GetChannelId() + " current channel recv unexpect response");
                        //Send Remove channel msg
                        channel.SendRemoveChannel();
                    }
                }
                else if (_mrcpmethod == (int)MrcpMethod.RECOGNIZER_STOP)
                {
                    if (_mrcpreqstate == (int)MrcpReqState.MRCP_REQUEST_STATE_COMPLETE)
                    {
                        d("channel id: " + channel.GetChannelId() + " current channel recv mrcp_request_state_complete for recog stop");
                        channel.SendRemoveChannel();
                    }
                }
            }
            else if (_mrcptype == (int)MrcpMsgType.MRCP_MESSAGE_TYPE_EVENT)
            {
                if (_mrcpmethod == (int)MrcpEvent.RECOGNIZER_START_OF_INPUT)
                {
                    d("channel id: " + channel.GetChannelId() + " recv recognizer_start_of_input");
                }
                else if (_mrcpmethod == (int)MrcpEvent.RECOGNIZER_RECOGNITION_INTERMEDIA_RESULT||
                    _mrcpmethod == (int)MrcpEvent.RECOGNIZER_RECOGNITION_COMPLETE)
                {      
                    ProcessRecognizeResult(channel, msg);
                }
            }
        }

        public override byte[] OnStreamRead(IMrcpChannel channel, int size,out int read)
        {
            byte[] buffer = new byte[size];
            if (!_open)
                _file = new FileStream(_filename, FileMode.Open, FileAccess.Read);
            read = _file.Read(buffer, 0, buffer.Length);
            _open = true;
            return buffer;
        }

        public override void OnStreamOut(IMrcpChannel channel)
        {
            Streaming = false;
            if (_open)
            {
                _open = false;
                _file.Close();
            }
            //set timer to check last result            
            StartTimer();
        }

        public virtual void StartTimer()
        {
            lock (_stoptimer)
            {
               if (!_stoptimer.Enabled)
               {
                   _stoptimer.Start();
               }
            }
        }

        public virtual void ParseNLResult(IMrcpMessage msg)
        {
            d(msg.GetBody());
        }

        public virtual void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_stoptimer)
            {
                //RECOGNIZER_RECOGNITION_COMPLETE result
                if (resultflag)
                {
                    //send stop message
                    SendStopChannel(_mrcp);
                    var timer = (Timer)sender;
                    timer.Stop();
                }
                else
                {
                    //Timeout log
                    d("channle id :" + _mrcp.GetChannelId() + " timeout!!!");
                }
            }
        }
    }

    public class HWSingleTestCase : HWTestCase
    {

        public HWSingleTestCase(String name):base(name)
        {

        }

        public HWSingleTestCase(String name, StreamWriter logStream) : base(name,logStream)
        {

        }

        public override void ProcessRecognizeResult(IMrcpChannel channel,IMrcpMessage msg)
        {
            d("channel id: " + channel.GetChannelId() + " recv recognizer_recognition_complete");
            ParseNLResult(msg);
            resultflag = true;
        }             
    }

    public class HWCcontinuousTestCase : HWTestCase
    {
        public HWCcontinuousTestCase(String name)
            : base(name)
        {
      
        }

        public HWCcontinuousTestCase(String name, StreamWriter logStream)
            : base(name, logStream)
        {

        }

        public override void ProcessRecognizeResult(IMrcpChannel channel,IMrcpMessage msg)
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
  
    }

    public class HWSingleTestFactory : HWBaseFactory
    {
        private static HWSingleTestFactory instance = null;
        private static Int32 curcaseindex = 0;
        private static Int32 casecount = 0;

        private HWSingleTestFactory(String filepath, String grxml, Int32 casecount)
            : base(filepath, grxml, casecount)
        {
            
        }

        public static HWSingleTestFactory GetInstance(String filepath,String grxml,Int32 casecount)
        {
            if (instance == null)
            {
                return new HWSingleTestFactory(filepath, grxml, casecount);
            }
            return instance;
        }

        public virtual ITestCase GetNextCase()
        {
            if (curcaseindex == Files.Length)
               curcaseindex = 0;
            String casename = "HW_TEST"+Convert.ToString(casecount);
            StreamWriter logStream = new StreamWriter("HW_TEST" + Convert.ToString(casecount), false);
            HWSingleTestCase tcase = new HWSingleTestCase(casename, logStream);
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
                HWSingleTestCase[] tcases = new HWSingleTestCase[files.Length];
                int caseindex = 0;
                string casename = "HWTEST";
                foreach (string file in files)
                {
                    StreamWriter logStream = new StreamWriter(casename +  Convert.ToString(caseindex));
                    tcases[caseindex] = new HWSingleTestCase(casename + Convert.ToString(caseindex), logStream);
                    tcases[caseindex].filename = file;
                    tcases[caseindex].grxml = Grxml;
                    caseindex++;
                }
                return tcases;
            }
            return null;
        }
        
    }

    public class TestApp : BaseApp
    {
        static String pbx;
        static String filelist;
        static String grxml;
        static String testmode;
        static Int32 maxruncase = 20;
        static volatile int runingcasecount;
        static TextWriter LOG_TW;
        static Dictionary<string, object> config;

        static TestApp(){
            LOG_TW = new StreamWriter(new FileStream("testapp.log", FileMode.Create, FileAccess.Write));
            config = new Dictionary<string, object>();
            config["pbx"] = "HUAWEI";
            config["mode"] = "single";
            config["filelist"] = @"D:\liukaijin_70\config-asr\70.list";
            config["grxml"] = "http://192.168.5.72:8080/asr_gram/qw_dc.grxml";
            config["maxruncase"] = 20;
            config["exitserver"] = true;
        }

        public TestApp():base(LOG_TW)
        {
          
        }


        public override ITestCase Case
        {
            get
            {
                if (config["mode"].Equals("single"))
                {
                    if(!IsRuningCaseLimit())
                        return HWSingleTestFactory.GetInstance(config["filelist"].ToString(), config["grxml"].ToString(), 20).GetNextCase();
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

        public override  bool IsRuningCaseLimit()
        {
            return runingcasecount >= Convert.ToInt32(config["maxruncase"].ToString()) ? true : false;
        }

        public override void OnDestory()
        {
            if (config.ContainsKey("exitserver") && (bool)config["exitserver"])
            {
                try
                {
                    //tell server to exit
                    IPEndPoint remote = new IPEndPoint(IPAddress.Parse("192.168.5.63"), 12345);
                    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sock.Connect(remote);
                    byte[] data = UTF8Encoding.UTF8.GetBytes("exit\n");
                    sock.Send(data);
                    sock.Close();
                }
                catch (Exception e)
                {
                    d(e.ToString());
                }
            }
            base.OnDestory();
        }
    }

}
