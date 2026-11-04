using AttendanceLesson.Application.Interfaces;
using AttendanceLesson.Domain.Models;

namespace AttendanceLesson.Infrastructure.Repositories;

public class UserRepositories(AppDbContext db) : IUserRepository
{
    private readonly AppDbContext _db = db;

    public async Task CreateAsync(User user, CancellationToken ct = default)
    {
        await _db.Users.AddAsync(user,ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<User?> GetUserByIdAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<User?> GetUserByNameAsync(string name, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(int id, User user, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
