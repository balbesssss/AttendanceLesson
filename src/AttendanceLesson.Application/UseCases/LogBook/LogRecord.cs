using AttendanceLesson.Application.DTO.LogBooks;
using AttendanceLesson.Application.Interfaces;
using AttendanceLesson.Domain.Models;

namespace AttendanceLesson.Application.UseCases.LogBook;

public class LogRecord
{
    private readonly ILogBookRepository _logbookRepo;
    public LogRecord(ILogBookRepository logBookRepository)
    {
        _logbookRepo = logBookRepository;
    }

    public async Task<Logbook> ExecuteAsync(LogRecordDTO dto, CancellationToken token)
    {
        var logbook = new Logbook
        (
            dto.UserPlaceId,
            dto.CreatedAt,
            dto.UserId,
            dto.GroupId,
            dto.LessonId,
            dto.AbsenceDuration,
            dto.IsPresent,
            dto.AbsenceReason
        );
        await _logbookRepo.CreateAsync(logbook, token);
        return logbook;
    }
}
