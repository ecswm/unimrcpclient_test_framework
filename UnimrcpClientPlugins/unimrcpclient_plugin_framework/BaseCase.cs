using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ucf;
using System.IO;

namespace ucf
{
    public class BaseCase : ITestCase
    {
        string _name;
        Logger _logger;
        ITestApp _app;

        public virtual string Name
        {
            get { return _name; }
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


        public string Cmd
        {
            get { return ""; }
        }

        public bool OnCondition()
        {
            if (_app.ChannelMgr.Count() < 5)
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

        public void OnChannelAdd(IMrcpChannel channel)
        {
            i(String.Format("OnChannelAdd({0})", channel));
        }

        public void OnChannelRemove(IMrcpChannel channel)
        {
            i(String.Format("OnChannelRemove({0})", channel));
        }

        public void OnMessageReceive(IMrcpChannel channel, IMrcpMessage msg)
        {
            i(String.Format("OnMessageRecive({0})", channel));
        }

        public byte[] OnStreamRead(IMrcpChannel channel, int size)
        {
            return new byte[0];
        }

        public void OnStreamOut(IMrcpChannel channel)
        {
            i(String.Format("OnStreamOut({0})", channel));
        }
    }
}
