namespace Taskify.Api.AI;

public sealed class TaskifyAiOptions
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "Ollama";
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "gemma3:4b";
    public int TimeoutSeconds { get; set; } = 25;
}
