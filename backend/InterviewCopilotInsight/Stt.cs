using System.Net.Http.Headers;
using System.Text.Json;

namespace InterviewCopilotInsight
{
    public interface ITranscriber
    {
        Task<string> TranscribeAsync(Stream audio, CancellationToken ct);
    }

    public class OpenAiWhisperTranscriber : ITranscriber
    {
        private readonly string _apiKey;
        public OpenAiWhisperTranscriber(string apiKey) { _apiKey = apiKey; }

        public async Task<string> TranscribeAsync(Stream audio, CancellationToken ct)
        {
            using var hc = new HttpClient();
            hc.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var form = new MultipartFormDataContent();

            // ⚠️ Importante: NO fijamos ContentType aquí; el server usará el filename.
            var streamContent = new StreamContent(audio);
            form.Add(streamContent, "file", "audio.webm"); // o "audio.mp4"/"audio.mp3" según FE

            form.Add(new StringContent("whisper-1"), "model");

            var resp = await hc.PostAsync("https://api.openai.com/v1/audio/transcriptions", form, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Whisper HTTP {(int)resp.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("text", out var textEl))
                return textEl.GetString() ?? string.Empty;

            if (root.TryGetProperty("error", out var err))
                throw new InvalidOperationException($"Whisper error: {err}");

            throw new InvalidOperationException($"Whisper unexpected payload: {body}");
        }
    }

    public class MockTranscriber : ITranscriber
    {
        public Task<string> TranscribeAsync(Stream audio, CancellationToken ct) =>
          Task.FromResult("This is a mock transcript about designing a rate limiter with token bucket and backoff...");
    }
}
