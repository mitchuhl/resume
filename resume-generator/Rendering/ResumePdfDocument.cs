using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using resume_generator.Models;

namespace resume_generator.Rendering;

public sealed class ResumePdfDocument : IDocument
{
    private const float SidebarWidth = 178.6f;
    private const float PhotoSize = 112f;
    private const string DefaultPhotoFileName = "photo.jpg";

    private static readonly Color SidebarBackground = Color.FromHex("#1F4E79");
    private static readonly Color SidebarTrack = Color.FromHex("#3E6D9E");
    private static readonly Color SidebarText = Colors.White;
    private static readonly Color SidebarMuted = Color.FromHex("#B8CCE0");
    private static readonly Color Accent = SidebarBackground;

    private static readonly Dictionary<string, float> SkillLevelFractions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["no experience"] = 0.2f,
        ["beginner"] = 0.2f,
        ["elementary"] = 0.2f,
        ["novice"] = 0.2f,
        ["intermediate"] = 0.4f,
        ["advanced"] = 0.6f,
        ["proficient"] = 0.8f,
        ["expert"] = 0.8f,
        ["master"] = 1f
    };

    private static readonly Dictionary<string, string> PersonalDetailLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["maritalStatus"] = "Burgerlijke staat",
        ["birthDate"] = "Geboortedatum",
        ["birthPlace"] = "Geboorteplaats",
        ["nationality"] = "Nationaliteit"
    };

    private readonly Resume _resume;
    private readonly string? _photoPath;

    public ResumePdfDocument(Resume resume)
    {
        _resume = resume;
        _photoPath = ResolvePhotoPath();
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
            page.Margin(0);
            page.PageColor(SidebarBackground);
            page.DefaultTextStyle(static style => style.FontSize(10).FontColor(Colors.Grey.Darken3).LineHeight(1.35f));

            page.Content()
                .PaddingLeft(SidebarWidth)
                .Background(Colors.White)
                .PaddingTop(38)
                .PaddingRight(28)
                .PaddingBottom(30)
                .PaddingLeft(20)
                .Element(ComposeContent);

            page.Foreground()
                .Width(SidebarWidth)
                .PaddingTop(30)
                .PaddingRight(16)
                .PaddingBottom(12)
                .PaddingLeft(20)
                .Element(ComposeSidebarForeground);

            page.Footer().Element(ComposeFooter);
        });
    }

    private string? ResolvePhotoPath()
    {
        List<string?> candidates = [_resume.Basics?.Image, DefaultPhotoFileName];

        foreach (string? candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            try
            {
                string fullPath = Path.GetFullPath(candidate);

                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch (Exception)
            {
            }
        }

        return null;
    }

    private void ComposeSidebarForeground(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().ShowOnce().Element(ComposeSidebarFirstPage);
            column.Item().SkipOnce().ShowOnce().Element(ComposeSidebarSecondPage);
        });
    }

    private void ComposeSidebarFirstPage(IContainer container)
    {
        container.Column(column =>
        {
            ComposeSidebarPhoto(column);
            ComposeSidebarName(column);
            ComposeSidebarContact(column);
            ComposeSidebarPersonalDetails(column);
            ComposeSidebarSkills(column);
        });
    }

    private void ComposeSidebarSecondPage(IContainer container)
    {
        container.Column(column =>
        {
            ComposeSidebarCertificates(column);
            ComposeSidebarLanguages(column);
            ComposeSidebarInterests(column);
        });
    }

    private void ComposeSidebarPhoto(ColumnDescriptor column)
    {
        if (_photoPath is null)
        {
            return;
        }

        column.Item()
            .AlignCenter()
            .PaddingBottom(14)
            .Width(PhotoSize)
            .Height(PhotoSize)
            .CornerRadius(10)
            .Image(_photoPath)
            .FitArea();
    }

    private void ComposeSidebarName(ColumnDescriptor column)
    {
        Basics? basics = _resume.Basics;

        column.Item().PaddingBottom(6).Column(item =>
        {
            if (!string.IsNullOrWhiteSpace(basics?.Name))
            {
                item.Item()
                    .AlignCenter()
                    .PaddingBottom(2)
                    .Text(basics!.Name!)
                    .FontSize(17)
                    .Bold()
                    .FontColor(SidebarText);
            }

            if (!string.IsNullOrWhiteSpace(basics?.Label))
            {
                item.Item()
                    .AlignCenter()
                    .Text(basics!.Label!)
                    .FontSize(9)
                    .FontColor(SidebarMuted);
            }
        });
    }

    private void ComposeSidebarContact(ColumnDescriptor column)
    {
        Basics? basics = _resume.Basics;
        string location = ComposeLocationText();

        column.Item().Column(item =>
        {
            AddSidebarSectionTitle(item, "Contact");

            if (!string.IsNullOrWhiteSpace(basics?.Email))
            {
                AddSidebarDetail(item, basics!.Email!);
            }

            if (!string.IsNullOrWhiteSpace(basics?.Phone))
            {
                AddSidebarDetail(item, basics!.Phone!);
            }

            if (!string.IsNullOrWhiteSpace(basics?.Url))
            {
                item.Item()
                    .PaddingBottom(2)
                    .Hyperlink(basics!.Url!)
                    .Text(basics!.Url!)
                    .FontSize(8.5f)
                    .FontColor(SidebarText);
            }

            if (location.Length > 0)
            {
                AddSidebarDetail(item, location);
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

                if (string.IsNullOrWhiteSpace(profile.Url))
                {
                    AddSidebarDetail(item, display);
                }
                else
                {
                    item.Item()
                        .PaddingBottom(2)
                        .Hyperlink(profile.Url!)
                        .Text(display)
                        .FontSize(8.5f)
                        .FontColor(SidebarText);
                }
            }
        });
    }

    private void ComposeSidebarPersonalDetails(ColumnDescriptor column)
    {
        List<(string Label, string Value)> details = ComposePersonalDetails();

        if (details.Count == 0)
        {
            return;
        }

        column.Item().Column(item =>
        {
            AddSidebarSectionTitle(item, "Persoonlijk");

            foreach ((string label, string value) in details)
            {
                item.Item()
                    .PaddingBottom(3)
                    .Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8.5f).FontColor(SidebarMuted).LineHeight(1.25f));
                        text.Span($"{label}: ").Bold().FontColor(SidebarText);
                        text.Span(value);
                    });
            }
        });
    }

    private void ComposeSidebarSkills(ColumnDescriptor column)
    {
        List<Skill>? items = _resume.Skills?
            .Where(skill => !string.IsNullOrWhiteSpace(skill.Name) || (skill.Keywords?.Count ?? 0) > 0)
            .ToList();

        if (items is null || items.Count == 0)
        {
            return;
        }

        column.Item().Column(item =>
        {
            AddSidebarSectionTitle(item, "Vaardigheden");

            foreach (Skill skill in items)
            {
                item.Item().PaddingBottom(7).Column(skillColumn =>
                {
                    skillColumn.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .Text(skill.Name ?? string.Empty)
                            .FontSize(8.5f)
                            .Bold()
                            .FontColor(SidebarText);

                        if (!string.IsNullOrWhiteSpace(skill.Level))
                        {
                            row.ConstantItem(62)
                                .AlignRight()
                                .Text(skill.Level)
                                .FontSize(7)
                                .FontColor(SidebarMuted);
                        }
                    });

                    float fraction = ResolveSkillLevelFraction(skill.Level);

                    skillColumn.Item()
                        .PaddingTop(3)
                        .Height(5)
                        .Background(SidebarTrack)
                        .CornerRadius(2.5f)
                        .Row(row =>
                        {
                            row.RelativeItem(fraction).Height(5).Background(SidebarText).CornerRadius(2.5f);
                            row.RelativeItem(1f - fraction);
                        });

                    List<string> keywords = skill.Keywords?.Where(keyword => !string.IsNullOrWhiteSpace(keyword)).ToList() ?? [];

                    if (keywords.Count > 0)
                    {
                        skillColumn.Item()
                            .PaddingTop(2)
                            .Text(string.Join(", ", keywords))
                            .FontSize(7.5f)
                            .FontColor(SidebarMuted);
                    }
                });
            }
        });
    }

    private void ComposeSidebarCertificates(ColumnDescriptor column)
    {
        List<Certificate>? items = _resume.Certificates;

        if (items is null || items.Count == 0)
        {
            return;
        }

        column.Item().Column(item =>
        {
            AddSidebarSectionTitle(item, "Certificaten");

            foreach (Certificate certificate in items)
            {
                item.Item().PaddingBottom(6).Column(certificateColumn =>
                {
                    if (!string.IsNullOrWhiteSpace(certificate.Name))
                    {
                        certificateColumn.Item()
                            .Text(certificate.Name)
                            .FontSize(8.5f)
                            .Bold()
                            .FontColor(SidebarText);
                    }

                    List<string> details = [];

                    if (!string.IsNullOrWhiteSpace(certificate.Issuer))
                    {
                        details.Add(certificate.Issuer);
                    }

                    string date = PartialDateFormatter.Format(certificate.Date);

                    if (date.Length > 0)
                    {
                        details.Add(date);
                    }

                    if (details.Count > 0)
                    {
                        certificateColumn.Item()
                            .Text(string.Join(" - ", details))
                            .FontSize(7.5f)
                            .FontColor(SidebarMuted);
                    }
                });
            }
        });
    }

    private void ComposeSidebarLanguages(ColumnDescriptor column)
    {
        List<SpokenLanguage>? items = _resume.Languages;

        if (items is null || items.Count == 0)
        {
            return;
        }

        column.Item().Column(item =>
        {
            AddSidebarSectionTitle(item, "Talen");

            foreach (SpokenLanguage language in items)
            {
                item.Item().PaddingBottom(4).Row(row =>
                {
                    row.RelativeItem()
                        .Text(language.Language ?? string.Empty)
                        .FontSize(8.5f)
                        .Bold()
                        .FontColor(SidebarText);

                    row.ConstantItem(80)
                        .AlignRight()
                        .Text(language.Fluency ?? string.Empty)
                        .FontSize(8)
                        .FontColor(SidebarMuted);
                });
            }
        });
    }

    private void ComposeSidebarInterests(ColumnDescriptor column)
    {
        List<Interest>? items = _resume.Interests;

        if (items is null || items.Count == 0)
        {
            return;
        }

        column.Item().Column(item =>
        {
            AddSidebarSectionTitle(item, "Interesses");

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

                item.Item()
                    .PaddingBottom(4)
                    .Text(text)
                    .FontSize(8.5f)
                    .FontColor(SidebarText);
            }
        });
    }

    private void AddSidebarSectionTitle(ColumnDescriptor column, string title)
    {
        column.Item()
            .PaddingBottom(5)
            .PaddingTop(11)
            .BorderBottom(1)
            .BorderColor(SidebarTrack)
            .Text(title)
            .FontSize(10)
            .Bold()
            .FontColor(SidebarText);
    }

    private void AddSidebarDetail(ColumnDescriptor column, string text)
    {
        column.Item()
            .PaddingBottom(2)
            .Text(text)
            .FontSize(8.5f)
            .FontColor(SidebarText);
    }

    private static float ResolveSkillLevelFraction(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            return 0.4f;
        }

        return SkillLevelFractions.TryGetValue(level, out float fraction)
            ? fraction
            : 0.5f;
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(14);

            ComposeProfile(column);
            ComposeWork(column);
            ComposeEducation(column);
            ComposeProjects(column);
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
                section.Item().PaddingBottom(10).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Column(header =>
                        {
                            if (!string.IsNullOrWhiteSpace(work.Name))
                            {
                                header.Item().Text(work.Name).FontSize(11.5f).Bold();
                            }

                            if (!string.IsNullOrWhiteSpace(work.Position))
                            {
                                header.Item().Text(work.Position).FontSize(10).Bold().FontColor(Accent);
                            }
                        });

                        string dates = PartialDateFormatter.FormatRange(work.StartDate, work.EndDate);

                        if (dates.Length > 0)
                        {
                            row.ConstantItem(120).AlignBottom().AlignRight().Text(dates).FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(work.Location))
                    {
                        item.Item().PaddingBottom(2).Text(work.Location).FontSize(9).FontColor(Colors.Grey.Darken1);
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
                section.Item().PaddingBottom(10).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Column(header =>
                        {
                            if (!string.IsNullOrWhiteSpace(volunteer.Organization))
                            {
                                header.Item().Text(volunteer.Organization).FontSize(11.5f).Bold();
                            }

                            if (!string.IsNullOrWhiteSpace(volunteer.Position))
                            {
                                header.Item().Text(volunteer.Position).FontSize(10).Bold().FontColor(Accent);
                            }
                        });

                        string dates = PartialDateFormatter.FormatRange(volunteer.StartDate, volunteer.EndDate);

                        if (dates.Length > 0)
                        {
                            row.ConstantItem(120).AlignBottom().AlignRight().Text(dates).FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(volunteer.Location))
                    {
                        item.Item().PaddingBottom(2).Text(volunteer.Location).FontSize(9).FontColor(Colors.Grey.Darken1);
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
                section.Item().PaddingBottom(10).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Column(header =>
                        {
                            if (!string.IsNullOrWhiteSpace(education.Institution))
                            {
                                header.Item().Text(education.Institution).FontSize(11.5f).Bold();
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
                            row.ConstantItem(120).AlignBottom().AlignRight().Text(dates).FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(education.Score))
                    {
                        item.Item().PaddingBottom(2).Text($"Gemiddeld cijfer: {education.Score}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    }

                    List<string>? courses = education.Courses?.Where(course => !string.IsNullOrWhiteSpace(course)).ToList();

                    if (courses is { Count: > 0 })
                    {
                        item.Item().PaddingBottom(2).Text($"Vakken: {string.Join(", ", courses)}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    }
                });
            }
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
                section.Item().PaddingBottom(10).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Column(header =>
                        {
                            if (!string.IsNullOrWhiteSpace(project.Name))
                            {
                                header.Item().Text(project.Name).FontSize(11.5f).Bold();
                            }

                            List<string> roles = project.Roles?.Where(role => !string.IsNullOrWhiteSpace(role)).ToList() ?? [];

                            List<string> subTitleParts = [];

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
                                header.Item().Text(string.Join(" | ", subTitleParts)).FontSize(9).FontColor(Colors.Grey.Darken1);
                            }
                        });

                        string dates = PartialDateFormatter.FormatRange(project.StartDate, project.EndDate);

                        if (dates.Length > 0)
                        {
                            row.ConstantItem(120).AlignBottom().AlignRight().Text(dates).FontSize(9).FontColor(Colors.Grey.Darken1);
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
                        item.Item().PaddingTop(2).Text($"Technieken: {string.Join(", ", keywords)}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    }
                });
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
                section.Item().PaddingBottom(8).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Text(award.Title ?? string.Empty).Bold().FontSize(10.5f);

                        string date = PartialDateFormatter.Format(award.Date);

                        if (date.Length > 0)
                        {
                            row.ConstantItem(120).AlignRight().Text(date).FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(award.Awarder))
                    {
                        item.Item().Text(award.Awarder).FontSize(9.5f).FontColor(Colors.Grey.Darken1);
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
                section.Item().PaddingBottom(8).Column(item =>
                {
                    item.Item().Row(row =>
                    {
                        row.RelativeItem().Text(publication.Name ?? string.Empty).Bold().FontSize(10.5f);

                        string date = PartialDateFormatter.Format(publication.ReleaseDate);

                        if (date.Length > 0)
                        {
                            row.ConstantItem(120).AlignRight().Text(date).FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(publication.Publisher))
                    {
                        item.Item().Text(publication.Publisher).FontSize(9.5f).FontColor(Colors.Grey.Darken1);
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
                section.Item().PaddingBottom(8).Column(item =>
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
            item.Item().PaddingLeft(12).PaddingBottom(2).Row(row =>
            {
                row.ConstantItem(10).Text("•").FontColor(Accent);
                row.RelativeItem().Text(highlight);
            });
        }
    }

    private static void AddSection(ColumnDescriptor column, string title, Action<ColumnDescriptor> body)
    {
        column.Item().Column(section =>
        {
            section.Item()
                .PaddingBottom(6)
                .BorderBottom(1.5f)
                .BorderColor(Accent)
                .Text(title)
                .FontSize(12.5f)
                .Bold()
                .FontColor(Accent);

            section.Item().PaddingTop(6).Column(body);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        string name = _resume.Basics?.Name ?? string.Empty;

        container.Row(row =>
        {
            row.ConstantItem(SidebarWidth)
                .PaddingTop(10)
                .PaddingRight(20)
                .PaddingBottom(12)
                .PaddingLeft(20)
                .Text(name)
                .FontSize(7.5f)
                .FontColor(SidebarMuted);

            row.RelativeItem()
                .Background(Colors.White)
                .PaddingTop(10)
                .PaddingRight(28)
                .PaddingBottom(12)
                .PaddingLeft(20)
                .AlignRight()
                .Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(7.5f).FontColor(Colors.Grey.Darken1));
                    text.Span("Pagina ");
                    text.CurrentPageNumber();
                    text.Span(" van ");
                    text.TotalPages();
                });
        });
    }

    private string ComposeLocationText()
    {
        Location? location = _resume.Basics?.Location;

        if (location is null)
        {
            return string.Empty;
        }

        List<string> parts = [];

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
        List<(string Label, string Value)> details = [];

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
}
