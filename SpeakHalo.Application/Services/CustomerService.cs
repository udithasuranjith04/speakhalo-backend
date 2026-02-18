using Microsoft.Extensions.Logging;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Interfaces.Repositories;
using SpeakHalo.Shared.Constants;
using SpeakHalo.Shared.Extensions;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IUnitOfWork unitOfWork, ILogger<CustomerService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CustomerResponse>> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingCustomer = (await _unitOfWork.Customers.FindAsync(
                c => c.UserId == request.UserId && c.Phone == request.Phone,
                cancellationToken
            )).FirstOrDefault();

            if (existingCustomer != null)
                return Result<CustomerResponse>.Failure(ErrorMessages.AlreadyExists("Customer"));

            var customer = new Customer
            {
                UserId = request.UserId,
                Phone = request.Phone,
                Name = request.Name ?? string.Empty,
                Email = request.Email,
                Address = request.Address,
                Notes = request.Notes,
                LeadStatus = request.LeadStatus
            };

            await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CustomerResponse>.Success(MapToResponse(customer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create customer for user {UserId}", request.UserId);
            return Result<CustomerResponse>.Failure($"Failed to create customer: {ex.Message}");
        }
    }

    public async Task<Result<CustomerResponse>> GetCustomerByIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken);

            if (customer == null)
                return Result<CustomerResponse>.Failure(ErrorMessages.NotFound("Customer"));

            return Result<CustomerResponse>.Success(MapToResponse(customer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get customer {CustomerId}", customerId);
            return Result<CustomerResponse>.Failure($"Failed to get customer: {ex.Message}");
        }
    }

    public async Task<Result<CustomerResponse>> GetCustomerByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = (await _unitOfWork.Customers.FindAsync(c => c.Phone == phone, cancellationToken)).FirstOrDefault();

            if (customer == null)
                return Result<CustomerResponse>.Failure(ErrorMessages.NotFound("Customer"));

            return Result<CustomerResponse>.Success(MapToResponse(customer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get customer by phone");
            return Result<CustomerResponse>.Failure($"Failed to get customer: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<CustomerResponse>>> GetUserCustomersAsync(string userId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var customers = await _unitOfWork.Customers.FindAsync(c => c.UserId == userId, cancellationToken);
            var orderedCustomers = customers.OrderByDescending(c => c.CreatedAt);

            var paginatedCustomers = await orderedCustomers
                .AsQueryable()
                .ToPaginatedListAsync(pageNumber, pageSize, cancellationToken);

            var responses = paginatedCustomers.Items.Select(MapToResponse).ToList();

            var result = new PaginatedList<CustomerResponse>(
                responses,
                paginatedCustomers.TotalCount,
                paginatedCustomers.CurrentPage,
                paginatedCustomers.PageSize
            );

            return Result<PaginatedList<CustomerResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get customers for user {UserId}", userId);
            return Result<PaginatedList<CustomerResponse>>.Failure($"Failed to get customers: {ex.Message}");
        }
    }

    public async Task<Result<CustomerResponse>> UpdateCustomerAsync(string customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken);

            if (customer == null)
                return Result<CustomerResponse>.Failure(ErrorMessages.NotFound("Customer"));

            if (request.Name != null)
                customer.Name = request.Name;

            if (request.Phone != null)
                customer.Phone = request.Phone;

            if (request.Email != null)
                customer.Email = request.Email;

            if (request.Address != null)
                customer.Address = request.Address;

            if (request.Notes != null)
                customer.Notes = request.Notes;

            if (request.LeadStatus.HasValue)
                customer.LeadStatus = request.LeadStatus.Value;

            if (request.TotalRevenue.HasValue)
                customer.TotalSpend = request.TotalRevenue.Value;

            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CustomerResponse>.Success(MapToResponse(customer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update customer {CustomerId}", customerId);
            return Result<CustomerResponse>.Failure($"Failed to update customer: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteCustomerAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken);

            if (customer == null)
                return Result<bool>.Failure(ErrorMessages.NotFound("Customer"));

            _unitOfWork.Customers.Remove(customer);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete customer {CustomerId}", customerId);
            return Result<bool>.Failure($"Failed to delete customer: {ex.Message}");
        }
    }

    public async Task<Result<List<CustomerResponse>>> SearchCustomersAsync(string userId, string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            var customers = await _unitOfWork.Customers.FindAsync(
                c => c.UserId == userId && (
                    (c.Name != null && c.Name.Contains(searchTerm)) ||
                    c.Phone.Contains(searchTerm) ||
                    (c.Email != null && c.Email.Contains(searchTerm))
                ),
                cancellationToken
            );

            var responses = customers
                .OrderByDescending(c => c.LastContactedAt)
                .Select(MapToResponse)
                .ToList();

            return Result<List<CustomerResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search customers for user {UserId}", userId);
            return Result<List<CustomerResponse>>.Failure($"Failed to search customers: {ex.Message}");
        }
    }

    private static CustomerResponse MapToResponse(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            UserId = customer.UserId,
            Phone = customer.Phone,
            Name = customer.Name,
            Email = customer.Email,
            Address = customer.Address,
            Notes = customer.Notes,
            LeadStatus = customer.LeadStatus,
            TotalCalls = 0,
            TotalAppointments = customer.Appointments.Count,
            TotalRevenue = customer.TotalSpend,
            LastContactedAt = customer.LastContactedAt,
            CreatedAt = customer.CreatedAt
        };
    }
}
