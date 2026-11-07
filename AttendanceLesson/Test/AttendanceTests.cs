using System.Net;
using System.Net.Http.Json;
using AttendanceLesson.DataBase;
using AttendanceLesson.DataBase.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace AttendanceLesson.Tests;

//  Fixture: реальный PostgreSQL в контейнере, применены миграции, baseline-seed
public class AttendanceFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("attendance_test")
        .WithUsername("admin")
        .WithPassword("admin")
        .Build();

    public int AdminRoleId   { get; private set; }
    public int TeacherRoleId { get; private set; }
    public int StudentRoleId { get; private set; }
    public int MainGroupId   { get; private set; }
    public int MainStudentId { get; private set; }
    public int AltStudentId  { get; private set; }
    public int PlaceUserId   { get; private set; }
    public int MainLessonId  { get; private set; }
    public int AltLessonId   { get; private set; }

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        using var scope = Services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        await db.Database.MigrateAsync();

        var adminRole   = new Role { Name = "admin" };
        var teacherRole = new Role { Name = "teacher" };
        var studentRole = new Role { Name = "student" };
        db.Roles.AddRange(adminRole, teacherRole, studentRole);

        var group = new Group { Name = "k-21" };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        AdminRoleId   = adminRole.Id;
        TeacherRoleId = teacherRole.Id;
        StudentRoleId = studentRole.Id;
        MainGroupId   = group.Id;

        var admin   = new User { Name = "testadmin", RoleId = adminRole.Id };
        admin.PasswordHash = hasher.HashPassword(admin, "password123");

        var student = new User { Name = "student1", RoleId = studentRole.Id, GroupId = group.Id };
        student.PasswordHash = hasher.HashPassword(student, "student123");

        var student2 = new User { Name = "student2", RoleId = studentRole.Id, GroupId = group.Id };
        student2.PasswordHash = hasher.HashPassword(student2, "student123");

        var headman = new User { Name = "headman1", RoleId = studentRole.Id, GroupId = group.Id };
        headman.PasswordHash = hasher.HashPassword(headman, "headman123");

        db.Users.AddRange(admin, student, student2, headman);

        var math    = new Lesson { Name = "math" };
        var physics = new Lesson { Name = "physics" };
        db.Lessons.AddRange(math, physics);

        await db.SaveChangesAsync();

        MainStudentId = student.Id;
        AltStudentId  = student2.Id;
        PlaceUserId   = headman.Id;
        MainLessonId  = math.Id;
        AltLessonId   = physics.Id;
    }

    public async Task<int> CreateLessonAsync(string prefix)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lesson = new Lesson { Name = $"{prefix}_{Guid.NewGuid():N}"[..Math.Min(40, prefix.Length + 33)] };
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();
        return lesson.Id;
    }

    public async Task<int> SeedLogbookAsync(
        int userId, int groupId, int lessonId, int userPlaceId,
        bool isPresent, Status? reason, TimeSpan? duration = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = new Logbook
        {
            UserId          = userId,
            GroupId         = groupId,
            LessonId        = lessonId,
            UserPlaceId     = userPlaceId,
            IsPresent       = isPresent,
            AbsenceReason   = reason,
            AbsenceDuration = duration ?? TimeSpan.Zero,
            CreatedAt       = DateTime.UtcNow
        };
        db.Logbooks.Add(log);
        await db.SaveChangesAsync();
        return log.Id;
    }

    public async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateClient();
        var res = await client.PostAsJsonAsync("/login",
            new { Name = "testadmin", Password = "password123" });
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadAsStringAsync()).Trim('"');
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public override async ValueTask DisposeAsync()
    {
        await _pg.DisposeAsync();
        await base.DisposeAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType == typeof(AppDbContext)
                         || d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(_pg.GetConnectionString()));
        });
    }
}

//  5 ПОЛОЖИТЕЛЬНЫХ тестов учёта посещаемости
public class AttendancePositiveTests : IClassFixture<AttendanceFixture>
{
    private readonly AttendanceFixture _f;
    public AttendancePositiveTests(AttendanceFixture f) => _f = f;

    // POS-1: создать запись об отсутствии с причиной
    [Fact]
    public async Task CreateAbsenceLog_WithReason_PersistsAndReturns201()
    {
        var lessonId = await _f.CreateLessonAsync("pos1");
        var client   = await _f.CreateAdminClientAsync();

        var payload = new
        {
            UserId          = _f.MainStudentId,
            UserPlaceId     = _f.PlaceUserId,
            GroupId         = _f.MainGroupId,
            LessonId        = lessonId,
            AbsenceDuration = TimeSpan.FromMinutes(15),
            IsPresent       = false,
            AbsenceReason   = (int)Status.Overslept // 0
        };

        var response = await client.PostAsJsonAsync("/logbooks/", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Logbooks.SingleAsync(l => l.LessonId == lessonId);
        Assert.Equal(_f.MainStudentId, saved.UserId);
        Assert.Equal(_f.PlaceUserId,   saved.UserPlaceId);
        Assert.False(saved.IsPresent);
        Assert.Equal(Status.Overslept, saved.AbsenceReason);
        Assert.Equal(TimeSpan.FromMinutes(15), saved.AbsenceDuration);
    }

    // POS-2: создать запись о присутствии
    [Fact]
    public async Task CreatePresentLog_WithoutReason_SucceedsByCheckConstraint()
    {
        var lessonId = await _f.CreateLessonAsync("pos2");
        var client   = await _f.CreateAdminClientAsync();

        var payload = new
        {
            UserId          = _f.MainStudentId,
            UserPlaceId     = _f.PlaceUserId,
            GroupId         = _f.MainGroupId,
            LessonId        = lessonId,
            AbsenceDuration = TimeSpan.Zero,
            IsPresent       = true,
            AbsenceReason   = (int?)null
        };

        var response = await client.PostAsJsonAsync("/logbooks/", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Logbooks.SingleAsync(l => l.LessonId == lessonId);
        Assert.True(saved.IsPresent);
        Assert.Null(saved.AbsenceReason);
    }

    // POS-3: GET /logbooks группирует записи по группам
    [Fact]
    public async Task GetLogbooks_GroupsRecordsByGroupName()
    {: две записи в одной группе, у разных студентов
        var lessonA = await _f.CreateLessonAsync("pos3a");
        var lessonB = await _f.CreateLessonAsync("pos3b");
        await _f.SeedLogbookAsync(_f.MainStudentId, _f.MainGroupId, lessonA, _f.PlaceUserId,
                                  isPresent: false, reason: Status.Overslept);
        await _f.SeedLogbookAsync(_f.AltStudentId,  _f.MainGroupId, lessonB, _f.PlaceUserId,
                                  isPresent: false, reason: Status.Forgot);

        var client = await _f.CreateAdminClientAsync();

        var response = await client.GetAsync("/logbooks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var groups = doc.RootElement.EnumerateArray().ToList();

        var k21 = groups.Single(g => g.GetProperty("name").GetString() == "k-21");
        var info = k21.GetProperty("info").EnumerateArray().ToList();
        Assert.Equal(2, info.Count);
        Assert.Contains(info, i => i.GetProperty("lessonId").GetInt32() == lessonA);
        Assert.Contains(info, i => i.GetProperty("lessonId").GetInt32() == lessonB);
    }

    // POS-4: PATCH — отметить отсутствующего как присутствующего
    [Fact]
    public async Task PatchLogbook_MarkStudentPresent_UpdatesInDatabase()
    {
        var lessonId = await _f.CreateLessonAsync("pos4");
        var logId    = await _f.SeedLogbookAsync(
            _f.MainStudentId, _f.MainGroupId, lessonId, _f.PlaceUserId,
            isPresent: false, reason: Status.Overslept);
        var client = await _f.CreateAdminClientAsync();
        var response = await client.PatchAsJsonAsync($"/logbooks/{logId}",
            new { IsPresent = true });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updated = await db.Logbooks.FindAsync(logId);
        Assert.NotNull(updated);
        Assert.True(updated!.IsPresent);
    }

    // POS-5: DELETE — удалить ошибочную запись
    [Fact]
    public async Task DeleteLogbook_RemovesRecordFromDatabase()
    {
        var lessonId = await _f.CreateLessonAsync("pos5");
        var logId    = await _f.SeedLogbookAsync(
            _f.MainStudentId, _f.MainGroupId, lessonId, _f.PlaceUserId,
            isPresent: false, reason: Status.PersonalReason);
        var client = await _f.CreateAdminClientAsync();

        var response = await client.DeleteAsync($"/logbooks/{logId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(await db.Logbooks.FindAsync(logId));
    }
}

//  5 НЕГАТИВНЫХ тестов учёта посещаемости

public class AttendanceNegativeTests : IClassFixture<AttendanceFixture>
{
    private readonly AttendanceFixture _f;
    public AttendanceNegativeTests(AttendanceFixture f) => _f = f;

    private async Task<PostgresException> SaveAndCaptureErrorAsync(Action<Logbook> configure)
    {
        using var scope = Services(scope => scope);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var log = new Logbook
        {
            UserId          = _f.MainStudentId,
            GroupId         = _f.MainGroupId,
            LessonId        = await _f.CreateLessonAsync("neg"),
            UserPlaceId     = _f.PlaceUserId,
            IsPresent       = false,
            AbsenceReason   = Status.Overslept,
            AbsenceDuration = TimeSpan.Zero,
            CreatedAt       = DateTime.UtcNow
        };
        configure(log);

        db.Logbooks.Add(log);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        return Assert.IsType<PostgresException>(ex.InnerException);

        ServiceProvider Services(Func<ServiceProvider, ServiceProvider> f2)
        {
            var s = _f.Services.CreateScope();
            return s.ServiceProvider;
        }
    }

    // NEG-1: отсутствие без AbsenceReason → нарушение CHECK
    [Fact]
    public async Task AbsentWithoutReason_ViolatesCheckConstraint()
    {
        var pgEx = await SaveAndCaptureErrorAsync(l =>
        {
            l.IsPresent     = false;
            l.AbsenceReason = null;
        });

        Assert.Equal("23514", pgEx.SqlState);
        Assert.Contains("CK_Logbook_AbsenceReason", pgEx.ConstraintName ?? "");
    }

    // NEG-2: дубль (UserId, LessonId) → unique violation
    [Fact]
    public async Task DuplicateStudentLesson_ViolatesUniqueIndex()
    {
        var lessonId = await _f.CreateLessonAsync("neg2");
        await _f.SeedLogbookAsync(_f.MainStudentId, _f.MainGroupId, lessonId,
                                  _f.PlaceUserId, isPresent: true, reason: null);

        var pgEx = await SaveAndCaptureErrorAsync(l =>
        {
            l.LessonId  = lessonId;
            l.IsPresent = true;
            l.AbsenceReason = null;
        });

        Assert.Equal("23505", pgEx.SqlState);
    }

    // NEG-3: несуществующий UserId → FK violation
    [Fact]
    public async Task UnknownUserId_ViolatesForeignKey()
    {
        var pgEx = await SaveAndCaptureErrorAsync(l => l.UserId = 999_999);
        Assert.Equal("23503", pgEx.SqlState);
    }

    // NEG-4: несуществующий GroupId → FK violation
    [Fact]
    public async Task UnknownGroupId_ViolatesForeignKey()
    {
        var pgEx = await SaveAndCaptureErrorAsync(l => l.GroupId = 999_999);
        Assert.Equal("23503", pgEx.SqlState);
    }

    // NEG-5: несуществующий LessonId → FK violation
    [Fact]
    public async Task UnknownLessonId_ViolatesForeignKey()
    {
        var pgEx = await SaveAndCaptureErrorAsync(l => l.LessonId = 999_999);
        Assert.Equal("23503", pgEx.SqlState);
    }
}