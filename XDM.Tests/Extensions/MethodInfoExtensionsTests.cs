using System.Reflection;
using System.Text.Json;
using Mittons.Xdm.Extensions;

namespace Mittons.Xdm.Tests.Extensions;

public class MethodInfoExtensionsTests
{
    [Fact]
    public void WrapForRpc_WhenWrappingAnActionWithNoReturnValue_ExpectNullResult()
    {
        // Arrange
        var function = () => { };
        var wrapped = function.GetMethodInfo().WrapForRpc(function.Target);

        // Act
        var result = wrapped(default);

        // Assert
        Assert.Null(result);
    }
    public struct Tests
    {
        public int Third = 2;

        public Tests(int first, int second)
        {
            First = first;
            Second = second;
        }

        public int First { get; }

        public int Second { get; }
    }

    public record Tests2(int First, string Second);

    [Fact]
    public void WrapForRpc_WhenWrappingAnActionWithArgumentsAndAReturnValue_ExpectNullResult()
    {
        // Arrange
        var q = (int x) => x * 2;

        var temp = new List<object> { 10, "a", q };
        temp.Add(temp);

        var obj1 = new
        {
            ListTest = temp,
            ArrayTest = new object[] { 10, "a" },
            StackTest = new Stack<object>(new object[] { 10, "a" }),
            QueueTest = new Queue<object>(new object[] { 10, "a" }),
            DictionaryTest = new Dictionary<string, object> { { "First", 10 }, { "Second", "a" } }
        };

        obj1.ListTest.Add(obj1);

        var a = new
        {
            first = 1,
            second = new int[] { 2 },
            third = new
            {
                value = 3
            }
        };

        var b = JsonSerializer.Deserialize<dynamic>(JsonSerializer.Serialize(a));
        var c = (JsonElement)b;

        var c1 = c.ValueKind.GetType();


        var arr = JsonSerializer.Deserialize<dynamic>(JsonSerializer.Serialize(1));
        var arrEl = (JsonElement)arr;
        var arrE2 = arrEl.TryGetByte
        //var c = b.GetType().GetField("ValueKind").GetValue(b);
        var d = XdmSerializer.Serialize(obj1, new Channel());
        var function = (int a) => a + 10;
        //var wrapped = function.GetMethodInfo().WrapForRpc(function.Target);

        // Act
        // Assert
        //Assert.Equal(20, wrapped(new object[] { 10 }));
    }
    // public static Func<object?[]?, object?> WrapForRpc(this MethodInfo @methodInfo, object? context)
    // {
    //     return (object?[]? args) =>
    //     {
    //         return methodInfo.Invoke(context, args);
    //     };
    // }
}