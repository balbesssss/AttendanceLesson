using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AttendanceLesson.Api.Service;

public static class ExceptionHandler
{
    public static (int statusCode, string message) GetError (Exception? exception)
    {
    if (exception is DbUpdateException dbex && dbex.InnerException is PostgresException innrePg) exception = innrePg;
    
    if (exception is PostgresException pg)
    {
        var (statusCode, message) = pg.SqlState switch
            {
                "23502" => (400,"Обязательное поле не заполнено"),
                "23503" => (400,"Связанная запись не найдена"),
                "23505" => (409, "Запись уже существует"),
                "23514" => (400,"Нарушено ограничение"),
                _ => (500,"what")
            };
        return (statusCode,message);
    }
    return (500,"Внутренняя ошибка сервера");
    }
}
