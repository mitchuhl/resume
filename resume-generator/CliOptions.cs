namespace resume_generator;

public sealed class CliOptions
{
    public string InputPath { get; private init; } = "resume-instance.json";

    public string SchemaPath { get; private init; } = Path.Combine("jsonresume", "schema.json");

    public string? OutputPath { get; private init; }

    public bool ShowHelp { get; private init; }

    public string? ErrorMessage { get; private init; }

    public string ResolveOutputPath()
    {
        if (!string.IsNullOrWhiteSpace(OutputPath))
        {
            return Path.GetFullPath(OutputPath);
        }

        string inputFullPath = Path.GetFullPath(InputPath);
        string directory = Path.GetDirectoryName(inputFullPath) ?? ".";

        return Path.Combine(directory, Path.GetFileNameWithoutExtension(inputFullPath) + ".pdf");
    }

    public static CliOptions Parse(string[] args)
    {
        string? inputPath = null;
        string? schemaPath = null;
        string? outputPath = null;
        bool showHelp = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i" or "--input":
                    inputPath = ReadValue(args, ref i, "--input", out string? inputError);

                    if (inputError is not null)
                    {
                        return new CliOptions { ErrorMessage = inputError };
                    }

                    break;
                case "-s" or "--schema":
                    schemaPath = ReadValue(args, ref i, "--schema", out string? schemaError);

                    if (schemaError is not null)
                    {
                        return new CliOptions { ErrorMessage = schemaError };
                    }

                    break;
                case "-o" or "--output":
                    outputPath = ReadValue(args, ref i, "--output", out string? outputError);

                    if (outputError is not null)
                    {
                        return new CliOptions { ErrorMessage = outputError };
                    }

                    break;
                case "-h" or "--help":
                    showHelp = true;
                    break;
                default:
                    if (args[i].StartsWith('-'))
                    {
                        return new CliOptions { ErrorMessage = $"Unknown option: {args[i]}" };
                    }

                    inputPath ??= args[i];
                    break;
            }
        }

        return new CliOptions
        {
            InputPath = inputPath ?? "resume-instance.json",
            SchemaPath = schemaPath ?? Path.Combine("jsonresume", "schema.json"),
            OutputPath = outputPath,
            ShowHelp = showHelp
        };
    }

    private static string? ReadValue(string[] args, ref int index, string option, out string? error)
    {
        error = null;

        if (index + 1 >= args.Length)
        {
            error = $"Missing value for option {option}";
            return null;
        }

        index++;

        return args[index];
    }
}
