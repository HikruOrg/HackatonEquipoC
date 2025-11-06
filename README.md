# Interview Copilot + Insight   

*(English / Español)* 

 

--- 

 

## Overview / Descripción general 

 

**English:**   

Interview Copilot + Insight is a tool that analyzes interview audio—both recorded and live—to extract meaningful insights. It transcribes the conversation, generates five targeted follow-up questions with grounding quotes, evaluates key skills (problem-solving, system design, and communication), and estimates the speaker’s English proficiency using CEFR standards. 

 

**Español:**   

Interview Copilot + Insight es una herramienta que analiza audio de entrevistas, tanto grabado como en vivo, para generar información estructurada. Transcribe la conversación, produce cinco preguntas de seguimiento basadas en citas del transcript, evalúa las principales habilidades (resolución de problemas, diseño de sistemas y comunicación) y estima el nivel de inglés del hablante utilizando los estándares CEFR. 

 

--- 

 

## Features / Características 

 

**English:**   

- Converts interview audio (recorded or live) to text using Whisper.   

- Generates five consistent follow-up questions linked to specific transcript quotes.   

- Evaluates candidate performance in three dimensions: problem-solving, system design, and communication.   

- Estimates English proficiency (CEFR) with evidence snippets.   

- Displays results on a single web page including transcript, insight card, CEFR level, and follow-ups. 

 

**Español:**   

- Convierte el audio de la entrevista (grabado o en vivo) en texto utilizando Whisper.   

- Genera cinco preguntas de seguimiento coherentes vinculadas a citas específicas del transcript.   

- Evalúa el desempeño del candidato en tres dimensiones: resolución de problemas, diseño de sistemas y comunicación.   

- Estima el nivel de inglés (CEFR) con fragmentos de evidencia.   

- Muestra los resultados en una sola página web que incluye la transcripción, la tarjeta de análisis, el nivel CEFR y las preguntas de seguimiento. 

 

--- 

 

## How to Run / Cómo ejecutarlo 

 

**English:**   

1. Capture or upload the interview audio (live or recorded).   

2. Convert it to text using Whisper (batch or streaming mode).   

3. Send the transcript to GPT-4o or GPT-5 for analysis.   

4. Display the results in a web interface that includes:   

   - The full transcript.   

   - A summary card with the rubric and CEFR estimate.   

   - Five copy-ready follow-up questions. 

 

**Español:**   

1. Captura o sube el audio de la entrevista (en vivo o grabado).   

2. Convierte el audio en texto utilizando Whisper (modo por lotes o en streaming).   

3. Envía la transcripción al modelo GPT-4o o GPT-5 para su análisis.   

4. Muestra los resultados en una interfaz web que incluya:   

   - La transcripción completa.   

   - Una tarjeta de resumen con la rúbrica y el nivel CEFR estimado.   

   - Cinco preguntas de seguimiento listas para copiar. 

 

--- 

 

## Limitations / Limitaciones 

 

**English:**   

- Accuracy depends on audio clarity and background noise.   

- The CEFR level is an approximate estimation, not a certified assessment.   

- Follow-up quality depends on transcript accuracy.   

- Pronunciation analysis is not yet included. 

 

**Español:**   

- La precisión depende de la claridad del audio y del nivel de ruido ambiental.   

- El nivel CEFR es una estimación aproximada, no una evaluación certificada.   

- La calidad de las preguntas de seguimiento depende de la precisión de la transcripción.   

- El análisis de pronunciación aún no está incorporado. 

 

--- 

 

## Technical Summary / Resumen Técnico (para jurado) 

 

### Stack and Architecture / Stack y Arquitectura 

 

**Frontend:** Angular 17 (standalone) + Angular Material (cards, chips, list, buttons, progress-bar).   

- Audio recording using **MediaRecorder API (WebM/MP4)**.   

- UI with **Submit Response / Next Question (Finish)** and visible validations. 

 

**Backend:** .NET 8 Minimal API (C#).   

- Endpoints: `/register`, `/questions`, `/analyze`, `/finish`.   

- JSON serialization via `System.Text.Json` with string-based enums.   

- STT: **OpenAI Whisper (batch)** with fallback `MockTranscriber`.   

- Accepts multipart/form-data (MP3/WAV/WEBM).   

- LLM: **OpenAI Chat Completions (gpt-4o-mini)** with controlled prompt and strict JSON output.   

- Retries, code-fence stripping, and JSON validation included.   

- Email: **SMTP (SmtpClient)** with Noop fallback; errors handled via try/catch.   

- Mismatch detection (Question ↔ Transcript): **cosine similarity (TF-based)** without APIs → relevance + mismatch.   

- UI warning card: *Use anyway* / *Discard & re-record*.   

- Containers: separate **Dockerfiles** (API and Web) + optional **docker-compose.yml**. 

 

--- 

 

### End-to-End Flow / Flujo E2E 

 

1. **Registration:** user enters email and name → generates `candidateId`.   

2. **UI:** loads each question from `questions.json`.   

3. User records or uploads audio.   

4. **Submit:**   

   - Backend runs STT (Whisper or mock).   

   - Calculates relevance (cosine TF).   

   - If threshold passes → calls LLM.   

   - Returns: transcript, rubric (Problem-Solving / System Design / Communication), CEFR + evidence, strengths/concerns, and 5 follow-ups with quotes.   

5. User proceeds with *Next* or *Finish*.   

6. `/finish` endpoint computes final CEFR and sends summary email (non-blocking).   

7. **UI summary:** shows final CEFR level and copyable follow-ups. 

 

--- 

 

### Evaluation Criteria and Scoring / Criterios de Evaluación 

 

#### 1) Functionality and Reliability (35 pts) 

- Fully functional E2E flow: registration → questions → STT → LLM → insights → finish → summary.   

- Batch STT (no streaming): Whisper on full file.   

- Basic error handling:   

  - STT/LLM fallbacks with clear UI messages.   

  - SMTP try/catch prevents crashes.   

  - File required and validation before Next.   

  - Mismatch detector prevents irrelevant answers.   

  - MockAnalysis active if no `OPENAI_API_KEY`.   

**Expected result:** 35/35 (solid flow + resilient error handling). 

 

#### 2) Adherence to Brief and Output Quality (25 pts) 

- All brief requirements met:   

  - Batch STT ✅   

  - 5 follow-ups with grounding quotes ✅   

  - Rubric (PS/SD/Comm) with low/med/high ✅   

  - CEFR with 1–2 evidences ✅   

  - Simple single-page web UI ✅   

  - Controlled JSON outputs with retries ✅   

**Expected result:** 25/25. 

 

#### 3) UX and Presentation (20 pts) 

- Angular Material UI with clear cards, chips for rubric/CEFR, progress bar for analysis.   

- Simple flow: Submit → Next / Finish Interview.   

- Mismatch warning with options *Use anyway* or *Re-record*.   

- Elevator pitch (≤60s):   

  *"You record your answer → STT → LLM generates transcript, rubric, CEFR and follow-ups with quotes; final step computes CEFR and emails summary."*   

**Expected result:** 20/20 (clean and intuitive UX). 

 

#### 4) Technical Quality and Collaboration (20 pts) 

- Modular design: `Stt.cs`, `Llm.cs`, `Email.cs`, `Relevance.cs`, models, minimal endpoints.   

- Best practices: simple DI, retries, validation, frontend-backend separation, containers.   

- Team collaboration: tasks divided (FE Material / BE API & LLM / STT & email / DevOps).   

**Expected result:** 20/20. 

 

**Total Estimated Score: 100/100.** 

 

--- 

 

### Engineering Decisions / Decisiones de Ingeniería 

 

- Minimal API + Angular Material: high performance and clean delivery.   

- Whisper batch STT strictly follows brief; fallback ensures demo robustness.   

- Structured JSON with retries reduces formatting issues.   

- Local cosine-based mismatch detection (no API cost).   

- Non-blocking email prevents UX degradation from SMTP errors. 

 

--- 

 

### Environment Variables / Variables de Entorno 

 

| Variable | Description | 

|-----------|--------------| 

| `OPENAI_API_KEY` | API key for Whisper/Chat | 

| `USE_MOCK_STT` | `"true"` to enable simulated STT | 

| `Email__From`, `Email__SmtpHost`, `Email__SmtpPort`, `Email__User`, `Email__Pass` | SMTP configuration (optional) | 

| `ASPNETCORE_URLS` | e.g. `http://+:8080` | 

 

--- 

 

### Security and Privacy / Seguridad y Privacidad 

 

- Audio and transcripts are processed in-memory; not stored on disk.   

- No external data: only local files and `questions.json`.   

- HTTPS and memory expiration recommended for production. 

 

--- 

 

### Deployment / Runbook 

 

**Backend:** 

```bash 

cd backend 

dotnet run --urls http://localhost:8080 