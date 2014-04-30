using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace DynamicCompiler
{
    /// <summary>
    /// 内部使用CSharpProvider创建对象,
    /// 保存一个对象实例,在更新成功时触发CompileComplate事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AutoCompiler<T>
    {
        Timer m_timer;
        string m_sourceFile;
        string m_className;
        string[] m_references;
        string m_watchPath;

        FileSystemWatcher filewatch;
        CSharpProvider<T> provider;
        Type m_type;
        /// <summary>
        /// 是否在生成对象后自动清理编译产生的文件
        /// </summary>
        public bool AutoClean { get; set; }

        public AutoCompiler(string sourceFile, string className, params string[] references)
            :this(sourceFile,references)
        {
            m_className = className;
        }


        public AutoCompiler(string sourceFile, params string[] references)
        {
            AutoClean = true;
            m_sourceFile = sourceFile;
            m_references = references;
            m_watchPath = Path.GetDirectoryName(Path.GetFullPath(m_sourceFile));
        }

        private void Init()
        {
            if (filewatch != null)
                filewatch.EnableRaisingEvents = false;

            m_timer = new Timer(new TimerCallback(timerCallback));
            filewatch = new FileSystemWatcher(m_watchPath,Path.GetFileName(m_sourceFile));
            filewatch.Changed += new FileSystemEventHandler(FileChanged);
            filewatch.Renamed += FileChanged;
            provider = new CSharpProvider<T>(m_references);
            provider.OnCompileFailed += OnCompileFailed;
        }

        T instance;
        /// <summary>
        /// 如果文件未更新,不重新编译创建对象,可以减缓内存泄露
        /// </summary>
        /// <returns></returns>
        public T GetCache()
        {
            if (instance == null)
            {
                Init();
                RefreshInstance();
            }
            return instance;
        }

        public void Start()
        {
            //已经开启监视
            if (filewatch != null && filewatch.EnableRaisingEvents)
                return;
            Init();
            filewatch.EnableRaisingEvents = true;
            timerCallback(null);
        }

        public void Stop()
        {
            filewatch.EnableRaisingEvents = false;
        }


        void FileChanged(object sender, FileSystemEventArgs e)
        {
            m_timer.Change(300, Timeout.Infinite);
            //Console.WriteLine(DateTime.Now.ToString()+ " 5秒后开始编译文件 "+m_sourceFile);
        }


        bool RefreshInstance()
        {
            var preType = m_type;//保存当前Type
            if (string.IsNullOrEmpty(m_className))//不指定类名实查找第一个实现接口类
            {
                m_type = provider.GetObjectType(m_sourceFile);
                if (AutoClean)
                    provider.Clear();
            }
            else
            {
                m_type = provider.GetObjectType(m_sourceFile, m_className);
                if (AutoClean)
                    provider.Clear();
            }
            if (m_type == null)//编译失败，恢复到上次
            {
                m_type = preType;
                return false;
            }
            else
                try
                {
                    instance = (T)Activator.CreateInstance(m_type);
                }
                catch (Exception e)
                {
                    if (OnCreateFailed != null)
                        OnCreateFailed(e);
                }
            return true;
        }
        private void timerCallback(object state)
        {
            var preInstance = instance;
            if (RefreshInstance() && instance !=null)
            {
                if (OnInstanceRefreshed != null)
                    OnInstanceRefreshed(preInstance,instance);
            }
        }

        public event Action<Exception> OnCreateFailed;
        public event CompileFailedHandler OnCompileFailed;

        public delegate void InstanceRefreshedHandler(T prev,T current);
        public event InstanceRefreshedHandler OnInstanceRefreshed;
    }
}
