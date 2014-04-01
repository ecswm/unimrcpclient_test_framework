using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace AMC
{
    public class AMCMsg
    {
        public delegate void Runner();
        public int what = 0;
        public int arg1 = 0;
        public int arg2 = 0;
        public object obj = null;        
        private AMCHandler _target;
        private AMCBundle _bundle;
        internal Runner Runnable;
        internal long targetTime;
        internal bool used = false;

        public AMCMsg(int what)
        {
            this.what = what;            
        }

        public AMCMsg()
        {            
        }

        public AMCMsg(int what, AMCBundle bundle)
        {
            this.what = what;
            this.SetData(bundle);
        }

        public AMCMsg(int what, object obj)
        {            
            this.what = what;
            this.obj = obj;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetTarget(AMCHandler h)
        {
            _target = h;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public AMCHandler GetTarget()
        {
            return _target;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetData(AMCBundle b)
        {
            _bundle = b;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public AMCBundle GetData()
        {
            return _bundle;
        }        
    }
}
