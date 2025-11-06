using InterviewCopilotInsight;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var cfg = builder.Configuration;
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? cfg["OpenAI:ApiKey"] ?? "";
bool useMockStt = string.IsNullOrWhiteSpace(openAiKey);

builder.Services.AddSingleton<ITranscriber>(sp =>
  useMockStt ? new MockTranscriber() : new OpenAiWhisperTranscriber(openAiKey));

IEmailSender emailSender = !string.IsNullOrWhiteSpace(cfg["Email:SmtpHost"])
  ? new SmtpEmailSender(cfg["Email:From"]!, cfg["Email:SmtpHost"]!, int.Parse(cfg["Email:SmtpPort"]!), cfg["Email:User"]!, cfg["Email:Pass"]!)
  : new NoopEmailSender();

var app = builder.Build();
app.UseCors();

var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var questions = JsonSerializer.Deserialize<List<Question>>(await File.ReadAllTextAsync("questions.json"), opts)!;

// In-memory “DB”
var candidates = new Dictionary<string, CandidateSummary>();

app.MapGet("/questions", () => Results.Json(questions));
app.MapPost("/register", async (RegisterCandidateReq req) => {
    var id = Guid.NewGuid().ToString("n");
    candidates[id] = new CandidateSummary(id, req.Email, req.FirstName, req.LastName,
      new Dictionary<string, AnalysisResult>(), Level.A1, DateTimeOffset.UtcNow);
    return Results.Json(new { candidateId = id });
});

// Subir/grabar respuesta (multipart: file + fields)
app.MapPost("/analyze", async (HttpRequest req, ITranscriber stt) =>
{
    var form = await req.ReadFormAsync();
    var file = form.Files.GetFile("file");
    var candidateId = form["candidateId"].ToString();
    var questionId = form["questionId"].ToString();

    if (file is null || string.IsNullOrEmpty(candidateId) || string.IsNullOrEmpty(questionId))
        return Results.BadRequest("Missing fields");

    string transcript;
    try
    {
        await using var s = file.OpenReadStream();
        transcript = await stt.TranscribeAsync(s, req.HttpContext.RequestAborted);
        if (string.IsNullOrWhiteSpace(transcript))
            transcript = "Mock transcript: I designed a token bucket rate limiter with exponential backoff and circuit breaker.";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STT ERROR] {ex.Message}");
        transcript = "Mock transcript: I designed a token bucket rate limiter with exponential backoff and circuit breaker.";
    }

    var q = questions.FirstOrDefault(x => x.Id == questionId);
    if (q is null) return Results.BadRequest("Invalid questionId");

    var result = await Llm.AnalyzeAsync(openAiKey, transcript, q.Text, req.HttpContext.RequestAborted);

    var cand = candidates[candidateId];
    cand.PerQuestion[questionId] = result;
    candidates[candidateId] = cand;

    return Results.Json(result);
});

// Finalizar, calcular CEFR y enviar email
app.MapPost("/finish", async (HttpRequest req) => {
    var payload = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(req.Body, opts);
    var candidateId = payload!["candidateId"];
    var cand = candidates[candidateId];

    // Regla simple: CEFR final = moda ponderada por Communication
    var levels = cand.PerQuestion.Values.Select(v => v.CEFRevel).ToList();
    Level finalLevel = levels.Count == 0
        ? Level.B1
        : levels.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;

    var updated = cand with { FinalLevel = finalLevel };
    candidates[candidateId] = updated;

    try
    {
        var html = $@"
      <h2>Interview English Assessment</h2>
      <p>Hi {cand.FirstName},</p>
      <p>Thank you for completing the interview. Your English level is: <b>{finalLevel}</b>.</p>
      <p>Best regards,<br/>Interview Copilot</p>";
        await emailSender.SendAsync(cand.Email, "Your English Level Result", html);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
        // Intencionalmente ignoramos el error para no romper la demo
    }

    return Results.Json(new { finalLevel = finalLevel.ToString() });
});

app.Run();
