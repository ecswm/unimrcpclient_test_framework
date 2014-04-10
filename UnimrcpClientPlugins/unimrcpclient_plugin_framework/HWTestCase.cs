﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.IO;
using ucf;

namespace ucf
{

    public enum HWCaseResult : int
    {
        KEYWORD_DC_NORMAL,
        KEYWORD_DC_EXCEPTION,
        KEYWORD_DC_TIMEOUT,
        KEYWORD_LX_NORMAL,
        KEYWORD_LX_EXCEPTION,
        KEYWORD_LX_TIMEOUT,
        QW_DC_NORMAL,
        QW_DC_EXCEPTION,
        QW_DC_TIMEOUT,
        QW_LX_NORMAL,
        QW_LX_EXCEPTION,
        QW_LX_TIMEOUT
    }

    public class HWTestCase : BaseCase
    {
        public IMrcpChannel _mrcp;
        public Timer _stoptimer;
        public HWCaseResult _caseresult;

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
            set { _resultflag = value; }
        }

        public HWCaseResult caseresult
        {
            get { return _caseresult; }
            set { _caseresult = value; }
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
            ParseNLResult(msg);
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

        }

        public override void OnDestory()
        {
            base.OnDestory();
            State = false;
            _app.DecreaseCaseCount();
            _app.i(String.Format("Case {0} result: is {1}",Name,caseresult.ToString("G"))); 
        }

        public override void OnMessageReceive(IMrcpChannel channel, IMrcpMessage msg)
        {
            Dictionary<Int32, Object> ret = msg.GetFirstLine();
            int _mrcptype = Convert.ToInt32(ret[(int)MrcpConst.FIRST_LINE_type]);
            int _mrcpmethod = Convert.ToInt32(ret[(int)MrcpConst.FIRST_LINE_method_id]);
            int _mrcpreqstate = Convert.ToInt32(ret[(int)MrcpConst.FIRST_LINE_request_state]);

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
                        NotifyException();
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
                else if (_mrcpmethod == (int)MrcpEvent.RECOGNIZER_RECOGNITION_INTERMEDIA_RESULT ||
                    _mrcpmethod == (int)MrcpEvent.RECOGNIZER_RECOGNITION_COMPLETE)
                {
                    ProcessRecognizeResult(channel, msg);
                }
            }
        }

        public virtual void NotifyException()
        {

        }

        public override byte[] OnStreamRead(IMrcpChannel channel, int size, out int read)
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

        //public virtual void OnE
        public virtual void _timer_Elapsed(object sender, ElapsedEventArgs e)
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
    }
}
