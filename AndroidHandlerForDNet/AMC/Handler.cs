using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace AMC
{
    public class AMCHandler
    {
        private AMCLooper _looper = null;

        public AMCHandler(AMCLooper looper)
        {
            _looper = looper;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void HandleMessage(AMCMsg message) {            
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Post(AMCMsg.Runner runnable)
        {
            AMCMsg msg = new AMCMsg();
            msg.SetTarget(this);
            msg.Runnable = runnable;
            msg.targetTime = Util.NowMs;
            Send(msg);
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PostDelayed(AMCMsg.Runner runnable, long delay)
        {
            AMCMsg msg = new AMCMsg();
            msg.targetTime = Util.DelayMs(delay);
            msg.SetTarget(this);
            msg.Runnable = runnable;
            SendAtTime(msg, msg.targetTime);
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Send(AMCMsg message)
        {
            SendAtTime(message, Util.NowMs);
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendAtTime(AMCMsg message, long time)
        {
            message.targetTime = time;
            message.SetTarget(this);
            _looper.Add(message);
            //Debug.WriteLine("send message at " + message.targetTime);
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Send(AMCMsg message, long delay)
        {            
            SendAtTime(message, Util.DelayMs(delay));            
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Remove(int what)
        {
            _looper.Remove(this, what);                        
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Remove(Delegate runnable)
        {
            _looper.Remove(this, runnable);
        }
        [MethodImpl(MethodImplOptions.Synchronized)]        
        public bool Has(int what)
        {
            return _looper.Has(this, what);
        }
    }
}
