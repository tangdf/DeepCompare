using System.Threading;

namespace DeepCompare
{
    public static class DeepComparer
    {

        private static readonly ThreadLocal<CompareContext> s_context = new ThreadLocal<CompareContext>(() => new CompareContext());

        public static bool Compare<T>(T x, T y)
        {
            var context = s_context.Value;

            try
            {
                return ComparerGenerator<T>.Compare(x, y, context);
            }
            finally
            {
                context.Reset();
            }
        }
        public static bool Compare<T>(T x, T y, CompareContext context)
        {
            return ComparerGenerator<T>.Compare(x, y, context);
        }
    }
}
