using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;

namespace DynamicCompiler
{
    public class CompileException : Exception
    {
        private string[] m_SourceFiles;
        private string m_OutFile;
        private CompilerErrorCollection m_errors;

        public string[] SourceFiles
        {
            get{  return m_SourceFiles; }
        }

        public string OutFile {
            get { return m_OutFile; }
        }

        public CompilerErrorCollection Errors
        {
            get{  return m_errors; }
        }

        public CompileException(string[] sourceFiles,string outFile, CompilerErrorCollection errors)
        {
            m_OutFile = outFile;
            m_SourceFiles = sourceFiles;
            m_errors = errors;
        }


        public override string Message
        {
            get
            {
                return this.ToString();
            }
        }

        //格式化错误信息
        public override string ToString()
        {
            StringBuilder message = new StringBuilder();
            foreach (CompilerError err in m_errors)
            {
                message.AppendLine(string.Format("Line:({0},{1}) {2}:{3}", err.Line,err.Column, err.ErrorNumber, err.ErrorText));
            }
            return message.ToString();
        }
    }
}
