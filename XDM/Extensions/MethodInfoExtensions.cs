using System.Reflection;

namespace Mittons.Xdm.Extensions;

public static class MethodInfoExtensions
{
    public static Func<object?[]?, object?> WrapForRpc(this MethodInfo @methodInfo, object? context)
    {
        return (object?[]? args) =>
        {
            return methodInfo.Invoke(context, args);
        };
    }
}