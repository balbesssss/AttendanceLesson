namespace AttendanceLesson.Domain.Models;

public class User
{
    public int Id { get; private set;}
    private string _name;
    private string _passwordHash;
    private int _roleId;
    private int? _groupId;
    public string Name 
    {
        get => _name;
        private set 
        {
            if (string.IsNullOrWhiteSpace(value)) 
                throw new ArgumentException("name must not be empty");
            _name = value;
        }
    }
    public int RoleId 
    {
        get => _roleId; 
        private set
        {
            if (value < 0) 
                throw new ArgumentException("roleId must be greater than 0");
            _roleId = value;
        }
    }
    public Role? Role { get; private set; }
    public string PasswordHash 
    {
        get => _passwordHash; 
        private set
        {
            if (string.IsNullOrWhiteSpace(value)) 
                throw new ArgumentException("passwordHash must not be empty");
            _passwordHash = value;
        }
    }
    public int? GroupId 
    {
        get => _groupId;
        private set
        {
            if (value < 0) 
                throw new ArgumentException("groupId must be greater than 0");
            _groupId = value;
        }
    }
    public Group? Group {get; private set;}
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpires {get; private set;}
    public bool IsArchive{ get; private set; }
    public DateTime ArchivedAt { get;private set; }
    private User() {}

    public User(string name, int roleId, string passwordHash)
    {
        Name = name;
        RoleId = roleId;
        PasswordHash = passwordHash;
    }

    public void Archived()
    {
        if (IsArchive) throw new InvalidOperationException("entity already archived");
        IsArchive = true;
        ArchivedAt = DateTime.UtcNow;
    }

    public void ChangePassword (string oldPasswordHash, string newPasswordHash)
    {
        if (PasswordHash != oldPasswordHash) throw new InvalidOperationException("old password incorrent");
        PasswordHash = newPasswordHash;
    }
}
