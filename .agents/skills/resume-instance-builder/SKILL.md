---
name: resume-instance-builder
description: Creates a tailored resume-instance.json from resume.json and a vacancy description, adopting the perspective of the target audience.
---

# Resume Instance Builder

## Goal

Produce a `resume-instance.json` that repackages the source `resume.json` into audience-specific prose and structure, making the candidate look like a strong fit for a specific vacancy.

## Role

Adopt the role of **Resume writer**. Your audience is determined by the user inputs below. Write as if you are speaking directly to that audience and optimise every section for their priorities, terminology, and expectations.

## Inputs

Ask the user for all of the following before proceeding:

1. **Company type** — what kind of organisation is this? (e.g. `Highschool`, `Consultancy`, `Start-up`, `Government agency`, `Non-profit`, `Corporation`).
2. **Role / Job** — what role or job title is being filled? (e.g. `Backend developer`, `Product Owner`, `ICT Teacher`, `DevOps Engineer`).
3. **Vacancy source** — a URL or a local file path to the vacancy description.

Do not continue until all three inputs have been collected.

## Workflow

1. **Detect vacancy language**
   - Determine the primary language of the vacancy text.
   - Note it as `vacancyLanguage` and use it consistently throughout `resume-instance.json`.

2. **Read the vacancy**
   - If the vacancy source is a URL, fetch it with the web fetch tool.
   - If the vacancy source is a local file path, read the file with the appropriate tool.
   - Extract and preserve the exact requirements, responsibilities, preferred qualifications, technologies, soft skills, and any stated values or mission of the employer.

3. **Read the resume**
   - Read `resume.json` (or the user-provided path) and extract every section:
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
   - Do not omit any entries. Every fact from the resume is a potential fit signal.

4. **Map resume to vacancy**
   - Identify the overlap between the resume and the vacancy.
   - For each resume entry, determine:
     - **Relevance**: does this entry support a vacancy requirement?
     - **Framing**: how should this be phrased for the target audience?
     - **Audience alignment**: does the audience value this kind of experience?

5. **Generate `resume-instance.json`**
   - Create a new file named `resume-instance.json` in the project root.
   - The file **must follow the same schema as `resume.json`** (use `@/jsonresume/schema.json` as reference). Use only these top-level fields:
     - `$schema`
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
   - Populate each section with content drawn **only** from `resume.json`, but rewritten in the **same language as the vacancy source** and framed for the audience.
   - Store the vacancy-derived context inside `meta` so the schema stays valid, for example:
     ```json
     "meta": {
       "canonical": "...",
       "version": "v0.5.0",
       "lastModified": "<ISO 8601 timestamp>",
       "vacancyContext": {
         "companyType": "<company type>",
         "role": "<role / job>",
         "vacancySource": "<URL or path>",
         "vacancyLanguage": "<detected language>",
         "audience": "<description of the target audience>"
       }
     }
     ```
    - Rewrite string values (`summary`, `description`, `label`, etc.) in the **vacancy language** while preserving factual accuracy.
    - All rewritten text must be written in **natural language from the perspective of the audience**, using their vocabulary and priorities.
    - Reorder and prioritise entries so that the most vacancy-relevant items appear first in each array.
    - Prioritise entries that directly match vacancy requirements. If a resume entry is only weakly relevant, include it briefly or omit it.
    - Do **not** invent experience or qualifications that are not present in `resume.json`.
    - Do **not** remove schema-valid fields unless they are empty in the source resume.
    - **Compact old irrelevant work experience**: remove work entries that are more than 15 years old and are not relevant to the vacancy. Relevant older entries may be compacted to show only key information.
    - **Compact volunteering**: remove volunteer entries that are not relevant to the vacancy. Relevant volunteer experience may be compacted.
    - **Compact skills**: remove skills and their keywords when they are clearly irrelevant to the vacancy. Compacting is allowed to show only relevant skills.
    - **Compact education courses**: remove courses from education entries when they are irrelevant to the vacancy. Keep relevant courses only.
    - **Languages**: keep only languages requested in the vacancy or that are relevant to the role. Remove low-grade languages unless specifically asked for in the vacancy. Strongest languages must stay.

6. **Review and reflect**
   - Tell the user who you are (based on the audience you've been given) and that you are going to read his resume (`resume-instance.json`).
   - Read the generated `resume-instance.json`.
   - Return to the user: What do you think of it now reading it? Does the candidate fit? Is all text in the resume spelled correctly? Does the language match? Is the text written by human and not an AI agent?

## Audience Perspective Rules

- **Corporate / Enterprise**: emphasise stability, scale, governance, delivery, stakeholder management, and process.
- **Start-up / Scale-up**: emphasise adaptability, breadth, ownership, speed, and impact.
- **Government / Public sector**: emphasise reliability, compliance, service, accessibility, and long-term stewardship.
- **Education / School**: emphasise clarity, communication, mentorship, curriculum alignment, and student or staff impact.
- **Consultancy**: emphasise client delivery, problem solving, cross-team collaboration, and commercial awareness.
- **Non-profit**: emphasise mission alignment, community impact, resourcefulness, and values-driven work.
- Match the language of the vacancy. If the vacancy uses `scrum`, use `scrum`. If it uses `agile`, use `agile`.

## Output

Write the completed `resume-instance.json` to the project root. Do not modify `resume.json`.
