using System.Text.Json.Serialization;

namespace AttendanceLesson.Domain.Models;

public class Logbook
{
    public int Id { get; private set; }
    private int _userPlacedId;
    public int UserPlaceId 
    { 
        get => _userPlacedId;  
        private set
        {
            if (value < 0) throw new ArgumentException ("userPlaceId must be greter than 0");
            _userPlacedId = value;        
        } 
    }
    public User UserPlace { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private int _userId;
    public int UserId 
    {
        get => _userId;
        private set
        {
            if (value < 0) throw new ArgumentException ("userId must be greter than 0");
            _userId = value;
        }
    }
    public User User { get; private set; }
    private int _groupId;
    public int GroupId 
    {
        get => _groupId;
        private set
        {
            if (value < 0) 
                throw new ArgumentException("groupId must be greater than 0");
            _groupId = value;
        }
    }
    public Group Group {get;private set;}
    private int _lessonId;
    public int LessonId 
    {
        get => _lessonId;
        private set
        {
            if (value < 0) 
                throw new ArgumentException("lessonId must be greater than 0");
            _lessonId = value;
        }
    }
    public Lesson Lesson {get;private set;}
    public TimeSpan AbsenceDuration {get;private set;}
    public bool IsPresent {get;private set;}
    public Status? AbsenceReason {get;private set;}

    private Logbook() {}
    public Logbook
    (
        int userPlaceId, 
        DateTime createdAt, 
        int userId, 
        int groupId, 
        int lessonId, 
        TimeSpan absenceDuration, 
        bool isPresent, 
        Status? absenceReason 
    )
    {
        UserPlaceId = userPlaceId;
        CreatedAt = createdAt;
        UserId = userId;
        GroupId = groupId;
        LessonId = lessonId; 
        AbsenceDuration = absenceDuration;
        IsPresent = isPresent;
        AbsenceReason = absenceReason;
    }
}

public enum Status
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    Overslept,
    PersonalReason,
    Forgot
}