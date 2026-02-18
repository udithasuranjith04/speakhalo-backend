namespace SpeakHalo.Application.Interfaces;

public interface IAIFunctionToolsService
{
    Task<object> BookAppointmentAsync(string userId, BookAppointmentParams parameters, CancellationToken cancellationToken = default);
    Task<object> CreateQuoteAsync(string userId, CreateQuoteParams parameters, CancellationToken cancellationToken = default);
    Task<object> UpdateCustomerAsync(string userId, UpdateCustomerParams parameters, CancellationToken cancellationToken = default);
    Task<object> SearchAvailabilityAsync(string userId, SearchAvailabilityParams parameters, CancellationToken cancellationToken = default);
    Task<object> GetCustomerInfoAsync(string userId, string phoneNumber, CancellationToken cancellationToken = default);
    List<AIFunctionTool> GetAvailableFunctions();
}

public class AIFunctionTool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class BookAppointmentParams
{
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CreateQuoteParams
{
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ServiceDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class UpdateCustomerParams
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
}

public class SearchAvailabilityParams
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ServiceType { get; set; }
}
