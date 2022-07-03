// using System.Reflection;
// using XDM.Extensions;

// namespace XDM;

// public record ParentObjects(Stack<object?> OriginalObjects, Stack<object?> NewObjects);

// public class Channel
// {
//     private int _nextMessageId = 1;

//     private int _nextProxyId = 1;

//     public const int MaxDepth = 100;

//     private Dictionary<string, Func<object?[]?, object?>> _proxyFunctions = new Dictionary<string, Func<object?[]?, object?>>();

//     private int RegisterProxyFunction(MethodInfo methodInfo, object? context)
//     {
//         var proxyFunctionId = _nextMessageId++;

//         _proxyFunctions[$"proxy{proxyFunctionId}"] = methodInfo.WrapForRpc(context);

//         return proxyFunctionId;
//     }

//     public object? SerializeObject(object? obj, ParentObjects? previousParentObjects)
//     {
//         var parentObjects = previousParentObjects ?? new ParentObjects(new Stack<object?>(), new Stack<object?>());

//         parentObjects.OriginalObjects.Push(obj);

//         var returnValue = new Dictionary<object, object>();

//         parentObjects.NewObjects.Push(returnValue);

//         foreach (var field in obj.GetType().GetFields())
//         {
//             // var value = field.GetValue(obj);

//             // var parentItemIndex = 1;

//             // parentObjects.OriginalObjects.
//             if (field.FieldType == typeof(DateTime))
//             {
//                 returnValue[field.Name] = new
//                 {
//                     __proxyDate = new DateTimeOffset((DateTime)field.GetValue(obj)).ToUnixTimeMilliseconds()
//                 };
//             }
//         }

//         parentObjects.OriginalObjects.Pop();
//         parentObjects.NewObjects.Pop();
//     }
// }