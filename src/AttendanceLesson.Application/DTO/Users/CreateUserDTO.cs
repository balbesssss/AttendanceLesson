using System;

namespace AttendanceLesson.Application.DTO.Users;

public class CreateUserDTO
{
    public string Name { get; set; }
    public string Password { get; set; }
    public int RoleId {get;set;}

}
