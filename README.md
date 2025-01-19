# Redactor

This is a protype application written in c# and is used to redact sensitive information from text. 
The application is built using the .NET Core framework and is deployed as a Docker container using Aspire.

A Chrome extension is included in the browser folder that can be used to redact text from a OpenAI ChatGpt and Gemini.

To Do:
- [ ] Add a feature to redact text from Anthropic Cluade AI.
- [ ] Add a feature to redact text from Perplexity AI.
- [ ] Add OpenTelemetry to the application, for observability and logging.
- [ ] Add load testing to the application.

Dependent:
[Presidio](https://github.com/microsoft/presidio) is written by Microsoft. Used to analyze text and images, inclusive of a redaction tool to redact sensitive information. 
The application is built using the Flask framework and is deployed as a Docker container.
For my needs a [customised version](https://github.com/AJGit/presidio_custom) of Presidio was built to include additional and specific features needed for my use case.

Redactor was built to "replace" the Presidio anonymizer tool to simplify and extend the core functionality.
These enhancements include:
- [x] Redact text from a ChatGpt.
- [x] Redact text from a Gemini.
- [x] Parse and detect issues with
    - [x] CSV files.
    - [x] Markdown files.
    - [x] Pdf files.
    - [x] Text files.
    - [x] Excel files.
    - [x] PowerPoint files.
    - [x] Word files.
- [x] Redaction methods include
    - [x] Encryption/Obfuscation.
    - [x] Masking with entity type.
    - [x] Replacing with fake data.
    - [x] Highlighting the sensitive text.
- [x] Include UK specific options.


# Chrome Extension

In the browser folder is a Chrome extension that can be used to redact text from a OpenAI ChatGpt and Gemini.
The extension has 3 main abilities:
- A side panel that can be used to redact text.
- An options page that can be used to set the redaction options.
- A content script that can be used to redact text from specific webpages.

In order to us this extension you may need to adjust the endpoint to the desired location and permissions.
Review and adjust the following endpoints in the files for your configuration:
- manifest.json
- content-script.js


# Presidio
Initial environment setup to build the Presidio image:

```cmd
git clone https://github.com/microsoft/presidio.git

conda activate presidio
poetry install --all-extras

conda install spacy
python -m spacy download en_core_web_lg
python -m spacy download en_core_web_sm
poetry remove spacy
poetry add spacy
poetry run python -m pip show spacy
python -c "import spacy; print(spacy.__version__)"

poetry shell
poetry run which python
poetry update
```

Docker setup:
```cmd
conda activate presidio
docker-compose down
docker image rm presidio-analyzer
docker-compose up -d --force-recreate
docker builder prune
```

Tagging local image:
```cmd
docker image tag {hash} {username}/presidio_custom:{#### or latest}
```

Pushing image to Docker Hub:
```cmd
docker push {username}/presidio_custom:latest
```

# Redator Application

To debug set the RedatorApiAspire.Apphost as the startup project.
> Note: When restarting the application the presidio docker container may take some time to be removed and recreated.

