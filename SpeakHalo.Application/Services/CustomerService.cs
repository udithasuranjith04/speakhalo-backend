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
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IUnitOfWork unitOfWork, IGeocodingService geocodingService, ILogger<CustomerService> logger)
    {
        _unitOfWork = unitOfWork;
        _geocodingService = geocodingService;
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
            {
                var existingName = !string.IsNullOrWhiteSpace(existingCustomer.Name) ? existingCustomer.Name : "Unknown";
                return Result<CustomerResponse>.Failure(
                    $"A customer with phone number {request.Phone} already exists ({existingName})");
            }

            var customer = new Customer
            {
                UserId = request.UserId,
                Phone = request.Phone,
                Name = request.Name ?? string.Empty,
                Email = request.Email,
                Address = request.Address,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode,
                Notes = request.Notes,
                LeadStatus = request.LeadStatus
            };

            var hasAddress = !string.IsNullOrWhiteSpace(request.Address) ||
                             !string.IsNullOrWhiteSpace(request.City) ||
                             !string.IsNullOrWhiteSpace(request.State) ||
                             !string.IsNullOrWhiteSpace(request.ZipCode);

            if (hasAddress)
            {
                var coords = await _geocodingService.GeocodeAddressAsync(
                    request.Address, request.City, request.State, request.ZipCode, cancellationToken);
                if (coords.HasValue)
                {
                    customer.Latitude = coords.Value.Latitude;
                    customer.Longitude = coords.Value.Longitude;
                }
            }

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
            var orderedCustomers = customers.OrderByDescending(c => c.CreatedAt).ToList();

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var totalCount = orderedCustomers.Count;
            var responses = orderedCustomers
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToResponse)
                .ToList();

            var result = new PaginatedList<CustomerResponse>(
                responses,
                totalCount,
                pageNumber,
                pageSize
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

            if (request.Phone != null && request.Phone != customer.Phone)
            {
                var duplicate = (await _unitOfWork.Customers.FindAsync(
                    c => c.UserId == customer.UserId && c.Phone == request.Phone && c.Id != customerId,
                    cancellationToken
                )).FirstOrDefault();
                if (duplicate != null)
                {
                    var dupName = !string.IsNullOrWhiteSpace(duplicate.Name) ? duplicate.Name : "Unknown";
                    return Result<CustomerResponse>.Failure(
                        $"Phone number {request.Phone} is already assigned to another customer ({dupName})");
                }
                customer.Phone = request.Phone;
            }

            if (request.Email != null)
                customer.Email = request.Email;

            var addressChanged = false;

            if (request.Address != null && request.Address != customer.Address)
            {
                customer.Address = request.Address;
                addressChanged = true;
            }

            if (request.City != null && request.City != customer.City)
            {
                customer.City = request.City;
                addressChanged = true;
            }

            if (request.State != null && request.State != customer.State)
            {
                customer.State = request.State;
                addressChanged = true;
            }

            if (request.ZipCode != null && request.ZipCode != customer.ZipCode)
            {
                customer.ZipCode = request.ZipCode;
                addressChanged = true;
            }

            if (request.Notes != null)
                customer.Notes = request.Notes;

            if (request.LeadStatus.HasValue)
                customer.LeadStatus = request.LeadStatus.Value;

            if (request.TotalRevenue.HasValue)
                customer.TotalSpend = request.TotalRevenue.Value;

            if (addressChanged)
            {
                var coords = await _geocodingService.GeocodeAddressAsync(
                    customer.Address, customer.City, customer.State, customer.ZipCode, cancellationToken);
                if (coords.HasValue)
                {
                    customer.Latitude = coords.Value.Latitude;
                    customer.Longitude = coords.Value.Longitude;
                }
            }

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

    public async Task<Result<List<CustomerLocationDto>>> GetCustomerLocationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var customers = await _unitOfWork.Customers.FindAsync(
                c => c.UserId == userId && c.Latitude != null && c.Longitude != null,
                cancellationToken
            );

            var locations = customers.Select(c => new CustomerLocationDto
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Latitude = c.Latitude!.Value,
                Longitude = c.Longitude!.Value,
                Address = string.Join(", ", new[] { c.Address, c.City, c.State, c.ZipCode }.Where(s => !string.IsNullOrWhiteSpace(s))),
                LeadStatus = c.LeadStatus,
                TotalAppointments = c.Appointments.Count,
                TotalRevenue = c.TotalSpend,
                LastContactedAt = c.LastContactedAt
            }).ToList();

            return Result<List<CustomerLocationDto>>.Success(locations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get customer locations for user {UserId}", userId);
            return Result<List<CustomerLocationDto>>.Failure($"Failed to get customer locations: {ex.Message}");
        }
    }

    public async Task<Result<int>> GeocodeAllCustomersAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var customers = await _unitOfWork.Customers.FindAsync(
                c => c.UserId == userId &&
                     c.Latitude == null &&
                     (c.Address != null || c.City != null || c.State != null || c.ZipCode != null),
                cancellationToken
            );

            var geocodedCount = 0;
            foreach (var customer in customers)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var coords = await _geocodingService.GeocodeAddressAsync(
                    customer.Address, customer.City, customer.State, customer.ZipCode, cancellationToken);

                if (coords.HasValue)
                {
                    customer.Latitude = coords.Value.Latitude;
                    customer.Longitude = coords.Value.Longitude;
                    _unitOfWork.Customers.Update(customer);
                    geocodedCount++;
                }
            }

            if (geocodedCount > 0)
                await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Geocoded {Count} customers for user {UserId}", geocodedCount, userId);
            return Result<int>.Success(geocodedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to geocode customers for user {UserId}", userId);
            return Result<int>.Failure($"Failed to geocode customers: {ex.Message}");
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
            City = customer.City,
            State = customer.State,
            ZipCode = customer.ZipCode,
            Latitude = customer.Latitude,
            Longitude = customer.Longitude,
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
