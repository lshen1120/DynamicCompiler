using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace DynamicCompiler
{
    /// <summary>
    /// 内部使用CSharpProviderEx提供对象创建
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AutoCompilerEx<T>:IDisposable
    {
        Timer m_timer;
        string m_sourceFile;
        string m_className;
        string[] m_references;
        string m_watchPath;
        FileSystemWatcher filewatch;
        CSharpProviderEx<T> provider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceFile">源文件名,不需要绝对路径</param>
        /// <param name="className"></param>
        /// <param name="path"></param>
        /// <param name="references"></param>
        public AutoCompilerEx(string sourceFile,string className,params string[] references)
        {
            m_sourceFile = sourceFile;
            m_className = className;
            m_references = references;
            m_watchPath = Path.GetDirectoryName(Path.GetFullPath( m_sourceFile));
        }

        private void Init()
        {
            m_timer = new Timer(new TimerCallback(timerCallback));
            filewatch = new FileSystemWatcher(m_watchPath,m_sourceFile);
            filewatch.Changed += new FileSystemEventHandler(filewatch_Changed);
            filewatch.Renamed += filewatch_Changed;
            provider = new CSharpProviderEx<T>(m_references);
            provider.OnCompileFailed += CompileFailed;
        }

        public void Start()
        {
            Init();
            filewatch.EnableRaisingEvents = true;
            timerCallback(null);
        }

        public void Stop()
        {
            filewatch.EnableRaisingEvents = false;
        }


        void filewatch_Changed(object sender, FileSystemEventArgs e)
        {
            m_timer.Change(300, Timeout.Infinite);
        }

        private void timerCallback(object state)
        {
            var instance= provider.CreateInstance(m_sourceFile, m_className);
            if (instance != null)
            {
                if (CompileComplate != null)
                    CompileComplate(instance);
            }
        }

        public event CompileFailedHandler CompileFailed;

        public event Action<T> CompileComplate;

        #region IDisposable 成员

        public void Dispose()
        {
            if (provider != null)
                provider.Clear();
        }

        #endregion
    }
}
