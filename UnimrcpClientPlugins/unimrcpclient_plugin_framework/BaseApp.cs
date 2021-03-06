﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.JScript.Vsa;

namespace ucf
{
    public class BaseApp : ITestApp
    {
        String _name;
        static volatile int runingcasecount = 0;
        static volatile int totalcasecount = 0;
        IMrcpChannelMgr _channelMgr;
        Logger _logger;
        public BaseApp(){
            _logger = new Logger(Name, Console.Out);
        }

        public BaseApp(TextWriter logStream)
        {
            _logger = new Logger(Name, logStream);
        }

        public virtual string Name
        {
            get { return "BaseApp"; }
        }        

        public virtual ITestCase[] Cases
        {
            get { return new ITestCase[1]; }
        }

        public virtual ITestCase Case
        {
            get 
            {
                return null;
            }
        }

        public virtual Int32 CurCaseCount
        {
            get { return runingcasecount; }
        }

        public virtual Int32 TotalCaseCount
        {
            get { return totalcasecount; }
            set { totalcasecount = value; }
        }

        public virtual bool IsRuningCaseLimit()
        {
            return true;
        }

        public virtual void IncreaseCaseCount()
        {
            runingcasecount += 1;
        }

        public virtual void DecreaseCaseCount()
        {
            runingcasecount -= 1;
        }

        public virtual void OnCreate(IMrcpChannelMgr mgr)
        {
            _channelMgr = mgr;
            d(String.Format("OnCreate({0})", _channelMgr.GetHashCode()));
        }

        public IMrcpChannelMgr ChannelMgr
        {
            get { return _channelMgr; }
        }

        public virtual void OnDestory()
        {
            d(String.Format("OnDestory({0})", _channelMgr.GetHashCode()));
        }

        public virtual void OnCaseFailed(ITestCase tcase,String failmsg)
        {
            i(String.Format("OnCaseFailed <{0}>,fail:<{1}>",tcase.Name,failmsg));
        }

        public virtual void OnCaseSuccess(ITestCase tcase)
        {
            i(String.Format("OnCaseSuccess <{0}>", tcase.Name));
        }

        public virtual void GenerateRepo()
        {

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
    }
}
