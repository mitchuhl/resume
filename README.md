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

Step: LinkedIn
- open your profile page, eg. https://www.linkedin.com/in/michael-de-graaf-software-developer/
- click the three dots and then "Save as PDF"
- rename to "LinkedIn.pdf" and save it in the "input" folder

Prompt:

```
/input-to-resume:skill
```


## resume.json

Write only short facts to the resume.json. In a structured way.

Eg. :
company: custom software development
role: internship, create vb.net calendar component 

And not: The company creates custom software for big companies. I did internalship at the company and wrote a calendar component.

Reason for this is that later this resume.json will be input for another agent that makes nices sentences (and only pick relevant pieces for the job I apply to)