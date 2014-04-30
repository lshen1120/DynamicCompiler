using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace DynamicCompiler
{
    /// <summary>
    /// Remoting方式创建跨域对象
    /// </summary>
    internal class RemoteLoader : MarshalByRefObject
    {
        public RemoteLoader()
        {
        }

        private Assembly GetAsm(string assemblyFile)
        {
            //在当前域载入dll
            string asmName = System.IO.Path.GetFileNameWithoutExtension(assemblyFile);
            AppDomain.CurrentDomain.Load(asmName);
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            Assembly asm = null;

            asm = asms.First(item =>
                  Path.GetFileName(item.Location).Equals(assemblyFile, StringComparison.CurrentCultureIgnoreCase)
                );
            return asm;
        }

        private Type GetObjectType(string assemblyFile, string typeName)
        {
            Assembly asm = GetAsm(assemblyFile);
            Type[] types = asm.GetTypes();

            Type matchType = types.First(item =>
            {
                return item.FullName == typeName;
            });
            return matchType;

        }

        public T Create<T>(string assemblyFile, params object[] constructArgs)
        {
            Type parent = typeof(T);
            Assembly asm = GetAsm(assemblyFile);
            Type type = asm.GetTypes().First(item=>
                {
                    return item.GetInterfaces().Any(x => x == parent)
                        || item.IsSubclassOf(parent)
                        || item == parent;
                });
            if (type == null)
                return default(T);
            return (T)Activator.CreateInstance(type, constructArgs);
        }


        public T Create<T>(string assemblyFile, string typeName, params object[] constructArgs)
        {
            var  type = GetObjectType(assemblyFile, typeName);
            if (type == null)
                return default(T);

            return (T)Activator.CreateInstance(type,constructArgs);
        }

    }


}
