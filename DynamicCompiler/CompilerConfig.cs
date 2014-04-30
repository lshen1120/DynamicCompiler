using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicCompiler
{
    class CompilerConfig
    {
        static CompilerConfig()
        {
#if DEBUG
            EnableDebug = true;
#else
            EnableDebug=false;
#endif
        }
        public static bool EnableDebug { get; set; }
    }
}
