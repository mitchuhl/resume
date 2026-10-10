using System.Text.Json;
using Json.Schema;

namespace resume_generator.Validation;

public static class ResumeSchemaValidator
{
    public static EvaluationResults Validate(JsonElement instance, string schemaPath)
    {
        JsonSchema schema = JsonSchema.FromFile(Path.GetFullPath(schemaPath));

        EvaluationOptions options = new EvaluationOptions
        {
            RequireFormatValidation = true,
            OutputFormat = OutputFormat.List
        };

        return schema.Evaluate(instance, options);
    }

    public static IEnumerable<string> CollectErrors(EvaluationResults results)
    {
        foreach (KeyValuePair<string, string> error in results.Errors ?? [])
        {
            string location = results.InstanceLocation.ToString();

            string message = $"{error.Key}: {error.Value}";

            yield return string.IsNullOrEmpty(location)
                ? message
                : $"{location}: {message}";
        }

        foreach (EvaluationResults detail in results.Details ?? [])
        {
            foreach (string nested in CollectErrors(detail))
            {
                yield return nested;
            }
        }
    }
}
