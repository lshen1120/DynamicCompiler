using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.IO;
using System.Reflection;

namespace DynamicCompiler
{
    /// <summary>
    /// 编译后载入内存不能卸载,多次编译会造成内存泄露,外部应该使用缓存方式减少重新编译次数
    /// 生成对象不受任何限制,可以和.NET原生对象正常交互
    /// </summary>
    public class CSharpProvider<T>:CodeProviderBase
    {
        private List<string> m_ReferencedAssemblies;
        private string m_outFile;

        public CSharpProvider(params string[] references)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
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
            .Select(item=>
            {
                string localPath=Path.Combine(basePath,item);
                if (File.Exists(localPath))
                    return localPath;
                else
                    return item;
            })//优先使用当前目录依赖库
            .ToList();
        }

        public T CreateInstance(string sourceFile, string typeFullName, params object[] constructArgs)
        {
            var type = GetObjectType(sourceFile, typeFullName);
            if (type != null)
            {
                return  (T)Activator.CreateInstance(type, constructArgs);
            }
            return default(T);
        }

        public T CreateInstance(string sourceFile, params object[] constructArgs)
        {
            var type = GetObjectType(sourceFile);
            if (type != null)
            {
                return (T)Activator.CreateInstance(type, constructArgs);
            }
            return default(T);
        }

        public Type GetObjectType(string sourceFile, string typeFullName)
        {
            m_outFile = Path.GetFileNameWithoutExtension(sourceFile) + ".dll";
            if (Compile(m_outFile, new string[] { sourceFile }, m_ReferencedAssemblies.ToArray()))
            {
                var asm = Assembly.Load(File.ReadAllBytes(m_outFile));
                return  asm.GetTypes().Single(item => item.FullName == typeFullName);
            }
            return null;
        }

        public Type GetObjectType(string sourceFile)
        {
            string dir=Path.GetDirectoryName(sourceFile);
            string name = Path.GetFileNameWithoutExtension(sourceFile) + GetDateString();
            m_outFile = Path.Combine(dir,name+".dll");
            if (Compile(m_outFile, new string[] { sourceFile }, m_ReferencedAssemblies.ToArray()))
            {
                var asm = Assembly.Load(File.ReadAllBytes(m_outFile));
                Type parent = typeof(T);
                return asm.GetTypes().Single(item => {
                    return item.GetInterfaces().Any(x => x == parent) 
                         || item.IsSubclassOf(parent)
                         || item ==parent;
                });
            }
            return null;
        }

        /// <summary>
        /// 清理生成文件,重新生成
        /// 内存不会被清除！无法解决内存泄露
        /// 因为内存没有被清理，所以清理后之前创建的对象仍然能正常使用
        /// </summary>
        public void Clear()
        {
            if (File.Exists(m_outFile))
                File.Delete(m_outFile);
            if (!CompilerConfig.EnableDebug)
            {
                string debugFile = Path.GetFileNameWithoutExtension(m_outFile) + ".pdb";
                if (File.Exists(debugFile))
                    File.Delete(debugFile);
            }
        }

        private string GetDateString()
        {
            var date = DateTime.Now;
            int[] times = new int[] { 
                date.Year,
                date.Month,
                date.Day,
                date.Hour,
                date.Minute,
                date.Second
            };
           string dateStr=  times.Aggregate("", (seed, item) =>
           {
               string tmp = item.ToString();
               if (tmp.Length == 1)
                   tmp = "0" + tmp;
               return seed + "-" + tmp;
           });
           return dateStr;
        }
    }
}
