using AttendanceLesson.Application.Interfaces;
using AttendanceLesson.Domain.Models;
using Microsoft.AspNetCore.Identity;


namespace AttendanceLesson.Api.Service;

public class AdapterPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public bool Verify(string password, string hash)
    {
        var result = _hasher.VerifyHashedPassword(null!, hash, password);
        return result == PasswordVerificationResult.Success;
    }
}
