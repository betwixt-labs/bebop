using System;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace Bebop.Extensions
{
    internal static class DynamicModuleLambdaCompiler
    {
        internal static Func<T> GenerateFactory<T>() where T : new()
        {
            Expression<Func<T>> expr = () => new T();
            var newExpr = (NewExpression) expr.Body;

            var method = new DynamicMethod("lambda",
                newExpr.Type,
                new Type[0],
                typeof(DynamicModuleLambdaCompiler).Module,
                true);

            var ilGen = method.GetILGenerator();
            // Constructor for value types could be null
            if (newExpr.Constructor != null)
            {
                ilGen.Emit(OpCodes.Newobj, newExpr.Constructor);
            }
            else
            {
                var temp = ilGen.DeclareLocal(newExpr.Type);
                ilGen.Emit(OpCodes.Ldloca, temp);
                ilGen.Emit(OpCodes.Initobj, newExpr.Type);
                ilGen.Emit(OpCodes.Ldloc, temp);
            }

            ilGen.Emit(OpCodes.Ret);

            return (Func<T>) method.CreateDelegate(typeof(Func<T>));
        }
    }
}