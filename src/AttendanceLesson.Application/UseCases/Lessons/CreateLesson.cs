using AttendanceLesson.Application.DTO.Lessons;
using AttendanceLesson.Application.Interfaces;
using AttendanceLesson.Domain.Models;

namespace AttendanceLesson.Application.UseCases.Lessons;

public class CreateLesson
{
    private readonly ILessonRepository _lessonRepo; 
    public CreateLesson(ILessonRepository lessonRepository)
    {
        _lessonRepo = lessonRepository;
    }

    public async Task <Lesson> ExecuteAsync(CreateLessonDTO dto)
    {
        var lesson = new Lesson(dto.Name);
        await _lessonRepo.AddAsync(lesson);
        await _lessonRepo.SaveChangesAsync();
        return lesson;
    }
}
