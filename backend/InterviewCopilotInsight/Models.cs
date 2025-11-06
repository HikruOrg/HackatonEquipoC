namespace InterviewCopilotInsight
{
    public record RegisterCandidateReq(string Email, string FirstName, string LastName);
    public record Question(string Id, string Text);
    public record AnalyzeReq(string CandidateId, string QuestionId); // audio via multipart
    public enum Level { A1, A2, B1, B2, C1, C2 }
    public record Rubric(string ProblemSolving, string SystemDesign, string Communication);
    public record FollowUp(string Question, string GroundingQuote, int CharStart, int CharEnd);
    public record AnalysisResult(
      string Transcript, string Language, Rubric Rubric,
      Level CEFRevel, string[] CEFREvidence,
      string[] Strengths, string[] Concerns, List<FollowUp> FollowUps);

    public record CandidateSummary(
      string CandidateId, string Email, string FirstName, string LastName,
      Dictionary<string, AnalysisResult> PerQuestion,
      Level FinalLevel, DateTimeOffset CreatedAt);
}
