using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.IO;

namespace DynamicCompiler
{

    /// <summary>
    /// 提供跨域对象创建,限制比较大,对象都是通过复制传递,但是可以把载入的程序集卸载,不会内存泄露
    /// 使用CreateInstance和GetObjectType,都会清理上次编译产生的文件和内存
    /// 直接调用Clear会清理所有动态编译文件和内存
    /// </summary>
    public class CSharpProviderEx<T>:CodeProviderBase, IDisposable
    {
        //上一次编译的信息,在下次编译前清理用
        private string m_outFile;
        private AppDomain m_newDomain;
        private List<string> m_ReferencedAssemblies;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="references">添加的引用,不需要加dll后缀</param>
        public CSharpProviderEx(params string[] references)
        {
            m_ReferencedAssemblies = new List<string>(){
                "System",
                "System.Core",
                "System.Data",
                "Microsoft.CSharp",
                "System.Data.DataSetExtensions"
            }.Union(references)
             .Select(item =>
            {
               if (item.ToLower().EndsWith(".dll") || item.ToLower().EndsWith(".exe"))
                     return item;
              return item + ".dll";
            })//有后缀不修改,无后缀添加.dll
            .ToList();
        }

        private AppDomain CreateNewDomain(string path)
        {
            AppDomainSetup appDomainSetup = new AppDomainSetup();
            appDomainSetup.LoaderOptimization = LoaderOptimization.SingleDomain;
            appDomainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            appDomainSetup.ShadowCopyDirectories = appDomainSetup.ApplicationBase;
            appDomainSetup.ShadowCopyFiles = "true";
            return AppDomain.CreateDomain(path, null, appDomainSetup);
        }

        /// <summary>
        /// 类必须继承MarshalByRefObject,并且标记Serializable特性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceFile"></param>
        /// <param name="typeFullName"></param>
        /// <param name="constructArgs"></param>
        /// <returns></returns>
        public T CreateInstance(string sourceFile, string typeFullName, params object[] constructArgs)
        {
            //保证内存不泄露
            Clear();
            m_outFile= Path.GetFileNameWithoutExtension(sourceFile) + ".dll";
            if (Compile(m_outFile,new string[]{ sourceFile}, m_ReferencedAssemblies.ToArray()))
            {
                //创建新域
                m_newDomain = CreateNewDomain(m_outFile);
                
                RemoteLoader remoteLoader = (RemoteLoader)m_newDomain.CreateInstance("DynamicCLRCompiler", "DynamicCLRCompiler.RemoteLoader").Unwrap();
                return remoteLoader.Create<T>(m_outFile, typeFullName, constructArgs);
            }
            return default(T);
        }


        public T CreateInstance(string sourceFile, params object[] constructArgs)
        {
            //保证内存不泄露
            Clear();
            m_outFile = Path.GetFileNameWithoutExtension(sourceFile) + ".dll";
            if (Compile(m_outFile, new string[] { sourceFile }, m_ReferencedAssemblies.ToArray()))
            {
                //创建新域
                m_newDomain = CreateNewDomain(m_outFile);

                RemoteLoader remoteLoader = (RemoteLoader)m_newDomain.CreateInstance("DynamicCLRCompiler", "DynamicCLRCompiler.RemoteLoader").Unwrap();
                return remoteLoader.Create<T>(m_outFile, constructArgs);
            }
            return default(T);
        }

        /// <summary>
        /// 清理内部AppDomain,会使此Provider产生的对象失效
        /// </summary>
        public void Clear()
        {
            if (m_newDomain != null)
            {
                AppDomain.Unload(m_newDomain);
                m_newDomain = null;
            }
            if (File.Exists(m_outFile))
                File.Delete(m_outFile);
            string debugFile=  Path.GetFileNameWithoutExtension(m_outFile) + ".pdb";
            if (File.Exists(debugFile))
                File.Delete(debugFile);
        }

        #region IDisposable 成员

        public void Dispose()
        {
            Clear();
        }

        #endregion
    }
}
