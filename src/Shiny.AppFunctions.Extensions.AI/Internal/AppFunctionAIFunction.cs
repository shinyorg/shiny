using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Shiny.AppFunctions.Extensions.AI.Internal;

/// <summary>
/// One app function as an <see cref="AIFunction"/>. Arguments are written to JSON without reflection and run through
/// <see cref="AppFunctionDispatcher.Execute"/>, so binding, delegates and error mapping match Siri and Gemini.
/// </summary>
sealed class AppFunctionAIFunction : AIFunction
{
    readonly AppFunctionDispatcher dispatcher;
    readonly AppFunctionDescriptor function;
    readonly JsonElement schema;

    public AppFunctionAIFunction(AppFunctionDispatcher dispatcher, AppFunctionDescriptor function)
    {
        this.dispatcher = dispatcher;
        this.function = function;

        using var doc = JsonDocument.Parse(function.GetParametersJsonSchema());
        this.schema = doc.RootElement.Clone();
    }

    public override string Name => this.function.Id;
    public override string Description => this.function.Description;
    public override JsonElement JsonSchema => this.schema;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        // the chat runs inside the app, so the app is on screen: OpensApp functions and OpenApp gates pass
        var outcome = await this.dispatcher
            .Execute(new AppFunctionInvocation(this.function.Id, IsForeground: true), ToJson(arguments), cancellationToken)
            .ConfigureAwait(false);

        if (outcome.Status != AppFunctionStatus.Success)
        {
            return new JsonObject
            {
                ["error"] = outcome.Message ?? "The app function failed.",
                ["code"] = (outcome.ErrorCode ?? AppFunctionErrorCode.AppError).ToString()
            };
        }

        var result = new JsonObject { ["success"] = true };
        if (outcome.ResultJson != null)
            result["result"] = JsonNode.Parse(outcome.ResultJson);
        if (!String.IsNullOrWhiteSpace(outcome.Dialog))
            result["message"] = outcome.Dialog;
        return result;
    }

    static string ToJson(AIFunctionArguments arguments)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var pair in arguments)
            {
                writer.WritePropertyName(pair.Key);
                WriteValue(writer, pair.Value);
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null: writer.WriteNullValue(); break;
            case JsonElement el: el.WriteTo(writer); break;
            case JsonNode node: node.WriteTo(writer); break;
            case string s: writer.WriteStringValue(s); break;
            case bool b: writer.WriteBooleanValue(b); break;
            case int i: writer.WriteNumberValue(i); break;
            case long l: writer.WriteNumberValue(l); break;
            case short sh: writer.WriteNumberValue(sh); break;
            case byte by: writer.WriteNumberValue(by); break;
            case double d: writer.WriteNumberValue(d); break;
            case float f: writer.WriteNumberValue(f); break;
            case decimal m: writer.WriteNumberValue(m); break;
            case DateTimeOffset dto: writer.WriteStringValue(dto.ToString("o", CultureInfo.InvariantCulture)); break;
            case DateTime dt: writer.WriteStringValue(dt.ToString("o", CultureInfo.InvariantCulture)); break;
            case Enum e: writer.WriteStringValue(e.ToString()); break;
            default: writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture)); break;
        }
    }
}
