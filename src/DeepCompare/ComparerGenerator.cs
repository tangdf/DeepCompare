using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DeepCompare
{
    internal static class ComparerGenerator<T>
    {
        private static readonly ConcurrentDictionary<Type, Func<T, T, CompareContext, bool>> s_cache = new ConcurrentDictionary<Type, Func<T, T, CompareContext, bool>>();

        private static readonly Type s_genericType = typeof(T);

        private static readonly Func<T, T, CompareContext, bool> s_matchingTypeComparer = CreateComparer(s_genericType);

        private static readonly Func<Type, Func<T, T, CompareContext, bool>> s_generateComparer = CreateComparer;

        public static bool Compare(T x, T y, CompareContext context)
        {
            if (x == null)
                return y == null;

            var type = x.GetType();

            if (type != y.GetType())
                return false;

            if (type == s_genericType) return s_matchingTypeComparer(x, y, context);

            var result = s_cache.GetOrAdd(type, s_generateComparer);
            return result(x, y, context);
        }
 
        private static Func<T, T, CompareContext, bool> CreateComparer(Type type)
        {
            if (type == ReflectionInfos.IntPtrType || type == ReflectionInfos.UIntPtrType || ReflectionInfos.DelegateType.IsAssignableFrom(s_genericType) || type.IsPointer)
            {
                return (x, y, context) => { return ReferenceEquals(x, y); };
            }

            if (type.IsValueType || type == typeof(string)) {
                return (x, y, context) => { return x.Equals(y); };
            }
            if (type.IsArray)
            {
                return CreateArrayComparer(type);
            }

            if (type.IsByRef) return ThrowNotSupportedType(type);

            var xParameter = Expression.Parameter(s_genericType, "x");
            var yParameter = Expression.Parameter(s_genericType, "y");

            Expression xExpression = xParameter;
            Expression yExpression = yParameter;

            if (type != s_genericType) {
                xExpression = Expression.Convert(xParameter, type);
                yExpression = Expression.Convert(yExpression, type);
            }

            var contextParameter = Expression.Parameter(typeof(CompareContext), "context");
            var falseConstant = Expression.Constant(false);

            var labelTarget = Expression.Label(typeof(bool));


            List<Expression> expressions = new List<Expression>();
            //引用比较
            var referenceEqualsExpression = Expression.Call(ReflectionInfos.ReferenceEquals, ConvertObject(xParameter), ConvertObject(yParameter));
            expressions.Add(Expression.IfThen(referenceEqualsExpression, Expression.Goto(labelTarget, Expression.Constant(true))));

            foreach (var fieldInfo in GetCopyableFields(type))
            {

                var xFieldExpression = Expression.Field(xExpression, fieldInfo);
                var yFieldExpression = Expression.Field(yExpression, fieldInfo);

                var methodCallExpression = Expression.Call(ReflectionInfos.CompareInner.MakeGenericMethod(fieldInfo.FieldType), xFieldExpression,
                  yFieldExpression, contextParameter);

                Expression testExpression = Expression.Equal(methodCallExpression, falseConstant);

                if (fieldInfo.FieldType.IsValueType == false)
                {
                    var skipExpression = Expression.Equal(Expression.Call(contextParameter, ReflectionInfos.Skip, ConvertObject(xFieldExpression), ConvertObject(yFieldExpression)), falseConstant);
                    
                    testExpression = Expression.AndAlso(skipExpression, testExpression);
                }
                //else
                //{
                //    testExpression = Expression.Equal(methodCallExpression, falseConstant);
                //}

                expressions.Add(Expression.IfThen(testExpression, Expression.Goto(labelTarget, falseConstant)));
            }

            expressions.Add(Expression.Label(labelTarget, Expression.Constant(true)));

            var blockExpression = Expression.Block(expressions);

            return Expression.Lambda<Func<T, T, CompareContext, bool>>(blockExpression, xParameter, yParameter, contextParameter).Compile();

            Expression ConvertObject(Expression expression)
            {
                return Expression.Convert(expression, typeof(object));
            }
        }

        
        private static List<FieldInfo> GetCopyableFields(Type type)
        {
            var result = GetAllFields(type).ToList();

            return result;

            IEnumerable<FieldInfo> GetAllFields(Type containingType)
            {
                const BindingFlags allFields =
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                var current = containingType;
                while (current != typeof(object) && current != null)
                {
                    var fields = current.GetFields(allFields);
                    foreach (var field in fields)
                    {
                        yield return field;
                    }

                    current = current.BaseType;
                }
            }
        }


        private static Func<T, T, CompareContext, bool> CreateArrayComparer(Type type)
        {
            var elementType = type.GetElementType();

            var rank = type.GetArrayRank();

            MethodInfo methodInfo;
            switch (rank) {
                case 1:
                    methodInfo = ReflectionInfos.CopyArrayRank1;
                    break;
                case 2:
                    methodInfo = ReflectionInfos.CopyArrayRank2;
                    break;
                default:
                    return ArrayComparer.CopyArray;
            }

            if (type == s_genericType)
                return (Func<T, T, CompareContext, bool>) methodInfo.MakeGenericMethod(elementType)
                    .CreateDelegate(typeof(Func<T, T, CompareContext, bool>));
            else {

                var @delegate = methodInfo.MakeGenericMethod(elementType)
                    .CreateDelegate(typeof(Func<,,,>).MakeGenericType(type, type, typeof(CompareContext), typeof(bool)));

                return (Func<T, T, CompareContext, bool>) ReflectionInfos.ConvertDelegate.MakeGenericMethod(type,s_genericType)
                    .Invoke(null, new object[] { @delegate });

            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Func<T, T, CompareContext, bool> ThrowNotSupportedType(Type type)
        {
            throw new NotSupportedException($"Unable to copy object of type {type}.");
        }
    }
}