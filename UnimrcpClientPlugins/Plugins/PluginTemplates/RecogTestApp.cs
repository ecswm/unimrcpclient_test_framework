using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using ucf;
using System.IO;

namespace Tests
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
            d("send stopmsg");
            _mrcp = channel;
            IMrcpMessage msg = _mrcp.CreateMessage((int)MrcpMethod.RECOGNIZER_STOP);
            _mrcp.SendMessage(msg);
        }

        public override void OnChannelAdd(IMrcpChannel channel)
        {
            State = true;
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
            State = false;
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
                        //TODO Set Streaming To True
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
                        State = false;
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
                _file.Close();
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

    public class TestApp : BaseApp
    {
        static HWTestCase[] tcases;
        static TextWriter LOG_TW;
        static TestApp(){
            LOG_TW = new StreamWriter(new FileStream("testapp.log", FileMode.Create, FileAccess.Write));
        }
        public TestApp():base(LOG_TW)
        {

        }

        public override ITestCase[] Cases
        {
            get
            {
                int index = 0;
                if (tcases == null)
                {
                    tcases = new HWCcontinuousTestCase[20];
                    while (index < 20)
                    {
                        StreamWriter logStream = new StreamWriter("HW_TEST"+Convert.ToString(index),false);
                        tcases[index] = new HWCcontinuousTestCase("HW_TEST" + Convert.ToString(index), logStream);
                        tcases[index].filename = "D:\\liukaijin\\16bit8k//1946076.wav";
                        tcases[index].grxml = "http://192.168.5.72:8080/asr_gram/qw_lx.grxml";
                        index++;
                    }  
                }
                return tcases;
            }
         }

        public override bool CaseLimit()
        {
            int _iRunning = 0;
            foreach(ITestCase tcase in tcases)
            {
                if (tcase.State == true)
                    _iRunning++;
            }
            d("iRunning count: " + Convert.ToString(_iRunning));
            return _iRunning >= 19 ? true : false;
        }
    }
}
