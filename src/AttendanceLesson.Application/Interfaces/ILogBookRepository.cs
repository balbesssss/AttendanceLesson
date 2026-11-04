using System;
using AttendanceLesson.Domain.Models;

namespace AttendanceLesson.Application.Interfaces;

public interface ILogBookRepository
{
    Task<Logbook?> GetLogBookByIdAsync(int id, CancellationToken ct = default);
    Task<Logbook?> GetLogBookByNameAsync(string name, CancellationToken ct = default);
    Task CreateAsync(Logbook logbook, CancellationToken ct = default);
    Task DeleteLogBookById(int id, CancellationToken ct = default);
}
