using Jitex.Attributes;

namespace FrameworkTest
{
    public static class MethodParser
    {
        [DetourByMethodName("SomarToReplace")]
        public static int Subtrair()
        {
            return 20;
        }
    }
}
