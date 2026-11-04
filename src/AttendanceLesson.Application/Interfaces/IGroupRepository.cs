using System;
using AttendanceLesson.Domain.Models;

namespace AttendanceLesson.Application.Interfaces;

public interface IGroupRepository
{
    Task<Group?> GetGroupByIdAsync(int id, CancellationToken ct = default);
    Task<Group?> GetGroupByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Group group, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task DeleteGroupById(int id, CancellationToken ct = default);
}
