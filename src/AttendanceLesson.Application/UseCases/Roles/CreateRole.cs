using AttendanceLesson.Application.DTO.Roles;
using AttendanceLesson.Application.Interfaces;
using AttendanceLesson.Domain.Models;

namespace AttendanceLesson.Application.UseCases.Roles;

public class CreateRole
{
    private readonly IRoleRepository _roleRepo;

    public CreateRole (IRoleRepository roleRepository)
    {
        _roleRepo = roleRepository;
    }

    public async Task<Role> ExecuteAsync (CreateRoleDTO dto)
    {
        var role = new Role(dto.Name);
        await _roleRepo.AddAsync(role);
        await _roleRepo.SaveChangesAsync();
        return role;
    }
}
