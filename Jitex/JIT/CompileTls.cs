﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Jitex.JIT
{
    internal class CompileTls
    {
        public int EnterCount;

        /// <summary>
        /// Get source from call
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodBase? GetSource()
        {
            StackTrace stack = new(false);

            StackFrame[]? frames = stack.GetFrames();

            if (frames == null)
                return null;

            MethodBase? method = null;

            if (frames.Length >= 2)
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    StackFrame frame = frames[i];
                    MethodBase tempMethod = frame.GetMethod();

                    if (tempMethod.Module == typeof(CompileTls).Module)
                        continue;

                    if (i == frames.Length - 1)
                        method = frames[i].GetMethod();
                    else
                        method = frames[i + 1].GetMethod();

                    break;
                }
            }
            else
            {
                foreach (StackFrame frame in frames)
                {
                    MethodBase tempMethod = frame.GetMethod();

                    if (tempMethod.Module == typeof(CompileTls).Module)
                        continue;

                    method = tempMethod;
                }
            }

            return method;

            // for (int i = 0; i < frames.Length; i++)
            // {
            //     StackFrame frame = frames[i];
            //     MethodBase method = frame.GetMethod();
            //     
            //     if(method)
            // }

            // MethodBase[] trace = frames.Select(m => m.GetMethod())
            //     .ToArray();
            //
            // return trace.Length >= 2 ? trace.ElementAt(1) : trace.LastOrDefault();
            //
            // return null;

            return method;
        }
    }
}