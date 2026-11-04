using AttendanceLesson.Domain.Models;

namespace AttendanceLesson.Application.DTO.LogBooks;

public class LogRecordDTO
{
    public int UserPlaceId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public int GroupId { get; set; }
    public int LessonId { get; set; }
    public TimeSpan AbsenceDuration { get; set; }
    public bool IsPresent { get; set; }
    public Status? AbsenceReason { get; set; }
}
