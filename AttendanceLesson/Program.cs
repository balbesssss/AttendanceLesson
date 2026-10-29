using System.Text;
using AttendanceLesson.DataBase;
using AttendanceLesson.DTO;
using AttendanceLesson.DataBase.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using AttendanceLesson.Responses;
using Microsoft.AspNetCore.Identity;
using System.Xml;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(opt=> opt.UseNpgsql(builder.Configuration.GetConnectionString("psql")));
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name="Authoriation",
        Description="JWT",
        In=ParameterLocation.Header,
        Type=SecuritySchemeType.Http,
        Scheme=JwtBearerDefaults.AuthenticationScheme,
        BearerFormat="JWT"
    });
    opt.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer",doc)] =[] 
    });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = true,
        ValidateLifetime=true,
        ValidateIssuer=true,
        ValidateIssuerSigningKey=true,
        ValidIssuer=builder.Configuration["JWT:Issuer"],
        ValidAudience=builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)), 
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("admin",p => p.RequireRole("admin"));
    opt.AddPolicy("revizor", p=> p.RequireRole("revizor"));
});

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IPasswordHasher<CreateUser>, PasswordHasher<CreateUser>>();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/roles", async (AppDbContext db) => Results.Ok(await db.Roles.ToArrayAsync()));

app.MapPost("/roles/", async (AppDbContext db, CreateRole dto) =>
{
    await db.Roles.AddAsync(new Role {Name=dto.Name.ToLower()});
    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithTags("Role");

app.MapDelete("/roles/{roleId}", async (AppDbContext db, int roleId) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r => r.Id== roleId);
    if (role == null) return Results.NotFound();
    db.Roles.Remove(role);
    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithTags("Role");

app.MapGet("/roles/{roleId}", async (AppDbContext db, int roleId) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r=> r.Id==roleId);
    return role != null ? Results.Ok(role) : Results.NotFound();
})
.WithTags("Role");

app.MapPatch("/roles/{roleId}",async (AppDbContext db, int roleId, UpdateRole dto ) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r=> r.Id == roleId);
    if (role == null) return Results.NotFound();
    if (dto.Name != null) role.Name=dto.Name; 
    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithTags("Role");

app.MapGet("/users", async (AppDbContext db) =>
{
    var users = await db.Users
        .Include(u => u.Role)
        .Select(u => new UserResponses
        {
            Id = u.Id,
            Name = u.Name,
            Role = u.Role!.Name,
        }).ToArrayAsync();

    return Results.Ok(users);
})
.WithTags("User");

app.MapPost("/users/", async (IPasswordHasher<CreateUser> hasher, AppDbContext db, CreateUser dto) =>
{
    dto.Password = hasher.HashPassword(dto, dto.Password);
    await db.Users.AddAsync(new User
    {
        Name = dto.Name,
        RoleId = dto.RoleId,
        GroupId = dto.GroupId,
        PasswordHash = dto.Password
    });
    await db.SaveChangesAsync();

    return Results.Ok("User is created");
})
.WithTags("User");

app.MapDelete("/users/{userId}", async (AppDbContext db, int userId) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null)
        return Results.NotFound("User not found");

    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.Ok("User is deleted");
})
.WithTags("User");

app.MapGet("/users/{userId}", async (AppDbContext db, int userId) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

    return user != null
        ? Results.Ok(user)
        : Results.NotFound("User does not exist");
})
.WithTags("User");

app.MapPatch("/users/{userId}", async (AppDbContext db, int userId, UpdateUser dto) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null)
        return Results.NotFound("User does not exist");

    if (dto.Name != null) user.Name = dto.Name;
    if (dto.GroupId != null) user.GroupId = dto.GroupId;
    if (dto.RoleId != null) user.RoleId = (int)dto.RoleId;

    await db.SaveChangesAsync();

    return Results.Ok("User is edited");
})
.WithTags("User");

app.MapGet("/groups", async (AppDbContext db) => Results.Ok(await db.Groups.ToArrayAsync()));

app.MapPost("/groups/", async (AppDbContext db, CreateGroup dto) =>
{
    await db.Groups.AddAsync(new Group {Name=dto.Name.ToLower()});
    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithTags("Role");

app.MapDelete("/groups/{groupId}", async (AppDbContext db, int groupId) =>
{
    var group = await db.Groups.FirstOrDefaultAsync(g => g.Id== groupId);
    if (group == null) return Results.NotFound();
    db.Groups.Remove(group);
    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithTags("Role");

app.MapGet("/groups/{groupId}", async (AppDbContext db, int groupId) =>
{
    var group = await db.Groups.FirstOrDefaultAsync(g=> g.Id==groupId);
    return group != null ? Results.Ok(group) : Results.NotFound();
})
.WithTags("Role");

app.MapPatch("/groups/{groupId}",async (AppDbContext db, int groupId, UpdateGroup dto ) =>
{
    var group = await db.Groups.FirstOrDefaultAsync(g=> g.Id == groupId);
    if (group == null) return Results.NotFound();
    if (dto.Name != null) group.Name=dto.Name; 
    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithTags("Role");

app.MapGet("/lessons", async (AppDbContext db) => Results.Ok(await db.Lessons.ToArrayAsync()));

app.MapPost("/lessons/", async (AppDbContext db, CreateGroup dto) =>
{
    await db.Lessons.AddAsync(new Lesson {Name=dto.Name.ToLower()});
    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithTags("Role");

app.MapDelete("/lessons/{lessonId}", async (AppDbContext db, int lessonId) =>
{
    var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id==lessonId );
    if (lesson == null) return Results.NotFound();
    db.Lessons.Remove(lesson);
    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithTags("Role");

app.MapGet("/lessons/{lessonId}", async (AppDbContext db, int lessonId) =>
{
    var lesson = await db.Lessons.FirstOrDefaultAsync(l=> l.Id==lessonId);
    return lesson != null ? Results.Ok(lesson) : Results.NotFound();
})
.WithTags("Role");

app.MapPatch("/lessons/{lessonId}",async (AppDbContext db, int lessonId, UpdateGroup dto ) =>
{
    var lesson = await db.Lessons.FirstOrDefaultAsync(l=> l.Id == lessonId);
    if (lesson == null) return Results.NotFound();
    if (dto.Name != null) lesson.Name=dto.Name; 
    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithTags("Role");

app.MapGet("/logbooks" , async(AppDbContext db) =>
{
    var logs = await db.Logbooks
        .Include(l=> l.Group)
        .Include(l=> l.Lesson)
        .Include(l=> l.User)
        .ToArrayAsync();
    var result = logs.GroupBy(l=> l.Group).Select( r=>  new LogBookToGroup
    {
        Name = r.Key.Name,
        Info = r.Select(a=> new Info
        {
            UseId = a.UserId,
            LessonId = a.LessonId,
            AbsenceDuration = a.AbsenceDuration,
            AbsenceReason = a.AbsenceReason,
            AbsenceTime = a.CreatedAt 
        }).ToArray()
    }
    ).ToArray();
    return Results.Ok(result);
});

app.MapPost("/logbooks/", async (AppDbContext db , CreateLogBook dto) =>
{
    await db.Logbooks.AddAsync(new Logbook
    {
        CreatedAt = DateTime.Now,
        UserId = dto.UserId,
        GroupId = dto.GroupId,
        LessonId = dto.LessonId,
        IsPresent = dto.IsPresent,
        AbsenceDuration = dto.AbsenceDuration,
        AbsenceReason = dto.AbsenceReason
    });
    await db.SaveChangesAsync();
    return Results.Created("/logbook","Record is creared");
});

app.MapPatch("/logbooks/{logbookId}", async (AppDbContext db, UpdateLogBook dto, int logbookId) =>
{
    var logbook = await db.Logbooks.FirstOrDefaultAsync(l=> l.Id == logbookId);
    if (logbook == null) return Results.NotFound();

    if (dto.UserId != null) logbook.UserId = (int)dto.UserId;
    if (dto.GroupId != null) logbook.GroupId = (int)dto.GroupId;
    if (dto.LessonId != null) logbook.LessonId = (int)dto.LessonId;
    if (dto.CreatedAt != null) logbook.CreatedAt = (DateTime)dto.CreatedAt;
    if (dto.IsPresent != null) logbook.IsPresent = (bool)dto.IsPresent;
    if (dto.UserPlaceId != null) logbook.UserPlaceId = (int)dto.UserPlaceId;
    if (dto.AbsenceReason != null) logbook.AbsenceReason = dto.AbsenceReason;
    if (dto.AbsenceDuration != null) logbook.AbsenceDuration = (TimeSpan)dto.AbsenceDuration;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/logbooks/{logbookId}", async (AppDbContext db, UpdateLogBook dto, int logbookId) =>
{
   var logbook = await db.Logbooks.FirstOrDefaultAsync(l=> l.Id == logbookId);
   if (logbook == null) return Results.NotFound();

    db.Logbooks.Remove(logbook);
    await db.SaveChangesAsync();

    return Results.NoContent(); 
});

app.MapGet("/logbooks/{groupId}", async (AppDbContext db, int groupId) =>
{
    var logbook = await db.Logbooks
                        .Where(l=> l.GroupId == groupId)
                        .Include(l=> l.Group) 
                        .Include(l=> l.Lesson)
                        .Include(l=> l.User) 
                        .Include(l=> l.UserPlace)
                        .OrderBy(r=> r.CreatedAt)
                        .ToArrayAsync();
    return Results.Ok(logbook);
});

app.Run();
