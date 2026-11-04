namespace AttendanceLesson.Domain.Models;

public class Lesson
{
    public int Id { get; private set; }
    private string _name;
    public string Name 
    {
        get => _name;
        private set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("name of lesson must not be empty");
            _name = value;
        }
    }
    private Lesson () {}
    public Lesson (string name)
    {
        Name = name;
    }
}

