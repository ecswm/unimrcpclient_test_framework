using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ucf;
using System.IO;

namespace Tests
{
    public class TestApp : BaseApp
    {
        static TextWriter LOG_TW;
        static TestApp(){
            LOG_TW = new StreamWriter(new FileStream("testapp.log", FileMode.Create, FileAccess.Write));
        }
        public TestApp():base(LOG_TW)
        {

        }

        public override ITestCase[] Cases
        {
            get
            {
               return new ITestCase[] { new BaseCase("dummyCase") };  
            }
        }

    }
}
