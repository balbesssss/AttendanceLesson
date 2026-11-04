using AttendanceLesson.Domain.Models;
using AttendanceLesson.Application.Interfaces;
using AttendanceLesson.Application.DTO.Groups;


namespace AttendanceLesson.Application.UseCases.Groups;

public class CreateGroup(IGroupRepository groupRepository)
{
    private readonly IGroupRepository _groupRepository = groupRepository;

    public async Task<Group> ExecuteAsync (CreateGroupDTO dto)
    {
        var group = new Group(dto.Name);
        await _groupRepository.AddAsync(group);
        await _groupRepository.SaveChangesAsync();
        return group;
    }

}
