namespace Mittons.Xdm.Extensions;

public static class TypeExtensions
{
    private static readonly Type[] MethodTypes = new Type[]
    {
        typeof(Func<>),
        typeof(Func<,>),
        typeof(Func<,,>),
        typeof(Func<,,,>),
        typeof(Func<,,,,>),
        typeof(Func<,,,,,>),
        typeof(Func<,,,,,,>),
        typeof(Func<,,,,,,,>),
        typeof(Func<,,,,,,,,>),
        typeof(Func<,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,,,,,,>),
        typeof(Action),
        typeof(Action<>),
        typeof(Action<,>),
        typeof(Action<,,>),
        typeof(Action<,,,>),
        typeof(Action<,,,,>),
        typeof(Action<,,,,,>),
        typeof(Action<,,,,,,>),
        typeof(Action<,,,,,,,>),
        typeof(Action<,,,,,,,,>),
        typeof(Action<,,,,,,,,,>),
        typeof(Action<,,,,,,,,,,>),
        typeof(Action<,,,,,,,,,,,>),
        typeof(Action<,,,,,,,,,,,,>),
        typeof(Action<,,,,,,,,,,,,,>),
        typeof(Action<,,,,,,,,,,,,,,>),
        typeof(Action<,,,,,,,,,,,,,,,>)
    };

    private static readonly Type[] ArrayTypes = new Type[]
    {
        typeof(List<>),
        typeof(Stack<>),
        typeof(Queue<>)
    };

    private static readonly Type[] MapTypes = new Type[]
    {
        typeof(Dictionary<,>)
    };

    private static readonly Type[] PrimitiveTypes = new Type[]
    {
        typeof(string),
        typeof(decimal)
    };

    private static bool IsOneOf(this Type @type, Type[] types)
    {
        var comparisonType = @type.IsGenericType ? @type.GetGenericTypeDefinition() : @type;

        return types.Contains(comparisonType);
    }

    public static TypeGroup GetTypeGroup(this Type @type)
    {
        if (typeof(Exception).IsAssignableFrom(@type))
        {
            return TypeGroup.Exception;
        }
        else if (@type.IsPrimitive || @type.IsEnum || @type.IsOneOf(PrimitiveTypes))
        {
            return TypeGroup.Primitive;
        }
        else if (@type.IsArray || @type.IsOneOf(ArrayTypes))
        {
            return TypeGroup.Array;
        }
        else if (@type.IsOneOf(MapTypes))
        {
            return TypeGroup.Map;
        }
        else if (typeof(Delegate).IsAssignableFrom(@type))
        {
            return TypeGroup.Method;
        }
        else if (@type.IsClass || @type.IsValueType)
        {
            return TypeGroup.Object;
        }

        return TypeGroup.Unknown;
    }
}

public enum TypeGroup
{
    Unknown,
    Primitive,
    Array,
    Map,
    Method,
    Object,
    Exception
}