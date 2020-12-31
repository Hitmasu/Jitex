using System;
using System.Reflection;

namespace Jitex.Intercept
{
    public class CallContext
    {
        private object _returnValue;
        public MethodBase Method { get; }
        private Delegate Call { get; }
        public object Instance { get; set; }
        public object[] Args { get; set; }

        public bool IsReturnSetted { get; private set; }

        public object ReturnValue
        {
            get => _returnValue;
            set
            {
                _returnValue = value;
                IsReturnSetted = true;
            }
        }

        public object[] RawArgs
        {
            get
            {
                if (Method.IsStatic)
                    return Args;

                object[] rawArgs = new object[Args.Length + 1];
                rawArgs[0] = Instance;
                Args.CopyTo(rawArgs, 1);

                return rawArgs;
            }
        }

        internal CallContext(MethodBase method, Delegate call, object[] args)
        {
            Method = method;
            Call = call;

            if (!Method.IsStatic)
            {
                Instance = args[0];

                Args = new object[args.Length - 1];

                for (int i = 1; i < args.Length; i++)
                {
                    Args[i - 1] = args[i];
                }
            }
            else
            {
                Args = args;
            }
        }

        public object Continue()
        {
            ReturnValue = Call.DynamicInvoke(RawArgs);
            return ReturnValue;
        }

        public TResult Continue<TResult>()
        {
            return (TResult) Continue();
        }
    }
}




