using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ucf
{
    public class HWBaseFactory : IHWCaseFactory
    {
        private String[] _files;
        private String _filepath;
        private String _grxml;
        private int _casecount;

        public virtual String[] Files
        {
            get { return _files; }
            set { _files = value; }
        }

        public virtual String FilePath
        {
            set { _filepath = value; }
        }

        public virtual String Grxml
        {
            set { _grxml = value; }
            get { return _grxml; }
        }

        public virtual int CaseCount
        {
            set { _casecount = value; }
        }

        public HWBaseFactory(String filepath, String grxml,int casecount)
        {
            Files = GetCaseFileName(filepath);
            Grxml = grxml;
            CaseCount = casecount;
        }

        public virtual ITestCase[] CreateHWCase()
        {
            return new ITestCase[1];
        }

        
        public String[] GetCaseFileName()
        {
            string[] files = Directory.GetFiles(_filepath);
            return files;
        }

        /// <summary>
        /// 根据文件列表获取音频文件数组
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public String[] GetCaseFileName(String path)
        {
            String line;
            List<String> files = new List<string>();
            StreamReader filepath = new StreamReader(path);
            while ((line = filepath.ReadLine()) != null)
            {
                files.Add(line);
            }
            return files.ToArray();
        }
    }

}
