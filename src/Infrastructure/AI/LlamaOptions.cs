namespace IntelliImport.Infrastructure.AI;

public sealed class LlamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.1:8b";
    public decimal Temperature { get; set; } = 0.3m;
    public int TimeoutSecs { get; set; } = 180;
}