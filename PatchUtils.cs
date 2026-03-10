global using IL = System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction>;
global using UnityObject = UnityEngine.Object;

using HarmonyLib;

using System;
using System.Linq.Expressions;
using System.Reflection;

public static class Member {
    public static MemberInfo Get(LambdaExpression expr) {
        var body = expr.Body;
        if (expr.Body is UnaryExpression { NodeType: ExpressionType.Convert } u) {
            body = u.Operand;
        }

        return body switch {
            MemberExpression m => m.Member,
            MethodCallExpression c => c.Method,
            NewExpression n => n.Constructor,
            _ => throw new InvalidProgramException($"Expression {expr} does not seem to access a member: the body is of type {body.GetType()}"),
        };
    }

    public static FieldInfo Field<T>(Expression<Func<T, object>> expr) => (FieldInfo)Get(expr);
    public static MethodInfo Method<T>(Expression<Func<T, object>> expr) => (MethodInfo)Get(expr);
    public static MethodInfo Method<T>(Expression<Action<T>> expr) => (MethodInfo)Get(expr);
    public static PropertyInfo Property<T>(Expression<Func<T, object>> expr) => (PropertyInfo)Get(expr);
    public static MethodInfo Getter<T>(Expression<Func<T, object>> expr) => Property(expr).GetGetMethod();
    public static MethodInfo Setter<T>(Expression<Func<T, object>> expr) => Property(expr).GetSetMethod();

    public static FieldInfo Field(Expression<Func<object>> expr) => (FieldInfo)Get(expr);
    public static MethodInfo Method(LambdaExpression expr) => (MethodInfo)Get(expr);
    public static PropertyInfo Property(Expression<Func<object>> expr) => (PropertyInfo)Get(expr);
    public static MethodInfo Getter(Expression<Func<object>> expr) => Property(expr).GetGetMethod();
    public static MethodInfo Setter(Expression<Func<object>> expr) => Property(expr).GetSetMethod();

    public static ConstructorInfo Constructor(LambdaExpression expr) => (ConstructorInfo)Get(expr);
}

public static class CodeMatcherExtensions {
    public static CodeMatcher GetPos(this CodeMatcher cm, out int pos) {
        pos = cm.Pos;
        return cm;
    }
    public static CodeMatcher GetOperand(this CodeMatcher cm, out object? operand) {
        operand = cm.Operand;
        return cm;
    }
    public static CodeMatcher GetOperand<T>(this CodeMatcher cm, out T operand) {
        operand = (T)cm.Operand;
        return cm;
    }
}