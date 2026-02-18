namespace SpeakHalo.Domain.Entities;

public class WhatsAppFunctionCall : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string? Result { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public int ExecutionTimeMs { get; set; }

    public virtual User User { get; set; } = null!;
}
