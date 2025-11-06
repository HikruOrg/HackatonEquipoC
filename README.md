# Interview Copilot + Insight — Resumen Técnico (para jurado)

## Stack y Arquitectura

- **Frontend:** Angular 17 (standalone) + **Angular Material** (cards, chips, list, buttons, progress-bar).  
  - Grabación de audio con **MediaRecorder API** (WebM/MP4).  
  - UI con **Submit Response** / **Next Question (Finish)** y validaciones visibles.
- **Backend:** **.NET 8 Minimal API** (C#).  
  - Endpoints: `/register`, `/questions`, `/analyze`, `/finish`.  
  - JSON por defecto (System.Text.Json) + enums serializados a string.
- **STT:** **OpenAI Whisper (batch)** con fallback **MockTranscriber**.  
  - Envío `multipart/form-data`; soporta MP3/WAV/WEBM.
- **LLM:** OpenAI Chat Completions (`gpt-4o-mini`) con **prompt controlado** y **salida JSON estricta**.  
  - **Retries + stripping de code fences** + validación de JSON.
- **Email:** SMTP (SmtpClient) con **Noop fallback** + `try/catch` para no romper la demo.  
- **Detección de mismatch (Q ↔ transcript):** similitud coseno **TF simple** (sin APIs) → `relevance` + `mismatch`.  
  - En UI: card de advertencia **Use anyway** / **Discard & re-record**.
- **Contenedores:** Dockerfiles separados (API y Web) + `docker-compose.yml` (opcional).

### Flujo E2E
1. Registro (email/nombre) → `candidateId`.  
2. UI muestra cada **pregunta** (desde `questions.json`).  
3. Usuario **graba o sube** audio.  
4. **Submit**: Backend hace **STT** → calcula **relevance** → si pasa umbral llama **LLM**.  
5. Devuelve `transcript`, **rúbrica** (PS/SD/Comm), **CEFR + evidencia**, **strengths/concerns** y **5 follow-ups con grounding quote**.  
6. Usuario **Next** (o **Finish** en la última). `/finish` calcula **CEFR final** y envía **email** (no bloqueante).  
7. **Summary** en UI: nivel final y recap de follow-ups (copiable).

---

## Criterios de Evaluación — Cómo se satisfacen

### 1) Funcionalidad y confiabilidad (35 pts)
- **Flujo principal E2E funcional**: registro → preguntas → STT → análisis LLM → insights → finish → summary.  
- **Batch STT (no streaming):** uso de Whisper con archivo completo.  
- **Manejo de errores básicos**:  
  - STT/LLM: **fallbacks** deterministas + mensajes claros en UI.  
  - SMTP: `try/catch` evita caídas si el host es inválido.  
  - “Archivo requerido” y “Submit antes de Next”.  
  - **Mismatch detector** previene respuestas fuera de tema.  
- **Resiliencia**: si no hay `OPENAI_API_KEY`, la demo sigue con **MockAnalysis**.

**Resultado esperado:** *35/35* (flujo sólido + errores básicos manejados).

### 2) Adherencia al brief y calidad del output (25 pts)
- **Requeridos del brief cubiertos**:  
  - **Batch STT** ✅  
  - **5 follow-ups** con **grounding quote** ✅  
  - **Rubric** (Problem-solving, System Design, Communication) con `low/med/high` ✅  
  - **CEFR** con **1–2 evidencias** ✅  
  - **Simple web UI** para grabar/subir y **mostrar resultados** ✅  
  - **Demo** de **single-page app** con transcript + insights + copy de follow-ups ✅
- **Control de salida**: JSON estricto con retries → **outputs claros** y consistentes.

**Resultado esperado:** *25/25*.

### 3) UX y presentación (20 pts)
- **UI intuitiva** con Angular Material: cards limpias, **chips** para rúbrica/CEFR, **progress bar** en análisis.  
- **Flow claro**: **Submit Response** (valida archivo) → **Next Question / Finish Interview**.  
- **Mensajes**: warning por **mismatch** con opciones **Use anyway** o **Re-record**.  
- **Pitch** (≤60s): “Grabas respuesta → STT → LLM genera transcript, rúbrica, CEFR y 5 follow-ups con citas; último paso calcula nivel final y lo envía por email.”

**Resultado esperado:** *20/20* (visual ordenado y claro).

### 4) Calidad técnica y trabajo en equipo (20 pts)
- **Diseño modular**: `Stt.cs`, `Llm.cs`, `Email.cs`, `Relevance.cs`, modelos, endpoints mínimos.  
- **Buenas prácticas**: DI simple, **reintentos** LLM, validación y defaults, separación FE/BE, contenedores.  
- **Colaboración**: tareas separables (FE Material, BE API/LLM, STT/email, DevOps).

**Resultado esperado:** *20/20*.

**Total estimado:** **100/100**.

---

## Decisiones de ingeniería (por qué así)

- **Minimal API + Angular Material**: máximo rendimiento/velocidad de entrega con UI cuidada.  
- **STT batch (Whisper)**: cumple estrictamente el brief; fallback para entornos sin keys.  
- **Salida estructurada** (JSON) con **retries**: reduce errores de formato en demo.  
- **Detección de mismatch** local (sin costo): mejora calidad percibida y evita “respuestas cruzadas”.  
- **Email no bloqueante**: la UX no falla por SMTP; registro en logs.

---

## Variables de entorno y configuración

- `OPENAI_API_KEY` — clave para Whisper/Chat.  
- `USE_MOCK_STT` — `"true"` para forzar STT simulado.  
- `Email__From`, `Email__SmtpHost`, `Email__SmtpPort`, `Email__User`, `Email__Pass` — SMTP (opcional).  
- `ASPNETCORE_URLS=http://+:8080` — puerto API.

---

## Seguridad y privacidad
- Audios y resultados se procesan **en memoria**; no se persisten en disco.  
- Sin datos externos: solo archivos locales del usuario y `questions.json`.  
- Se recomienda HTTPS y expiración de datos en memoria para prod.

---

## Deploy / Runbook corto

```bash
# Backend
cd backend
dotnet run --urls http://localhost:8080

# Frontend
cd frontend/interview-copilot-ui
ng serve --proxy-config proxy.conf.json
```

**Docker (opcional):**
```bash
docker compose up --build
```

---

## Roadmap corto (si hay tiempo extra)
- Resaltado de *grounding quotes* por índices `charStart/charEnd`.  
- CSV export de insights.  
- Almacenamiento en SQLite/Blob Storage.  
- Multi-idioma UI y selección de idioma STT.
