using System.Reflection;
using System.Text.Json.Serialization;
using Mittons.Xdm.Extensions;

namespace Mittons.Xdm;

public interface IObjectRegistry
{
    Dictionary<string, object>? GetRegisteredObject(string instanceid, object? instanceContext);
}

public interface ISerializationSettings
{
    [JsonPropertyName("includeUnderscoreProperties")]
    bool IncludeUnderscoreProperties { get; }
}

public interface IJsonRpcMessage
{
    [JsonPropertyName("id")]
    int Id { get; }

    [JsonPropertyName("instanceId")]
    string? InstanceId { get; }

    [JsonPropertyName("instanceContext")]
    object? InstanceContext { get; }

    [JsonPropertyName("methodName")]
    string? MethodName { get; }

    [JsonPropertyName("params")]
    object?[]? Params { get; }

    [JsonPropertyName("result")]
    object? Result { get; }

    [JsonPropertyName("error")]
    object? Error { get; }

    [JsonPropertyName("handshakeToken")]
    string HandshakeToken { get; }

    [JsonPropertyName("serializationSettings")]
    ISerializationSettings? SerializationSettings { get; }
}

public record JsonRpcMessage(
    int Id,
    string? InstanceId,
    object? InstanceContext,
    string? MethodName,
    object?[]? Params,
    object? Result,
    object? Error,
    string HandshakeToken,
    ISerializationSettings? SerializationSettings
) : IJsonRpcMessage
{
}

public interface IChannel
{
    int ChannelId { get; }

    IObjectRegistry ObjectRegistry { get; }

    int RegisterProxyFunction(Func<object?[]?, object?> function);

    Task<T> InvokeRemoteMethodAsync<T>(string methodName, string instanceId, object?[]? args, object? instanceContextData, CancellationToken cancellationToken);

    Task<T> GetRemoteObjectProxy<T>(string instanceId, object? contextData);
}

public class Channel : IChannel
{
    public int ChannelId { get; }

    private int _nextProxyId = 1;

    private int _nextMessageId = 1;

    private readonly string _handshakeToken = Guid.NewGuid().ToString();

    private Dictionary<string, Func<object?[]?, object?>> ProxyFunctions { get; } = new();

    public IObjectRegistry ObjectRegistry => throw new NotImplementedException();

    private Dictionary<int, TaskCompletionSource<object>> Messages = new();

    public int RegisterProxyFunction(Func<object?[]?, object?> function)
    {
        var proxyId = _nextProxyId++;

        ProxyFunctions.Add($"proxy{proxyId}", function);

        return proxyId;
    }

    public async Task<T> InvokeRemoteMethodAsync<T>(string methodName, string instanceId, object?[]? args, object? instanceContextData, CancellationToken cancellationToken)
    {
        var message = new JsonRpcMessage(
            Id: _nextMessageId++,
            InstanceId: instanceId,
            InstanceContext: instanceContextData,
            MethodName: null,
            Params: XdmSerializer.Serialize(args, this),
            Result: null,
            Error: null,
            HandshakeToken: _handshakeToken,
            SerializationSettings: null
        );

        Messages[message.Id] = new TaskCompletionSource<object>();

        await SendRpcMessageAsync(message, cancellationToken);

        return (T)(await Messages[message.Id].Task);
    }

    public Task<T> GetRemoteObjectProxy<T>(string instanceId, object? contextData)
    {
        throw new NotImplementedException();
    }

    private Task SendRpcMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task<bool> OnMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.InstanceId))
        {
            if (Messages.TryGetValue(message.Id, out var taskCompletionSource))
            {
                Messages.Remove(message.Id);

                if (message.Error is not null)
                {
                    // TODO: ADD DESERIALIZE
                    taskCompletionSource.TrySetException(new Exception());
                }
                else
                {
                    // TODO: ADD DESERIALIZE
                    taskCompletionSource.TrySetResult(1);
                }

                return true;
            }

            return false;
        }

        var registeredObject = ObjectRegistry.GetRegisteredObject(message.InstanceId, message.InstanceContext);

        if (registeredObject is null)
        {
            return false;
        }

        await InvokeMethodAsync(registeredObject, message, cancellationToken);

        return true;
    }

    private async Task InvokeMethodAsync(Dictionary<string, object> registeredInstance, IJsonRpcMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.MethodName))
        {
            await SendSuccessResponseAsync(message, registeredInstance, message.HandshakeToken, cancellationToken);

            return;
        }

        var method = registeredInstance[message.MethodName];

        if (method.GetType().GetTypeGroup() != TypeGroup.Method)
        {
            await SendErrorResponseAsync(message, new Exception($"RPC method not found: {message.MethodName}"), _handshakeToken, cancellationToken);

            return;
        }

        try
        {
            // TODO: ADD DESERIALIZE
            var methodArgs = new object[0];

            var methodInfo = method.GetType().GetProperty("Method")?.GetValue(method) as MethodInfo;
            var context = method.GetType().GetProperty("Target")?.GetValue(method);

            if (methodInfo is null)
            {
                await SendErrorResponseAsync(message, new Exception($"RPC method not found: {message.MethodName}"), _handshakeToken, cancellationToken);

                return;
            }

            var result = methodInfo.Invoke(context, methodArgs);

            if (result is Task)
            {
                // TODO: HANDLE ASYNC
                await SendErrorResponseAsync(message, new Exception("Can't support async methods yet"), _handshakeToken, cancellationToken);

                return;
            }

            await SendSuccessResponseAsync(message, result, message.HandshakeToken, cancellationToken);
        }
        catch (Exception exception)
        {
            await SendErrorResponseAsync(message, exception, _handshakeToken, cancellationToken);
        }
    }

    public Task SendErrorResponseAsync(IJsonRpcMessage message, Exception exception, string handshakeToken, CancellationToken cancellationToken)
    {
        var response = new JsonRpcMessage(
            Id: message.Id,
            InstanceId: null,
            InstanceContext: null,
            MethodName: null,
            Params: null,
            Result: null,
            Error: XdmSerializer.Serialize(exception, this),
            HandshakeToken: handshakeToken,
            SerializationSettings: null
        );

        return SendRpcMessageAsync(response, cancellationToken);
    }

    private Task SendSuccessResponseAsync(IJsonRpcMessage message, object? registeredInstance, string handshakeToken, CancellationToken cancellationToken)
    {
        var response = new JsonRpcMessage(
            Id: message.Id,
            InstanceId: null,
            InstanceContext: null,
            MethodName: null,
            Params: null,
            Result: XdmSerializer.Serialize(registeredInstance, this),
            Error: null,
            HandshakeToken: handshakeToken,
            SerializationSettings: null
        );

        return SendRpcMessageAsync(response, cancellationToken);
    }
}

public static class XdmSerializer
{
    private const int MaxXdmDepth = 100;

    public static object? Deserialize(object? value, Dictionary<int, object?> circularReferences)
    {
        if (value is null)
        {
            return null;
        }

        return null;
    }

    private static object? Serialize(object? value, Type inputType, IChannel channel, Stack<object> originalObjects, Stack<object> newObjects, int nextCircularReferenceId, int depth)
    {
        var typeGroup = inputType.GetTypeGroup();

        if (value is null || (depth == MaxXdmDepth && (typeGroup == TypeGroup.Array || typeGroup == TypeGroup.Map)))
        {
            return null;
        }

        switch (typeGroup)
        {
            case TypeGroup.Primitive:
                if (inputType == typeof(DateTime))
                {
                    return new
                    {
                        __proxyDate = new DateTimeOffset((DateTime)value).ToUnixTimeMilliseconds()
                    };
                }

                return value;
            case TypeGroup.Array:
                var returnArray = new List<object?>();

                // Javascript won't serialize the array references to JSON properly, so we'll just return an empty array
                if (depth < MaxXdmDepth && !originalObjects.Contains(value))
                {
                    originalObjects.Push(value);
                    newObjects.Push(returnArray);

                    foreach (var element in (IEnumerable<object>)value)
                    {
                        returnArray.Add(Serialize(element, element.GetType(), channel, originalObjects, newObjects, nextCircularReferenceId, depth + 1));
                    }

                    originalObjects.Pop();
                    newObjects.Pop();
                }

                return returnArray.ToArray();
            case TypeGroup.Object:
                var parentItemIndex = originalObjects.ToList().IndexOf(value);

                if (parentItemIndex >= 0)
                {
                    var parentItem = (Dictionary<string, object?>)newObjects.ElementAt(parentItemIndex);

                    if (!parentItem.ContainsKey("__circularReferenceId"))
                    {
                        parentItem.Add("__circularReferenceId", nextCircularReferenceId++);
                    }

                    return new
                    {
                        __circularReference = parentItem["__circularReferenceId"]
                    };
                }

                var returnObject = new Dictionary<string, object?>();

                if (depth < MaxXdmDepth)
                {
                    originalObjects.Push(value);
                    newObjects.Push(returnObject);

                    foreach (var property in inputType.GetProperties())
                    {
                        returnObject.Add(property.Name, Serialize(property.GetValue(value), property.PropertyType, channel, originalObjects, newObjects, nextCircularReferenceId, depth + 1));
                    }

                    foreach (var field in inputType.GetFields())
                    {
                        returnObject.Add(field.Name, Serialize(field.GetValue(value), field.FieldType, channel, originalObjects, newObjects, nextCircularReferenceId, depth + 1));
                    }

                    originalObjects.Pop();
                    newObjects.Pop();
                }

                return returnObject;
            case TypeGroup.Map:
                return value;
            case TypeGroup.Method:
                var methodInfo = inputType.GetProperty("Method")?.GetValue(value) as MethodInfo;
                var context = inputType.GetProperty("Target")?.GetValue(value);

                if (methodInfo is null)
                {
                    throw new Exception($"Unable to get Method property for [{inputType}]");
                }

                return new
                {
                    __proxyFunctionId = channel.RegisterProxyFunction(methodInfo.WrapForRpc(context)),
                    _channelId = channel.ChannelId
                };
            case TypeGroup.Exception:
                var exception = (Exception)value;

                return new
                {
                    message = exception.Message,
                    name = "Error",
                    stack = exception.StackTrace,
                    toString = Serialize(exception.ToString, channel)
                };
            default:
                throw new Exception($"Unknown type [{inputType}]");
        }
    }

    public static object? Serialize(object? value, Type inputType, IChannel channel)
        => Serialize(value, inputType, channel, new Stack<object>(), new Stack<object>(), 1, 1);

    public static object? Serialize<T>(T value, IChannel channel)
        => value is null ? null : Serialize(value, value.GetType(), channel);

    public static object?[]? Serialize(object?[]? value, IChannel channel)
    {
        if (value is null)
        {
            return null;
        }

        var returnArray = new List<object?>();

        var originalObjects = new Stack<object>(new object[] { value });
        var newObjects = new Stack<object>(new object[] { value });

        foreach (var element in value)
        {
            if (element is null)
            {
                returnArray.Add(null);
            }
            else
            {
                returnArray.Add(Serialize(element, element.GetType(), channel, originalObjects, newObjects, 1, 2));
            }
        }

        return returnArray.ToArray();
    }
}