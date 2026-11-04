using AttendanceLesson.Application.DTO.Users;
using AttendanceLesson.Application.Interfaces;
using AttendanceLesson.Domain.Models;

namespace AttendanceLesson.Application.UseCases.Users;

public class CreateUser(IUserRepository userRepo, IPasswordHasher hasher)
{
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IPasswordHasher _hasher = hasher;

    public async Task<User> ExecuteAsync(CreateUserDTO dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("name must not be empty");
        var exists = await _userRepo.GetUserByNameAsync(dto.Name, ct);
        if (exists is not null) throw new InvalidOperationException("user already exists");
        var user = new User(dto.Name, dto.RoleId, _hasher.Hash(dto.Password));
        await _userRepo.CreateAsync(user, ct);
        return user;
    }
}
