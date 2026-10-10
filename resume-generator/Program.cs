using System.Text.Json;
using Json.Schema;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using resume_generator;
using resume_generator.Models;
using resume_generator.Rendering;
using resume_generator.Validation;

CliOptions options = CliOptions.Parse(args);

if (options.ErrorMessage is not null)
{
    Console.Error.WriteLine(options.ErrorMessage);
    Console.Error.WriteLine();
    PrintUsage();
    return 2;
}

if (options.ShowHelp)
{
    PrintUsage();
    return 0;
}

try
{
    if (!File.Exists(options.InputPath))
    {
        Console.Error.WriteLine($"Input file not found: {options.InputPath}");

        string[] available = Directory.GetFiles(".", "resume-instance*.json");

        if (available.Length > 0)
        {
            Console.Error.WriteLine("Available resume instances:");

            foreach (string file in available)
            {
                Console.Error.WriteLine($"  {file}");
            }
        }

        return 2;
    }

    if (!File.Exists(options.SchemaPath))
    {
        Console.Error.WriteLine($"Schema file not found: {options.SchemaPath}");
        return 2;
    }

    using JsonDocument document = JsonDocument.Parse(File.ReadAllText(options.InputPath));

    EvaluationResults validation = ResumeSchemaValidator.Validate(document.RootElement, options.SchemaPath);

    if (!validation.IsValid)
    {
        Console.Error.WriteLine($"Schema validation failed for '{options.InputPath}' against '{options.SchemaPath}':");

        foreach (string error in ResumeSchemaValidator.CollectErrors(validation))
        {
            Console.Error.WriteLine($"  {error}");
        }

        return 1;
    }

    Console.WriteLine($"'{options.InputPath}' is valid according to '{options.SchemaPath}' (additionalProperties: true)");

    Resume? resume = document.RootElement.Deserialize<Resume>()
        ?? throw new InvalidOperationException($"'{options.InputPath}' could not be parsed as a resume.");

    QuestPDF.Settings.License = LicenseType.Community;

    string outputPath = options.ResolveOutputPath();

    string? outputDirectory = Path.GetDirectoryName(outputPath);

    if (!string.IsNullOrEmpty(outputDirectory))
    {
        Directory.CreateDirectory(outputDirectory);
    }

    ResumePdfDocument pdfDocument = new ResumePdfDocument(resume);
    pdfDocument.GeneratePdf(outputPath);

    Console.WriteLine($"PDF generated: {outputPath}");

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);

    return 2;
}

static void PrintUsage()
{
    Console.WriteLine("""
        resume-generator - generates a resume PDF from a jsonresume instance file

        Usage:
          dotnet run --project resume-generator -- [<resume-instance.json>] [options]

        Options:
          -i, --input <path>     resume instance JSON file (default: resume-instance.json)
          -s, --schema <path>    jsonresume schema file (default: jsonresume/schema.json)
          -o, --output <path>    output PDF file (default: output/<input file name>.pdf)
          -h, --help             show this help
        """);
}
