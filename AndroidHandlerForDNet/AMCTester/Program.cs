using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using AMC;
using System.Diagnostics;

namespace AMCTester
{
    class Program
    {
        static AMCHandlerThread T1;

        static void Main(string[] args)
        {
            T1 = new AMCHandlerThread("T1");
            T1.Start();
            AMCLooper lp = T1.getLooper();
            Handler1 h1 = new Handler1(lp);
            h1.Send(new AMCMsg(1));
            h1.Send(new AMCMsg(2), 1000);
            h1.Send(new AMCMsg(3, "Hello Handler."));
            AMCBundle bundle = new AMCBundle();
            bundle.put("key", "THE VALUE");
            h1.Send(new AMCMsg(4, bundle));
            Thread.Sleep(2000);
            T1.Stop();
            T1.Join();
        }
    }

    class Handler1 : AMCHandler
    {
        public Handler1(AMCLooper lp) : base(lp) { }
        public override void HandleMessage(AMCMsg msg)
        {            
            Debug.WriteLine("Handler1 to process message " + msg.what + " at ticket " + 
                Util.GetMilliseconds(DateTime.Now.Ticks));
            switch (msg.what)
            {
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    Debug.WriteLine("3:" + msg.obj);
                    break;
                case 4:
                    Debug.WriteLine("4:" + msg.GetData().get<String>("key", null));
                    break;
                default:
                    Debug.WriteLine("Unknown message " + msg.what);
                    break;
            }
        }
    }
}
