using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AMC
{
    public class AMCHandlerThread
    {
        private Thread _thread;
        private Object _looperLock = new Object();
        private AMCLooper _looper = null;
        private volatile bool _stop = false;

        public AMCHandlerThread(String name)
        {
            _thread = new Thread(_run);
            _thread.Name = name;
        }

        public void Start()
        {
            _thread.Start();
        }

        private void _run()
        {
            AMCLooper.prepare();
            lock (_looperLock)
            {
                _looper = AMCLooper.myLooper();
                Monitor.Pulse(_looperLock);
            }
            AMCLooper.loop();
        }

        public void Stop()
        {
            _stop = true;
            if (_looper != null)
            {
                _looper.quit();
            }
        }

        public void Join()
        {
            _thread.Join();
        }

        public AMCLooper getLooper()
        {
            if(!_thread.IsAlive)
                return null;
            lock (_looperLock)
            {
                while(_looper == null){
                    Monitor.Wait(_looperLock);
                }
                return _looper;
            }
        }
    }
}
