using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Diagnostics;

namespace ucf
{
    class Logger : ILog
    {
        enum LogLevel { verb = 0, info = 1, dbg = 2, warn = 3, err = 4 };
        string _tag = "";
        private StringBuilder _sb = new StringBuilder();
        private TextWriter _swLogger;

        public Logger(string defaultTag, TextWriter sw)
        {
            _tag = defaultTag;
            _swLogger = sw;
        }
        void _log(LogLevel level, string msg)
        {
            _log(level, _tag, msg);
        }
        
        void _log(LogLevel level, string tag, string msg)
        {

            lock (_swLogger)
            {
                int tid = Thread.CurrentThread.ManagedThreadId;
                string timestamp = DateTime.UtcNow.ToString("MM-dd HH:mm:ss.fff",
                                            CultureInfo.InvariantCulture);
                _sb.Clear();
                _sb.Append(timestamp);
                _sb.Append(" ");
                _sb.Append(tid);
                _sb.Append('\t');
                _sb.Append(level);
                _sb.Append('\t');
                _sb.Append(tag);
                _sb.Append('\t');
                _sb.Append(msg);
                string str = _sb.ToString();
                _swLogger.WriteLine(str);
                Console.Out.WriteLine(str);
                Debug.WriteLine(str);
            }
        }
        public void e(string tag, string msg, Exception e)
        {
            _log(LogLevel.err, tag, msg + e.ToString());
        }
        public void e(string tag, string msg)
        {
            _log(LogLevel.err, tag, msg);
        }
        public void w(string tag, string msg)
        {
            _log(LogLevel.warn, tag, msg);
        }
        public void d(string tag, string msg)
        {
            _log(LogLevel.dbg, tag, msg);
        }
        public void i(string tag, string msg)
        {
            _log(LogLevel.info, tag, msg);
        }
        public void v(string tag, string msg)
        {
            _log(LogLevel.verb, tag, msg);
        }

        public void e(string msg, Exception e)
        {
            _log(LogLevel.err,  msg + e.ToString());
        }
        public void e(string msg)
        {
            _log(LogLevel.err, msg);
        }
        public void w(string msg)
        {
            _log(LogLevel.warn, msg);
        }
        public void d(string msg)
        {
            _log(LogLevel.dbg, msg);
        }
        public void i(string msg)
        {
            _log(LogLevel.info, msg);
        }
        public void v(string msg)
        {
            _log(LogLevel.verb, msg);
        }
        internal void Finish()
        {
            if (_swLogger != null && _swLogger != Console.Out)
            {
                lock (_swLogger)
                {
                    _swLogger.Close();
                    _swLogger = null;
                }
            }            
        }
    }
}
