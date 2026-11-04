using System;
using AttendanceLesson.Domain.Models;

namespace AttendanceLesson.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetUserByNameAsync(string name, CancellationToken ct = default);
    Task CreateAsync(User user, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task UpdateAsync(int id, User user, CancellationToken ct = default);

}
