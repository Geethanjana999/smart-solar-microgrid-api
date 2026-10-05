/*
 * File        : UsersController.cs
 * Description : Backoffice-only endpoints for managing user accounts.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Backoffice)]
[Route("api/users")]
public sealed class UsersController(IUserService service) : ControllerBase
{
    // GET api/users - list all users.
    [HttpGet]
    public async Task<ActionResult<List<User>>> Get() => Ok(await service.GetAsync());

    // POST api/users - create a user.
    [HttpPost]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    public async Task<ActionResult<User>> Create(UserRequest request)
    {
        var user = await service.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    // PUT api/users/{id} - change role / active flag.
    [HttpPut("{id}")]
    public async Task<ActionResult<User>> Update(string id, UserUpdateRequest request) =>
        Ok(await service.UpdateAsync(id, request));
}
