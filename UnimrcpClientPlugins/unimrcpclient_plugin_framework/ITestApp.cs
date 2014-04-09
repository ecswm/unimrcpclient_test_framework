using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ucf;
using AMC;

namespace ucf
{
    public enum LogLevel { verb = 0, info = 1, dbg = 2, warn = 3, err = 4 };
    public interface ILog
    {
        void e(string tag, string msg, Exception e);
        void e(string tag, string msg);
        void w(string tag, string msg);
        void d(string tag, string msg);
        void i(string tag, string msg);
        void v(string tag, string msg);
        void e(string msg, Exception e);
        void e(string msg);
        void w(string msg);
        void d(string msg);
        void i(string msg);
        void v(string msg);
    }
    public interface ITestApp : ILog
    {
        String Name { get; }
        ITestCase[] Cases { get; }
        ITestCase Case { get; }
        IMrcpChannelMgr ChannelMgr { get; }
        Int32 CurCaseCount { get; }
        bool CaseLimit();
        void IncreaseCaseCount();
        void DecreaseCaseCount();
        bool IsRuningCaseLimit();
        void OnCreate(IMrcpChannelMgr mgr);
        void OnDestory();        
    }
    public interface ITestCase : ILog
    {
        bool Streaming { get; }
        
        bool State { get; set;}

        String Name { get; }
        /// <summary>
        /// Command string to run in Unimrcpclient console        
        /// </summary>
        String Cmd { get; }
        /// <summary>
        /// check the condition for running this case
        /// </summary>
        /// <returns>
        /// true this case is ready for running
        /// false this case is not ready for running
        /// </returns>
        bool OnCondition();
        /// <summary>
        /// before passing the <see cref="Cmd"/>Cmd to Unimrcpclient console
        /// </summary>
        /// <returns></returns>
        void OnPreCmdRun();
        /// <summary>
        /// after passing the Cmd to Unimrcpclient console
        /// </summary>
        void OnPostCmdRun();
        //void OnChannelCreate(IMrcpChannel channel);
        //void OnChannelDestroy(IMrcpChannel channel);       
        void OnChannelAdd(IMrcpChannel channel);
        void OnChannelRemove(IMrcpChannel channel);        
        void OnMessageReceive(IMrcpChannel channel, IMrcpMessage msg);        
        byte[] OnStreamRead(IMrcpChannel channel, int size,out int read);
        void OnStreamOut(IMrcpChannel channel);
        void OnCreate(ITestApp app);
        void OnDestory();
        void SetRunningState();
    }
    public interface ITestCaseFactory
    {
        //case Count
        int CaseCount {  set; }
        //
    }

    public enum HWCaseType { single = 0, continuous = 1 };
    public interface IHWCaseFactory : ITestCaseFactory
    {
        String FilePath { set; }
        String Grxml {  set; }
        String[] GetCaseFileName();
        ITestCase[] CreateHWCase();
    }
}
