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

- `.agents\skills\input-to-resume\tools\mutool.exe convert -F text -o .\temp\[filename].txt [input.pdf]`

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
4. Process the sections **one at a time** in the order below. For each section:
   - Identify any **missing or incomplete** items from the input file for **that section only**.
   - If none exist, proceed directly to the next section.
   - If missing/incomplete items exist, **show them to the user** and ask what they want to do for that section only (for example: add all, add selected items only, skip, or update existing entries).
   - Wait for the user's explicit confirmation before continuing.
   - Apply only the changes the user explicitly confirms for that section.
   - **Save the updated data back to `/resume.json`** while preserving the existing structure and formatting style.
   - Only then move on to the next section.
   - Sections in order:
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
5. After all sections have been processed, **increase the second digit in the version number** in `/resume.json`.

## Rules

- Do **not** add anything without explicit user confirmation.
- Do **not** overwrite existing entries unless the user explicitly requests an update.
- Write only **short facts** in a **structured way** — do **not** write full sentences or prose.
  - Good: `company: Custom software development`, `role: internship`, `summary: Built vb.net calendar component`
  - Bad: `summary: The company creates custom software for big companies. I did internship at the company and wrote a calendar component.`
- Use ISO 8601 dates where applicable (e.g. `YYYY-MM` or `YYYY-MM-DD`).
- After each confirmed addition, save the updated data back to `/resume.json`.
- Keep entries concise and keyword-oriented, matching the existing style of `/resume.json`.
- In the `work` section, always order entries by `startDate` from newest to oldest.
- In the `education` section, format the `institution` attribute as `{name}, {city}, {countryCode}`.
 - In the `education` section, always order entries by `startDate` from newest to oldest.
   - If `startDate` is not present, assume it's old.
   - If `endDate` is not present, assume it's new.
   - If both are not present, assume it's old.

## Skills transformation

Each keyword currently listed under `skills[].keywords[]` is treated as an individual skill. When restructuring `skills`, transform every keyword into its own object with the following shape:

- `name` — the original keyword value
- `type` — the former group name (e.g. `Database`, `Web Development`)
- `level` — default to `Basic`
- `lastUsed` — year string; try to find a year from `work` and `education` entries, otherwise default to `1981`
- `usedAt` — comma-separated names of work companies or education schools where the skill appears; leave empty if unknown
- `keywords` — keep empty for now

Order the transformed `skills` array by `type` group first, then by `lastUsed` year descending within each group.

If a skill was learned only in education and never used in work or projects, remove it from `skills`. Education courses already capture these details, so they do not need to be duplicated in `skills`.

If multiple skills share a common root or brand (for example, Lotus-related entries), group them into one skill item with `name` set to the root name, `type` set to the most relevant category, and move each unique original skill name into the `keywords` array. Remove the individual entries after grouping.
