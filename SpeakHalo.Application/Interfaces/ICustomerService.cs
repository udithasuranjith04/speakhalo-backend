using SpeakHalo.Application.DTOs;
using SpeakHalo.Shared.Helpers;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Interfaces;

public interface ICustomerService
{
    Task<Result<CustomerResponse>> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<Result<CustomerResponse>> GetCustomerByIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<Result<CustomerResponse>> GetCustomerByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<Result<PaginatedList<CustomerResponse>>> GetUserCustomersAsync(string userId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Result<CustomerResponse>> UpdateCustomerAsync(string customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteCustomerAsync(string customerId, CancellationToken cancellationToken = default);
    Task<Result<List<CustomerResponse>>> SearchCustomersAsync(string userId, string searchTerm, CancellationToken cancellationToken = default);
}
