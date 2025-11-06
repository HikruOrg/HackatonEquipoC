using System.Text.Json;

namespace InterviewCopilotInsight
{
    public static class Llm
    {
        public static async Task<AnalysisResult> AnalyzeAsync(
          string openAiKey, string transcript, string questionText, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(openAiKey))
                return MockAnalysis(transcript, questionText);
            var system = """
You are an interviewing copilot. Return STRICT minified JSON only with this schema:
{
 "followUps":[{"question":"...","groundingQuote":"...","charStart":0,"charEnd":0}],
 "strengths":["..."], "concerns":["..."],
 "rubric":{"problemSolving":"low|med|high","systemDesign":"low|med|high","communication":"low|med|high"},
 "cefr":{"level":"A1|A2|B1|B2|C1|C2","evidence":["..."]}
}
Rules:
- 5 follow-ups total, each MUST include a short grounding quote (<=30 words) and character indices on the transcript.
- Be concise, no explanations, only the JSON.
""";

            var user = $"""
QUESTION: {questionText}
TRANSCRIPT:
{transcript}
""";

            var json = await GetJsonWithRetries(openAiKey, system, user, ct);

            var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var rubric = new Rubric(
                root.GetProperty("rubric").GetProperty("problemSolving").GetString()!,
                root.GetProperty("rubric").GetProperty("systemDesign").GetString()!,
                root.GetProperty("rubric").GetProperty("communication").GetString()!
            );

            var cefrL = root.GetProperty("cefr").GetProperty("level").GetString()!;
            var lvl = Enum.Parse<Level>(cefrL.Replace("+", ""), ignoreCase: true);

            var evid = root.GetProperty("cefr").GetProperty("evidence").EnumerateArray()
                .Select(x => x.GetString()!).ToArray();

            var strengths = root.GetProperty("strengths").EnumerateArray().Select(x => x.GetString()!).ToArray();
            var concerns = root.GetProperty("concerns").EnumerateArray().Select(x => x.GetString()!).ToArray();

            var fus = root.GetProperty("followUps").EnumerateArray()
                .Select(x => new FollowUp(
                    x.GetProperty("question").GetString()!,
                    x.GetProperty("groundingQuote").GetString()!,
                    x.GetProperty("charStart").GetInt32(),
                    x.GetProperty("charEnd").GetInt32()
                )).ToList();

            return new AnalysisResult(
                Transcript: transcript,
                Language: "en",
                Rubric: rubric,
                CEFRevel: lvl,
                CEFREvidence: evid,
                Strengths: strengths,
                Concerns: concerns,
                FollowUps: fus
            );
        }

        static async Task<string> GetJsonWithRetries(string key, string system, string user, CancellationToken ct)
        {
            var last = "";
            for (int i = 0; i < 3; i++)
            {
                var raw = await Chat(key, system, user, ct); last = raw;
                raw = Strip(raw);
                if (IsValidJson(raw)) return raw;
                user = user + "\nYour previous output was invalid JSON. Return ONLY a valid minified JSON object per schema.";
            }
            throw new InvalidOperationException($"LLM invalid JSON. Last: {last}");
        }

        static async Task<string> Chat(string key, string system, string user, CancellationToken ct)
        {
            using var hc = new HttpClient();
            hc.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);

            var payload = new
            {
                model = "gpt-4o-mini",
                temperature = 0.1,
                messages = new object[] {
            new { role = "system", content = system },
            new { role = "user", content = user }
        }
            };

            var resp = await hc.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"), ct);

            var json = await resp.Content.ReadAsStringAsync(ct);

            // 🛡️ Manejo de error HTTP/JSON de OpenAI
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"OpenAI HTTP {(int)resp.StatusCode}: {json}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                if (root.TryGetProperty("error", out var err))
                    throw new InvalidOperationException($"OpenAI error: {err.ToString()}");
                throw new InvalidOperationException("OpenAI returned no choices.");
            }

            return choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }

        static string Strip(string s)
        {
            s = s.Trim();
            if (s.StartsWith("```")) { var i = s.IndexOf('\n'); if (i >= 0) s = s[(i + 1)..]; if (s.EndsWith("```")) s = s[..^3]; }
            return s.Trim();
        }
        static bool IsValidJson(string s) { try { JsonDocument.Parse(s); return true; } catch { return false; } }

        static AnalysisResult MockAnalysis(string transcript, string questionText)
        {
            var rubric = new Rubric("med", "med", "high");
            var follows = Enumerable.Range(1, 5).Select(i =>
                new FollowUp($"Follow-up #{i} about: {questionText}",
                             "“I used a token bucket and retries…”", 0, Math.Min(30, transcript.Length))
            ).ToList();

            return new AnalysisResult(
                Transcript: transcript,
                Language: "en",
                Rubric: rubric,
                CEFRevel: Level.B2,
                CEFREvidence: new[] { "Mostly fluent with technical vocabulary", "Minor grammatical issues" },
                Strengths: new[] { "Clear explanations", "Good structure" },
                Concerns: new[] { "Limited failure modes", "Sparse metrics" },
                FollowUps: follows
            );
        }
    }
}
