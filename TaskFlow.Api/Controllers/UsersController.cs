using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Application.Services;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Contracts.Mapping;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(UserService userService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request.Name, request.Email, cancellationToken);
        return Created($"/api/v1/users/{user.Id}", user.ToDto());
    }

    [HttpGet]
    [ProducesResponseType<List<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await userService.GetAllAsync(cancellationToken);
        return Ok(users.Select(u => u.ToDto()).ToList());
    }
}
