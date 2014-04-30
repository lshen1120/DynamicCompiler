using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Threading;

namespace DynamicCompiler
{

    public delegate void CompileFailedHandler(CompileException error);
    public delegate void CompileSuccessHandler(string[] sourceFiles, string outFile);


    abstract  public class CodeProviderBase
    {
        protected bool Compile(string outFile, string[] sourceFiles, params string[] references)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters cp = new CompilerParameters();

            cp.ReferencedAssemblies.AddRange(references);
            cp.GenerateExecutable = false;
            cp.OutputAssembly = outFile;
            cp.GenerateInMemory = false;
            //生成调试文件
            cp.IncludeDebugInformation = CompilerConfig.EnableDebug;
            try
            {
                CompilerResults cr = provider.CompileAssemblyFromFile(cp, sourceFiles);
                if (cr.Errors.Count > 0)
                {
                    if (OnCompileFailed != null)
                    {
                        CompileException exp = new CompileException(sourceFiles, cr.PathToAssembly, cr.Errors);
                        OnCompileFailed(exp);
                    }
                }
                else
                {
                    if (OnCompileSuccess != null)
                        OnCompileSuccess(sourceFiles, outFile);
                }
                return cr.Errors.Count == 0;
            }
            catch (Exception e) {
                if (OnCompileFailed != null)
                {
                    CompilerErrorCollection t = new CompilerErrorCollection();
                    t.Add(new CompilerError() { ErrorText = e.Message });
                    CompileException exp = new CompileException(sourceFiles, outFile, t);
                    OnCompileFailed(exp);
                }
                return false;
            }

        }

        /// <summary>
        /// 编译失败触发
        /// </summary>
        public event CompileFailedHandler OnCompileFailed;
        /// <summary>
        /// 编译成功触发
        /// </summary>
        public event CompileSuccessHandler OnCompileSuccess;
    }
}
