using System.Text.Json;
using OpenAI.Chat;

namespace AgentWorkshop.Core;

public class ToolMapper
{
    private readonly Dictionary<string, Func<string, Task<string>>> _toolMap = [];
    public ChatTool CreateTool<T,V>(string name, Func<T, Task<V>> func, bool strict = true) where T : class
    {
        if (_toolMap.ContainsKey(name)) {
            throw new Exception("Duplicate tool name " + name);
        }
        bool valueType = typeof(V).IsValueType;
        _toolMap[name] = async inputJson => {
            var input = JsonSerializer.Deserialize<T>(inputJson)
                ?? throw new Exception("Input was null");
            var output = await func(input);
            return (valueType ? output?.ToString() : JsonSerializer.Serialize(output)) ?? "";
        };
        return ChatTool.CreateFunctionTool(
            name,
            SchemaBuilder.GetDescription<T>(),
            BinaryData.FromString(SchemaBuilder.GetJsonSchema<T>()),
            strict
        );
    }
    public ChatTool CreateTool<T,V>(string name, Func<T, V> func, bool strict = true) where T : class
        => CreateTool<T,V>(name, input => Task.FromResult(func(input)), strict);
        
    public Task<string> InvokeToolAsync(string name, string args)
        => _toolMap.TryGetValue(name, out var func) ? func(args) : Task.FromResult("Tool " + name + " not found");
}