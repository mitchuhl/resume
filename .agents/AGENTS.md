# AGENTS.md

Context file for AI agents working in this repository.

## Project

A JSON Resume based résumé pipeline. The source of truth is `resume.json` (jsonresume format, see `jsonresume/schema.json`). A vacancy description is used to build a tailored `resume-instance*.json` file, which is then rendered to HTML (`template.html`) and PDF. The PDF step is implemented by the `resume-generator` .NET console application, which validates the instance against the schema and renders it with QuestPDF.

## C# Code Guidelines

These rules apply to all C# code in this repository (e.g. the `resume-generator` project).

- Use explicit types instead of `var`.
- Use `System.Text.Json` instead of Newtonsoft.Json.
- Never add libraries, unless specifically asked in the prompt.
- Put each class in its own file (never more than one class per file).
