using System;

namespace AttendanceLesson.Domain.Models;

public class Group
{
    public int Id { get; private set; }
    private string _name;
    public string Name 
    {
        get => _name;
        private set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("name of group must not be empty");
            _name = value;
        }
    }
    private Group () {}
    public Group (string name)
    {
        Name = name;
    }
}
