using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Jitex.Utils
{
    internal static class StackHelper
    {
        public static MethodBase? GetSourceCall(params Type[] typesToIgnore)
        {
            StackTrace trace = new StackTrace();
            StackFrame[]? frames = trace.GetFrames();

            if (frames == null)
                return null;

            MethodBase? source = null;

            foreach (StackFrame frame in frames)
            {
                MethodBase method = frame.GetMethod();
                
                if(typesToIgnore.Contains(method.DeclaringType))
                    continue;

                source = frame.GetMethod();
                break;
            }

            return source;
        }
    }
}