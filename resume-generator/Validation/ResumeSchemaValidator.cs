using System.Text.Json;
using Json.Schema;

namespace resume_generator.Validation;

public static class ResumeSchemaValidator
{
    public static EvaluationResults Validate(JsonElement instance, string schemaPath)
    {
        var schema = JsonSchema.FromFile(Path.GetFullPath(schemaPath));

        var options = new EvaluationOptions
        {
            RequireFormatValidation = true,
            OutputFormat = OutputFormat.List
        };

        return schema.Evaluate(instance, options);
    }

    public static IEnumerable<string> CollectErrors(EvaluationResults results)
    {
        foreach (var error in results.Errors ?? [])
        {
            var location = results.InstanceLocation.ToString();

            var message = $"{error.Key}: {error.Value}";

            yield return string.IsNullOrEmpty(location)
                ? message
                : $"{location}: {message}";
        }

        foreach (var detail in results.Details ?? [])
        {
            foreach (var nested in CollectErrors(detail))
            {
                yield return nested;
            }
        }
    }
}
