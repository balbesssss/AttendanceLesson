using System;
using AttendanceLesson.Domain.Models;

namespace AttendanceLesson.Application.Interfaces;

public interface ILessonRepository
{
    Task<User?> GetLessonByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetLessonByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Lesson lesson, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task DeleteLessonById(int id, CancellationToken ct = default);
}
