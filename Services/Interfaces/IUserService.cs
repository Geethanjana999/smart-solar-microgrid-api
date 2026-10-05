/*
 * File        : IUserService.cs
 * Description : Contract for Backoffice user management.
 */
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Services.Interfaces;

public interface IUserService
{
    Task<List<User>> GetAsync();

    Task<User> CreateAsync(UserRequest request);

    Task<User> UpdateAsync(string id, UserUpdateRequest request);
}
