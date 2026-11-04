namespace AttendanceLesson.Domain.Models;

public class Role
{
    public int Id { get; private set; }
    private string _name;
    public string Name 
    {
        get => _name;
        private set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("name of role must not be empty");
            _name = value;
        }
    }
    private Role () {}
    public Role (string name)
    {
        Name = name;
    }
}
