# Resume

Flow:
- Input: current resume files and sites (LinkedIn, JobBird, Indeed, Word document, etc)
- Create resume.json with dry facts ( https://jsonresume.org ). Text is structured with short keywords.
- AI writer. Input: audience (eg. high school), vacancy role (eq. Product Owner) and vacancy document
- Output 1: resume-instance.json
- AI designer
- Output 2: resume-instance.html
- PDF printer
- Output 3: resume-instance.html
- Send PDF by e-mail including link to resume-instance.json (and optionally a personal YouTube video)

## Prompt - first input

```
Read @jsonresume/schema.json . Then ask me questions to collect the data in order to create a new json file based on this schema. Between each step , save the output to a new file: resume.json
```

