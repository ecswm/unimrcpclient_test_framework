using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AMC
{
    public class AMCLooper
    {
        private List<AMCMsg> _messages = new List<AMCMsg>();
        private volatile bool _started = false;
        private Thread _thread;
        private static ThreadLocal<AMCLooper> _locals = new ThreadLocal<AMCLooper>();

        private AMCLooper(Thread t)
        {
            if (t.Equals(Thread.CurrentThread))
            {
                if (_locals.IsValueCreated)
                {
                    throw new ArgumentException("another AMCLooper has been attached to the thread " + t);
                }
            }
            _thread = t;
        }

        public static void loop()
        {
            if (!_locals.IsValueCreated)
            {
                throw new InvalidOperationException("thread " + Thread.CurrentThread + " has not been prepare()ed");
            }
            AMCLooper THIS = _locals.Value;
            while(THIS._started)
            {
                AMCMsg msg = THIS.Poll();
                if (msg == null)
                {
                    break;
                }
                if (msg.Runnable != null)
                {
                    msg.Runnable();
                }
                else if(msg.GetTarget() != null)
                {
                    msg.GetTarget().HandleMessage(msg);
                }
            }
        }

        public void quit()
        {
            lock (_messages)
            {
                _started = false;
                Monitor.Pulse(_messages);
            }
        }

        public Thread GetThread()
        {
            return _thread;
        }

        public static void prepare()
        {
            if (_locals.IsValueCreated)
            {
                throw new ArgumentException("another AMCLooper has been attached to the thread " + Thread.CurrentThread);
            }            
            _locals.Value = new AMCLooper(Thread.CurrentThread);
            _locals.Value._started = true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void Remove(AMCHandler aMCHandler, Delegate runnable)
        {
            lock (_messages)
            {
                for (int i = 0; i < _messages.Count; i++)
                {
                    if (_messages[i].GetTarget().Equals(aMCHandler)
                        && _messages[i].Runnable.Equals(runnable))
                    {
                        _messages.RemoveAt(i);                            
                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void Remove(AMCHandler aMCHandler, int what)
        {
            lock (_messages)
            {
                bool removed = false;
                for (int i = 0; i < _messages.Count; i++)
                {
                    if (_messages[i].GetTarget().Equals(aMCHandler)
                        && _messages[i].what == what)
                    {
                        _messages.RemoveAt(i);
                        removed = true;
                    }
                }
                if (removed)
                {
                    Monitor.PulseAll(_messages);
                }
            } 
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal bool Has(AMCHandler aMCHandler, int what)
        {
            lock (_messages)
            {
                for (int i = 0; i < _messages.Count; i++)
                {
                    if (_messages[i].GetTarget().Equals(aMCHandler)
                        && _messages[i].what == what)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal void Add(AMCMsg message)
        {
            if (message.used)
            {
                throw new InvalidOperationException("one AMCMsg can not be add to queue multiple times");
            }
            lock(_messages){
                message.used = true;
                _messages.Add(message);
                Monitor.PulseAll(_messages);
            }
        }

        public AMCMsg Poll()
        {
            AMCMsg ret = null;
            Monitor.Enter(_messages);
            if (!_started) return null;
            while (_messages.Count <= 0)
            {
                Monitor.Wait(_messages);
                if (!_started) return null;            
            }
        FIND_AGAIN:
            int count = _messages.Count;
            long firstMessageTime = long.MaxValue;
            long now = Util.GetMilliseconds(DateTime.Now.Ticks);
            foreach(AMCMsg m in _messages)
            {
                if (firstMessageTime > m.targetTime)
                {
                    firstMessageTime = m.targetTime;
                    ret = m;                    
                }                
            }
            if (firstMessageTime >= now)
            {
                if (Monitor.Wait(_messages, (int)(firstMessageTime - now)))
                {
                    if (!_started) return null;
                    if(_messages.Count != count)
                        goto FIND_AGAIN;
                }
                if (!_started) return null;            
            }
            if(ret != null)
                _messages.Remove(ret);            
            Monitor.Exit(_messages);
            return ret;
        }

        public static AMCLooper myLooper()
        {
            return _locals.Value;
        }
    }
}
