using Microsoft.Extensions.Logging;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.Application.Services;

public class AIFunctionToolsService : IAIFunctionToolsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AIFunctionToolsService> _logger;

    public AIFunctionToolsService(
        IUnitOfWork unitOfWork,
        ILogger<AIFunctionToolsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<object> BookAppointmentAsync(string userId, BookAppointmentParams parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("AI booking appointment for user {UserId}", userId);

            var customers = await _unitOfWork.Repository<Customer>()
                .FindAsync(c => c.UserId == userId && c.Phone == parameters.PhoneNumber, cancellationToken);

            var customer = customers.FirstOrDefault();

            if (customer == null)
            {
                customer = new Customer
                {
                    UserId = userId,
                    Name = parameters.CustomerName,
                    Phone = parameters.PhoneNumber,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<Customer>().AddAsync(customer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (!Enum.TryParse<ServiceType>(parameters.ServiceType, true, out var serviceType))
            {
                serviceType = ServiceType.Cleaning;
            }

            var appointment = new Appointment
            {
                UserId = userId,
                CustomerId = customer.Id,
                ScheduledAt = parameters.ScheduledAt,
                ServiceType = serviceType,
                Notes = parameters.Notes,
                Status = AppointmentStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Appointment>().AddAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new
            {
                success = true,
                appointment_id = appointment.Id,
                scheduled_at = appointment.ScheduledAt,
                message = $"Appointment scheduled for {parameters.CustomerName} on {parameters.ScheduledAt:MMM dd, yyyy 'at' h:mm tt}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking appointment via AI");
            return new { success = false, error = ex.Message };
        }
    }

    public async Task<object> CreateQuoteAsync(string userId, CreateQuoteParams parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("AI creating quote for user {UserId}", userId);

            var customers = await _unitOfWork.Repository<Customer>()
                .FindAsync(c => c.UserId == userId && c.Phone == parameters.PhoneNumber, cancellationToken);

            var customer = customers.FirstOrDefault();

            if (customer == null)
            {
                customer = new Customer
                {
                    UserId = userId,
                    Name = parameters.CustomerName,
                    Phone = parameters.PhoneNumber,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<Customer>().AddAsync(customer, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var quote = new Quote
            {
                UserId = userId,
                CustomerId = customer.Id,
                QuoteNumber = $"QT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
                Notes = parameters.ServiceDescription,
                Total = parameters.Amount,
                Subtotal = parameters.Amount,
                Tax = 0,
                Status = "draft",
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Quote>().AddAsync(quote, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new
            {
                success = true,
                quote_id = quote.Id,
                amount = quote.Total,
                expires_at = quote.ExpiresAt,
                message = $"Quote created for {parameters.CustomerName} - ${parameters.Amount:F2}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quote via AI");
            return new { success = false, error = ex.Message };
        }
    }

    public async Task<object> UpdateCustomerAsync(string userId, UpdateCustomerParams parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("AI updating customer for user {UserId}", userId);

            var customers = await _unitOfWork.Repository<Customer>()
                .FindAsync(c => c.UserId == userId && c.Phone == parameters.PhoneNumber, cancellationToken);

            var customer = customers.FirstOrDefault();

            if (customer == null)
            {
                return new { success = false, error = "Customer not found" };
            }

            if (!string.IsNullOrEmpty(parameters.Name))
                customer.Name = parameters.Name;

            if (!string.IsNullOrEmpty(parameters.Email))
                customer.Email = parameters.Email;

            if (!string.IsNullOrEmpty(parameters.Address))
                customer.Address = parameters.Address;

            if (!string.IsNullOrEmpty(parameters.Notes))
                customer.Notes = parameters.Notes;

            customer.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new
            {
                success = true,
                customer_id = customer.Id,
                message = $"Customer {customer.Name} updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer via AI");
            return new { success = false, error = ex.Message };
        }
    }

    public async Task<object> SearchAvailabilityAsync(string userId, SearchAvailabilityParams parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("AI searching availability for user {UserId}", userId);

            var appointments = await _unitOfWork.Repository<Appointment>()
                .FindAsync(a => a.UserId == userId &&
                              a.ScheduledAt >= parameters.StartDate &&
                              a.ScheduledAt <= parameters.EndDate,
                          cancellationToken);

            var bookedSlots = appointments.Select(a => a.ScheduledAt).ToList();

            var availableSlots = new List<DateTime>();
            var currentDate = parameters.StartDate.Date;

            while (currentDate <= parameters.EndDate.Date)
            {
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    for (int hour = 9; hour < 17; hour++)
                    {
                        var slot = currentDate.AddHours(hour);
                        if (!bookedSlots.Contains(slot) && slot > DateTime.UtcNow)
                        {
                            availableSlots.Add(slot);
                        }
                    }
                }
                currentDate = currentDate.AddDays(1);
            }

            return new
            {
                success = true,
                available_slots = availableSlots.Take(20).Select(s => s.ToString("yyyy-MM-dd HH:mm")),
                count = availableSlots.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching availability via AI");
            return new { success = false, error = ex.Message };
        }
    }

    public async Task<object> GetCustomerInfoAsync(string userId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("AI retrieving customer info for user {UserId}", userId);

            var customers = await _unitOfWork.Repository<Customer>()
                .FindAsync(c => c.UserId == userId && c.Phone == phoneNumber, cancellationToken);

            var customer = customers.FirstOrDefault();

            if (customer == null)
            {
                return new { success = false, message = "Customer not found" };
            }

            return new
            {
                success = true,
                customer = new
                {
                    id = customer.Id,
                    name = customer.Name,
                    phone = customer.Phone,
                    email = customer.Email,
                    address = customer.Address,
                    total_jobs = customer.JobCount,
                    notes = customer.Notes,
                    created_at = customer.CreatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer info via AI");
            return new { success = false, error = ex.Message };
        }
    }

    public List<AIFunctionTool> GetAvailableFunctions()
    {
        return new List<AIFunctionTool>
        {
            new AIFunctionTool
            {
                Name = "book_appointment",
                Description = "Book a new appointment for a customer",
                Parameters = new Dictionary<string, object>
                {
                    { "type", "object" },
                    { "properties", new Dictionary<string, object>
                        {
                            { "customer_name", new { type = "string", description = "Customer's full name" } },
                            { "phone_number", new { type = "string", description = "Customer's phone number" } },
                            { "scheduled_at", new { type = "string", description = "Appointment date and time (ISO 8601)" } },
                            { "service_type", new { type = "string", description = "Type of service needed" } },
                            { "notes", new { type = "string", description = "Additional notes or requirements" } }
                        }
                    },
                    { "required", new[] { "customer_name", "phone_number", "scheduled_at", "service_type" } }
                }
            },
            new AIFunctionTool
            {
                Name = "create_quote",
                Description = "Create a quote for a customer",
                Parameters = new Dictionary<string, object>
                {
                    { "type", "object" },
                    { "properties", new Dictionary<string, object>
                        {
                            { "customer_name", new { type = "string", description = "Customer's full name" } },
                            { "phone_number", new { type = "string", description = "Customer's phone number" } },
                            { "service_description", new { type = "string", description = "Description of services" } },
                            { "amount", new { type = "number", description = "Quote amount" } }
                        }
                    },
                    { "required", new[] { "customer_name", "phone_number", "service_description", "amount" } }
                }
            },
            new AIFunctionTool
            {
                Name = "get_customer_info",
                Description = "Retrieve customer information and history",
                Parameters = new Dictionary<string, object>
                {
                    { "type", "object" },
                    { "properties", new Dictionary<string, object>
                        {
                            { "phone_number", new { type = "string", description = "Customer's phone number" } }
                        }
                    },
                    { "required", new[] { "phone_number" } }
                }
            },
            new AIFunctionTool
            {
                Name = "search_availability",
                Description = "Search for available appointment time slots",
                Parameters = new Dictionary<string, object>
                {
                    { "type", "object" },
                    { "properties", new Dictionary<string, object>
                        {
                            { "start_date", new { type = "string", description = "Start date for search (ISO 8601)" } },
                            { "end_date", new { type = "string", description = "End date for search (ISO 8601)" } }
                        }
                    },
                    { "required", new[] { "start_date", "end_date" } }
                }
            }
        };
    }
}
