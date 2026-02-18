using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeakHalo.API.Filters;
using SpeakHalo.Application.DTOs;
using SpeakHalo.Application.Interfaces;
using SpeakHalo.Domain.Entities;
using SpeakHalo.Domain.Enums;
using SpeakHalo.Domain.Interfaces.Repositories;

namespace SpeakHalo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[RequireFeature(PlanFeature.BasicCRM)]
[RequireApiScope("customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IUnitOfWork _unitOfWork;

    public CustomersController(ICustomerService customerService, IUnitOfWork unitOfWork)
    {
        _customerService = customerService;
        _unitOfWork = unitOfWork;
    }

    private async Task<bool> UserOwnsCustomerAsync(string customerId, string userId)
    {
        return await _unitOfWork.Repository<Customer>().AnyAsync(c => c.Id == customerId && c.UserId == userId);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Override the UserId from the request with the authenticated user's ID
        var securedRequest = request with { UserId = userId };

        var result = await _customerService.CreateCustomerAsync(securedRequest, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetCustomerById), new { id = result.Data!.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomerById(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (!await UserOwnsCustomerAsync(id, userId))
            return NotFound();

        var result = await _customerService.GetCustomerByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("phone/{phone}")]
    public async Task<IActionResult> GetCustomerByPhone(string phone, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _customerService.GetCustomerByPhoneAsync(phone, cancellationToken);

        if (!result.IsSuccess)
            return NotFound(result);

        // Verify the returned customer belongs to the authenticated user
        if (result.Data!.UserId != userId)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUserCustomers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _customerService.GetUserCustomersAsync(userId, pageNumber, pageSize, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(string id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (!await UserOwnsCustomerAsync(id, userId))
            return NotFound();

        var result = await _customerService.UpdateCustomerAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (!await UserOwnsCustomerAsync(id, userId))
            return NotFound();

        var result = await _customerService.DeleteCustomerAsync(id, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchCustomers([FromQuery] string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            return BadRequest(new { error = "Search term must be at least 2 characters" });

        if (searchTerm.Length > 100)
            return BadRequest(new { error = "Search term must not exceed 100 characters" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _customerService.SearchCustomersAsync(userId, searchTerm, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}
