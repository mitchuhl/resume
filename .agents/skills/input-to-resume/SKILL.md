---
name: input-to-resume
description: Reads an external file (e.g. PDF, Word, LinkedIn export) and compares its content against /resume.json using the jsonresume schema. Asks the user for confirmation before adding any missing fields or entries.
---

# Input to Resume

## Goal

Read a user-provided file, detect resume-relevant information that is **not yet present** in `/resume.json`, and ask the user whether each missing piece should be added.

## Inputs

1. Ask the user for the **file path** to read (e.g. `@/input/LinkedIn.pdf`).
2. Read the file using the appropriate tool for the file type (PDF, Word, text, etc.).
3. For **PDF** files, first convert to text using the bundled `mutool.exe` before parsing resume data.

## PDF Text Extraction

For PDF input, extract text with the bundled `mutool.exe` using its path relative to the project root:

- `.agents\skills\input-to-resume\tools\mutool.exe convert -F text -o temp\[filename].txt [input.pdf]`

Then parse the resulting TXT file as the resume source.

## Tools

This skill expects `mutool.exe` at `.agents\skills\input-to-resume\tools\mutool.exe`.

## Reference Schema

Use `@/jsonresume/schema.json` as the source of truth for what fields and structures are valid. The top-level sections to scan for are:

- `basics`
- `work`
- `volunteer`
- `education`
- `awards`
- `certificates`
- `publications`
- `skills`
- `languages`
- `interests`
- `references`
- `projects`
- `meta`

## Workflow

1. Read `/resume.json` and extract existing data.
2. Read the user-provided file and extract any resume-relevant data.
3. Compare the extracted data against `/resume.json`.
4. For each top-level section below, **in order**:
   - `basics`
   - `work`
   - `volunteer`
   - `education`
   - `awards`
   - `certificates`
   - `publications`
   - `skills`
   - `languages`
   - `interests`
   - `references`
   - `projects`
   - `meta`
5. For the current section, identify any **missing or incomplete** items from the input file.
6. If missing/incomplete items exist, **show them to the user** and ask what they want to do for that section (for example: add all, add selected items only, skip, or update existing entries).
7. Apply only the changes the user explicitly confirms for that section.
8. After each confirmed change, **save the updated data back to `/resume.json`** while preserving the existing structure and formatting style.
9. After all sections have been processed, **increase the second digit in the version number** in `/resume.json`.

## Rules

- Do **not** add anything without explicit user confirmation.
- Do **not** overwrite existing entries unless the user explicitly requests an update.
- Write only **short facts** in a **structured way** — do **not** write full sentences or prose.
  - Good: `company: Custom software development`, `role: internship`, `summary: Built vb.net calendar component`
  - Bad: `summary: The company creates custom software for big companies. I did internship at the company and wrote a calendar component.`
- Use ISO 8601 dates where applicable (e.g. `YYYY-MM` or `YYYY-MM-DD`).
- After each confirmed addition, save the updated data back to `/resume.json`.
- Keep entries concise and keyword-oriented, matching the existing style of `/resume.json`.
