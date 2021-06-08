using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Jitex.Utils
{
    internal static class StackHelper
    {
        public static MethodBase? GetSourceCall(params Assembly[] assemblyToIgnore)
        {
            StackTrace trace = new(1, false);
            
            IEnumerable<MethodBase> methods = trace.GetFrames()
                .Select(frame => frame.GetMethod())
                .Where(method => !assemblyToIgnore.Contains(method.DeclaringType.Assembly));

            if (!methods.Any())
                return null;

            return methods.FirstOrDefault();
        }
    }
}