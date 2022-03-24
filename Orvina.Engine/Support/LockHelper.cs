namespace Orvina.Engine.Support
{
    internal static class LockHelper
    {
        public delegate bool Func3<T>(out T a);

        public static bool RunLock<T>(ref SpinLock sw, Func3<T> atomicAction, out T b)
        {
            var locked = false;
            try
            {
                sw.Enter(ref locked);
                var res = atomicAction(out T a);
                b = a;
                return res;
            }
            finally
            {
                if (locked)
                {
                    sw.Exit(false);
                }
            }
        }

        public static void RunLock(ref SpinLock sw, Action atomicAction)
        {
            var locked = false;
            try
            {
                sw.Enter(ref locked);
                atomicAction();
            }
            finally
            {
                if (locked)
                {
                    sw.Exit(false);
                }
            }
        }

        public static T RunLock<T>(ref SpinLock sw, Func<T> atomicAction)
        {
            var locked = false;
            try
            {
                sw.Enter(ref locked);
                return atomicAction();
            }
            finally
            {
                if (locked)
                {
                    sw.Exit(false);
                }
            }
        }
    }
}