using System;
using AttendanceLesson.Domain.Models;

namespace AttendanceLesson.Application.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetRoleByIdAsync(int id, CancellationToken ct = default);
    Task<Role> GetRoleByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Role role, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task DeleteRoleById(int id, CancellationToken ct = default);
}
