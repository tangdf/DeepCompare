using System;
using System.Linq.Expressions;
using System.Reflection;

namespace DeepCompare
{
    internal static class ReflectionInfos
    {

        public static readonly Type IntPtrType = typeof(IntPtr);

        public static readonly Type UIntPtrType = typeof(UIntPtr);

        public static readonly Type DelegateType = typeof(Delegate);

        public static readonly MethodInfo Skip;

        public static readonly MethodInfo CompareInner;

        public static readonly MethodInfo GetTypeFromHandle;

        public static readonly MethodInfo CopyArrayRank1;

        public static readonly MethodInfo CopyArrayRank2;

        public new static readonly MethodInfo ReferenceEquals;


        public static readonly MethodInfo ConvertDelegate;

        static ReflectionInfos()
        {

            GetTypeFromHandle = GetFuncCall(() => Type.GetTypeFromHandle(typeof(Type).TypeHandle));

            CompareInner = GetFuncCall(() => DeepComparer.Compare(default(object), default(object), default(CompareContext))).GetGenericMethodDefinition();
            Skip = typeof(CompareContext).GetMethod(nameof(CompareContext.Skip));

            CopyArrayRank1 = GetFuncCall(() => ArrayComparer.CopyArrayRank1(default(object[]), default(object[]), default(CompareContext))).GetGenericMethodDefinition();

            CopyArrayRank2 = GetFuncCall(() => ArrayComparer.CopyArrayRank2(default(object[,]), default(object[,]), default(CompareContext))).GetGenericMethodDefinition();

            // ReSharper disable once EqualExpressionComparison
            ReferenceEquals = GetFuncCall(() => ReferenceEquals(default(object), default(object)));

            ConvertDelegate = typeof(ReflectionInfos).GetMethod(nameof(_ConvertDelegate),BindingFlags.Static|BindingFlags.NonPublic);

            MethodInfo GetFuncCall<T>(Expression<Func<T>> expression)
            {
                return (expression.Body as MethodCallExpression)?.Method
                       ?? throw new ArgumentException("Expression type unsupported.");
            }
        }


        private static Func<TTo, TTo, CompareContext, bool> _ConvertDelegate<TFrom, TTo>(Func<TFrom, TFrom, CompareContext, bool> func)
        {
            return (TTo x, TTo y, CompareContext context) => func((TFrom)(object)x, (TFrom)(object)y, context);
        }
    }
}