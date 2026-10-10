using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using resume_generator.Models;

namespace resume_generator.Rendering;

public sealed class ResumePdfDocument : IDocument
{
    private static readonly Color Accent = Color.FromHex("#1F4E79");
    private static readonly Color Muted = Colors.Grey.Darken1;

    private static readonly Dictionary<string, string> PersonalDetailLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["maritalStatus"] = "Burgerlijke staat",
        ["birthDate"] = "Geboortedatum",
        ["birthPlace"] = "Geboorteplaats",
        ["nationality"] = "Nationaliteit"
    };

    private readonly Resume _resume;

    public ResumePdfDocument(Resume resume)
    {
        _resume = resume;
    }

    public DocumentMetadata GetMetadata()
    {
        Basics? basics = _resume.Basics;
        string name = string.IsNullOrWhiteSpace(basics?.Name) ? "Resume" : basics!.Name!;

        string title = string.IsNullOrWhiteSpace(basics?.Label)
            ? name
            : $"{name} - {basics!.Label}";

        return new DocumentMetadata
        {
            Title = title,
            Author = name,
            Creator = "resume-generator"
        };
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(45);
            page.MarginVertical(40);
            page.DefaultTextStyle(static style => style.FontSize(10).FontColor(Colors.Grey.Darken3).LineHeight(1.3f));

            page.Header().ShowOnce().Element(ComposeHeader);
            page.Content().PaddingTop(8).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        Basics? basics = _resume.Basics;

        container.Column(column =>
        {
            if (!string.IsNullOrWhiteSpace(basics?.Name))
            {
                column.Item()
                    .PaddingBottom(2)
                    .Text(basics!.Name!)
                    .FontSize(22)
                    .Bold()
                    .FontColor(Accent);
            }

            if (!string.IsNullOrWhiteSpace(basics?.Label))
            {
                column.Item()
                    .PaddingBottom(4)
                    .Text(basics!.Label!)
                    .FontSize(12)
                    .SemiBold()
                    .FontColor(Colors.Grey.Darken2);
            }

            column.Item().Element(ComposeContactLine);

            List<(string Label, string Value)> personalDetails = ComposePersonalDetails();

            if (personalDetails.Count > 0)
            {
                column.Item()
                    .PaddingTop(3)
                    .Text(string.Join("   |   ", personalDetails.Select(detail => $"{detail.Label}: {detail.Value}")))
                    .FontSize(9)
                    .FontColor(Muted);
            }
        });
    }

    private void ComposeContactLine(IContainer container)
    {
        Basics? basics = _resume.Basics;
        List<(string Display, string? Url)> segments = new List<(string Display, string? Url)>();

        if (!string.IsNullOrWhiteSpace(basics?.Email))
        {
            segments.Add((basics!.Email!, null));
        }

        if (!string.IsNullOrWhiteSpace(basics?.Phone))
        {
            segments.Add((basics!.Phone!, null));
        }

        if (!string.IsNullOrWhiteSpace(basics?.Url))
        {
            segments.Add((basics!.Url!, basics!.Url));
        }

        string location = ComposeLocationText();

        if (location.Length > 0)
        {
            segments.Add((location, null));
        }

        foreach (Profile profile in basics?.Profiles ?? [])
        {
            if (string.IsNullOrWhiteSpace(profile.Network) && string.IsNullOrWhiteSpace(profile.Username))
            {
                continue;
            }

            string display = profile.Network is null
                ? profile.Username!
                : profile.Username is null
                    ? profile.Network
                    : $"{profile.Network}: {profile.Username}";

            segments.Add((display, profile.Url));
        }

        if (segments.Count == 0)
        {
            return;
        }

        container.Text(text =>
        {
            text.DefaultTextStyle(style => style.FontSize(9.5f).FontColor(Muted));

            for (int i = 0; i < segments.Count; i++)
            {
                if (i > 0)
                {
                    text.Span("  |  ");
                }

                if (segments[i].Url is null)
                {
                    text.Span(segments[i].Display);
                }
                else
                {
                    text.Hyperlink(segments[i].Url, segments[i].Display);
                }
            }
        });
    }

    private string ComposeLocationText()
    {
        Location? location = _resume.Basics?.Location;

        if (location is null)
        {
            return string.Empty;
        }

        List<string> parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(location.Address))
        {
            parts.Add(location.Address);
        }

        string postalCity = $"{location.PostalCode} {location.City}".Trim();

        if (postalCity.Length > 0)
        {
            parts.Add(postalCity);
        }

        if (!string.IsNullOrWhiteSpace(location.Region))
        {
            parts.Add(location.Region);
        }

        if (!string.IsNullOrWhiteSpace(location.CountryCode))
        {
            parts.Add(location.CountryCode);
        }

        return string.Join(", ", parts);
    }

    private List<(string Label, string Value)> ComposePersonalDetails()
    {
        List<(string Label, string Value)> details = new List<(string Label, string Value)>();

        foreach (KeyValuePair<string, JsonElement> additionalProperty in _resume.Basics?.AdditionalProperties ?? new Dictionary<string, JsonElement>())
        {
            string label = PersonalDetailLabels.TryGetValue(additionalProperty.Key, out string? mapped)
                ? mapped
                : Humanize(additionalProperty.Key);

            string value = FormatExtensionValue(additionalProperty.Key, additionalProperty.Value);

            if (value.Length > 0)
            {
                details.Add((label, value));
            }
        }

        return details;
    }

    private static string FormatExtensionValue(string key, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                string text = value.GetString() ?? string.Empty;
                return key.Equals("birthDate", StringComparison.OrdinalIgnoreCase)
                    ? PartialDateFormatter.Format(text)
                    : text;
            case JsonValueKind.Array:
                return string.Join(", ", value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()));
            default:
                return value.ToString();
        }
    }

    private static string Humanize(string key)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        foreach (char character in key)
        {
            if (builder.Length > 0 && char.IsUpper(character))
            {
                builder.Append(' ');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.Length == 0 ? key : char.ToUpperInvariant(builder[0]) + builder.ToString(1, builder.Length - 1);
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(14);

            ComposeProfile(column);
            ComposeWork(column);
            ComposeEducation(column);
            ComposeSkills(column);
            ComposeProjects(column);
            ComposeCertificates(column);
            ComposeLanguages(column);
            ComposeInterests(column);
            ComposeAwards(column);
            ComposePublications(column);
            ComposeReferences(column);
            ComposeVolunteer(column);
        });
    }

    private void ComposeProfile(ColumnDescriptor column)
    {
        string? summary = _resume.Basics?.Summary;

        if (string.IsNullOrWhiteSpace(summary))
        {
            return;
        }

        AddSection(column, "Profiel", section =>
        {
            section.Item().Text(summary!).Justify();
        });
    }

    private void ComposeWork(ColumnDescriptor column)
    {
        List<WorkExperience>? items = _resume.Work;

        if (items is null || items.Count == 0)
        {
            return;
        }

        AddSection(column, "Werkervaring", section =>
        {
            foreach (WorkExperience work in items)
            {
                section.Item().PaddingBottom(9).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Column(header =>
                        {
                            if (!string.IsNullOrWhiteSpace(work.Name))
                            {
                                header.Item().Text(work.Name).FontSize(11).Bold();
                            }

                            if (!string.IsNullOrWhiteSpace(work.Position))
                            {
                                header.Item().Text(work.Position).FontSize(10).FontColor(Accent);
                            }
                        });

                        string dates = PartialDateFormatter.FormatRange(work.StartDate, work.EndDate);

                        if (dates.Length > 0)
                        {
                            row.RelativeItem().AlignRight().Text(dates).FontSize(9.5f).FontColor(Muted);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(work.Location))
                    {
                        item.Item().PaddingBottom(2).Text(work.Location).FontSize(9).FontColor(Muted);
                    }

                    if (!string.IsNullOrWhiteSpace(work.Description))
                    {
                        item.Item().PaddingBottom(3).Text(work.Description);
                    }

                    if (!string.IsNullOrWhiteSpace(work.Summary))
                    {
                        item.Item().PaddingBottom(3).Text(work.Summary).Italic().FontColor(Colors.Grey.Darken2);
                    }

                    ComposeHighlights(item, work.Highlights);
                });
            }
        });
    }

    private void ComposeVolunteer(ColumnDescriptor column)
    {
        List<VolunteerExperience>? items = _resume.Volunteer;

        if (items is null || items.Count == 0)
        {
            return;
        }

        AddSection(column, "Vrijwilligerswerk", section =>
        {
            foreach (VolunteerExperience volunteer in items)
            {
                section.Item().PaddingBottom(9).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Column(header =>
                        {
                            if (!string.IsNullOrWhiteSpace(volunteer.Organization))
                            {
                                header.Item().Text(volunteer.Organization).FontSize(11).Bold();
                            }

                            if (!string.IsNullOrWhiteSpace(volunteer.Position))
                            {
                                header.Item().Text(volunteer.Position).FontSize(10).FontColor(Accent);
                            }
                        });

                        string dates = PartialDateFormatter.FormatRange(volunteer.StartDate, volunteer.EndDate);

                        if (dates.Length > 0)
                        {
                            row.RelativeItem().AlignRight().Text(dates).FontSize(9.5f).FontColor(Muted);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(volunteer.Location))
                    {
                        item.Item().PaddingBottom(2).Text(volunteer.Location).FontSize(9).FontColor(Muted);
                    }

                    if (!string.IsNullOrWhiteSpace(volunteer.Summary))
                    {
                        item.Item().PaddingBottom(3).Text(volunteer.Summary);
                    }

                    ComposeHighlights(item, volunteer.Highlights);
                });
            }
        });
    }

    private void ComposeEducation(ColumnDescriptor column)
    {
        List<Education>? items = _resume.Education;

        if (items is null || items.Count == 0)
        {
            return;
        }

        AddSection(column, "Opleiding", section =>
        {
            foreach (Education education in items)
            {
                section.Item().PaddingBottom(9).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Column(header =>
                        {
                            if (!string.IsNullOrWhiteSpace(education.Institution))
                            {
                                header.Item().Text(education.Institution).FontSize(11).Bold();
                            }

                            string? studyType = education.StudyType;

                            if (!string.IsNullOrWhiteSpace(education.Area))
                            {
                                studyType = string.IsNullOrWhiteSpace(studyType)
                                    ? education.Area
                                    : $"{studyType} - {education.Area}";
                            }

                            if (!string.IsNullOrWhiteSpace(studyType))
                            {
                                header.Item().Text(studyType).FontSize(10).FontColor(Accent);
                            }
                        });

                        string dates = PartialDateFormatter.FormatRange(education.StartDate, education.EndDate);

                        if (dates.Length > 0)
                        {
                            row.RelativeItem().AlignRight().Text(dates).FontSize(9.5f).FontColor(Muted);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(education.Score))
                    {
                        item.Item().PaddingBottom(2).Text($"Gemiddeld cijfer: {education.Score}").FontSize(9).FontColor(Muted);
                    }

                    List<string>? courses = education.Courses?.Where(course => !string.IsNullOrWhiteSpace(course)).ToList();

                    if (courses is { Count: > 0 })
                    {
                        item.Item().PaddingBottom(2).Text($"Vakken: {string.Join(", ", courses)}").FontSize(9).FontColor(Muted);
                    }
                });
            }
        });
    }

    private void ComposeSkills(ColumnDescriptor column)
    {
        List<Skill>? items = _resume.Skills?.Where(skill => !string.IsNullOrWhiteSpace(skill.Name) || (skill.Keywords?.Count ?? 0) > 0).ToList();

        if (items is null || items.Count == 0)
        {
            return;
        }

        AddSection(column, "Vaardigheden", section =>
        {
            section.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(170);
                    columns.RelativeColumn();
                });

                foreach (Skill skill in items)
                {
                    string name = skill.Name ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(skill.Level))
                    {
                        name = $"{name} ({skill.Level})";
                    }

                    table.Cell().PaddingVertical(2).Text(name).Bold().FontSize(9.5f);

                    List<string> keywords = skill.Keywords?.Where(keyword => !string.IsNullOrWhiteSpace(keyword)).ToList() ?? [];

                    table.Cell().PaddingVertical(2).Text(string.Join(", ", keywords)).FontSize(9.5f);
                }
            });
        });
    }

    private void ComposeProjects(ColumnDescriptor column)
    {
        List<Project>? items = _resume.Projects;

        if (items is null || items.Count == 0)
        {
            return;
        }

        AddSection(column, "Projecten", section =>
        {
            foreach (Project project in items)
            {
                section.Item().PaddingBottom(9).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Column(header =>
                        {
                            if (!string.IsNullOrWhiteSpace(project.Name))
                            {
                                header.Item().Text(project.Name).FontSize(11).Bold();
                            }

                            List<string> roles = project.Roles?.Where(role => !string.IsNullOrWhiteSpace(role)).ToList() ?? [];

                            List<string> subTitleParts = new List<string>();

                            if (!string.IsNullOrWhiteSpace(project.Type))
                            {
                                subTitleParts.Add(project.Type);
                            }

                            if (!string.IsNullOrWhiteSpace(project.Entity))
                            {
                                subTitleParts.Add(project.Entity);
                            }

                            if (roles.Count > 0)
                            {
                                subTitleParts.Add(string.Join(", ", roles));
                            }

                            if (subTitleParts.Count > 0)
                            {
                                header.Item().Text(string.Join(" | ", subTitleParts)).FontSize(9).FontColor(Muted);
                            }
                        });

                        string dates = PartialDateFormatter.FormatRange(project.StartDate, project.EndDate);

                        if (dates.Length > 0)
                        {
                            row.RelativeItem().AlignRight().Text(dates).FontSize(9.5f).FontColor(Muted);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(project.Description))
                    {
                        item.Item().PaddingBottom(3).Text(project.Description);
                    }

                    ComposeHighlights(item, project.Highlights);

                    List<string> keywords = project.Keywords?.Where(keyword => !string.IsNullOrWhiteSpace(keyword)).ToList() ?? [];

                    if (keywords.Count > 0)
                    {
                        item.Item().PaddingTop(2).Text($"Technieken: {string.Join(", ", keywords)}").FontSize(9).FontColor(Muted);
                    }
                });
            }
        });
    }

    private void ComposeCertificates(ColumnDescriptor column)
    {
        List<Certificate>? items = _resume.Certificates;

        if (items is null || items.Count == 0)
        {
            return;
        }

        AddSection(column, "Certificaten", section =>
        {
            foreach (Certificate certificate in items)
            {
                section.Item().PaddingBottom(6).Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        if (!string.IsNullOrWhiteSpace(certificate.Name))
                        {
                            text.Span(certificate.Name).Bold();
                        }

                        if (!string.IsNullOrWhiteSpace(certificate.Issuer))
                        {
                            text.Span($" - {certificate.Issuer}");
                        }
                    });

                    string date = PartialDateFormatter.Format(certificate.Date);

                    if (date.Length > 0)
                    {
                        row.ConstantItem(120).AlignRight().Text(date).FontSize(9.5f).FontColor(Muted);
                    }
                });
            }
        });
    }

    private void ComposeLanguages(ColumnDescriptor column)
    {
        List<SpokenLanguage>? items = _resume.Languages;

        if (items is null || items.Count == 0)
        {
            return;
        }

        AddSection(column, "Talen", section =>
        {
            section.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(170);
                    columns.RelativeColumn();
                });

                foreach (SpokenLanguage language in items)
                {
                    table.Cell().PaddingVertical(2).Text(language.Language ?? string.Empty).Bold().FontSize(9.5f);
                    table.Cell().PaddingVertical(2).Text(language.Fluency ?? string.Empty).FontSize(9.5f);
                }
            });
        });
    }

    private void ComposeInterests(ColumnDescriptor column)
    {
        List<Interest>? items = _resume.Interests;

        if (items is null || items.Count == 0)
        {
            return;
        }

        AddSection(column, "Interesses", section =>
        {
            foreach (Interest interest in items)
            {
                List<string> keywords = interest.Keywords?.Where(keyword => !string.IsNullOrWhiteSpace(keyword)).ToList() ?? [];

                string text = keywords.Count > 0
                    ? $"{interest.Name}: {string.Join(", ", keywords)}"
                    : interest.Name ?? string.Empty;

                if (text.Length == 0)
                {
                    continue;
                }

                section.Item().PaddingBottom(4).Text(text);
            }
        });
    }

    private void ComposeAwards(ColumnDescriptor column)
    {
        List<Award>? items = _resume.Awards;

        if (items is null || items.Count == 0)
        {
            return;
        }

        AddSection(column, "Prijzen", section =>
        {
            foreach (Award award in items)
            {
                section.Item().PaddingBottom(7).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Text(award.Title ?? string.Empty).Bold();

                        string date = PartialDateFormatter.Format(award.Date);

                        if (date.Length > 0)
                        {
                            row.ConstantItem(120).AlignRight().Text(date).FontSize(9.5f).FontColor(Muted);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(award.Awarder))
                    {
                        item.Item().Text(award.Awarder).FontSize(9.5f).FontColor(Muted);
                    }

                    if (!string.IsNullOrWhiteSpace(award.Summary))
                    {
                        item.Item().Text(award.Summary);
                    }
                });
            }
        });
    }

    private void ComposePublications(ColumnDescriptor column)
    {
        List<Publication>? items = _resume.Publications;

        if (items is null || items.Count == 0)
        {
            return;
        }

        AddSection(column, "Publicaties", section =>
        {
            foreach (Publication publication in items)
            {
                section.Item().PaddingBottom(7).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Text(publication.Name ?? string.Empty).Bold();

                        string date = PartialDateFormatter.Format(publication.ReleaseDate);

                        if (date.Length > 0)
                        {
                            row.ConstantItem(120).AlignRight().Text(date).FontSize(9.5f).FontColor(Muted);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(publication.Publisher))
                    {
                        item.Item().Text(publication.Publisher).FontSize(9.5f).FontColor(Muted);
                    }

                    if (!string.IsNullOrWhiteSpace(publication.Summary))
                    {
                        item.Item().Text(publication.Summary);
                    }
                });
            }
        });
    }

    private void ComposeReferences(ColumnDescriptor column)
    {
        List<Reference>? items = _resume.References;

        if (items is null || items.Count == 0)
        {
            return;
        }

        AddSection(column, "Referenties", section =>
        {
            foreach (Reference reference in items)
            {
                section.Item().PaddingBottom(7).Column(item =>
                {
                    if (!string.IsNullOrWhiteSpace(reference.Name))
                    {
                        item.Item().Text(reference.Name).Bold().FontSize(9.5f);
                    }

                    if (!string.IsNullOrWhiteSpace(reference.ReferenceText))
                    {
                        item.Item().Text(reference.ReferenceText).FontSize(9.5f).Italic().FontColor(Colors.Grey.Darken2);
                    }
                });
            }
        });
    }

    private static void ComposeHighlights(ColumnDescriptor item, List<string>? highlights)
    {
        List<string> validHighlights = highlights?.Where(highlight => !string.IsNullOrWhiteSpace(highlight)).ToList() ?? [];

        foreach (string highlight in validHighlights)
        {
            item.Item().PaddingLeft(12).PaddingBottom(1).Row(row =>
            {
                row.ConstantItem(10).Text("•");
                row.RelativeItem().Text(highlight);
            });
        }
    }

    private static void AddSection(ColumnDescriptor column, string title, Action<ColumnDescriptor> body)
    {
        column.Item().Column(section =>
        {
            section.Item()
                .BorderBottom(1)
                .PaddingBottom(4)
                .Text(title)
                .FontSize(12)
                .Bold()
                .FontColor(Accent);

            section.Item().PaddingTop(6).Column(body);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        string name = _resume.Basics?.Name ?? string.Empty;

        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(style => style.FontSize(8.5f).FontColor(Muted));

            if (name.Length > 0)
            {
                text.Span($"{name}   |   ");
            }

            text.Span("Pagina ");
            text.CurrentPageNumber();
            text.Span(" van ");
            text.TotalPages();
        });
    }
}
