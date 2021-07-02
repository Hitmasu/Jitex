using System;
using System.Runtime.InteropServices;
using System.Text;
using Xunit.Abstractions;

namespace Jitex.Tests
{
    static class LogOutput
    {
        public static ITestOutputHelper Output { get; set; }

        public static void WriteLogMemory(IntPtr address, int size)
        {
            byte[] buffer = new byte[size];
            Marshal.Copy(address, buffer, 0, size);

            StringBuilder sb = new StringBuilder();

            sb.Append("--------------------");
            for (int i = 0; i < size; i++)
            {
                if (i % 8 == 0)
                {
                    sb.Append($"{Environment.NewLine}0x{(address + i).ToString("X2")} - ");
                }

                byte b = buffer[i];
                sb.Append($"{b:X2} ");
            }
            sb.Append("\n--------------------");

            Output.WriteLine(sb.ToString());
        }
    }
}
