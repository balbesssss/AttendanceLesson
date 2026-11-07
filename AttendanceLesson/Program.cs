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
using AttendanceLesson.Service;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using AttendanceLesson.Extension;

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
    opt.AddPolicy("admin", p => p.RequireRole("admin"));
    opt.AddPolicy("teacher", p=> p.RequireRole("teacher"));
    opt.AddPolicy("headman", p=> p.RequireRole("headman"));
    opt.AddPolicy("student", p=> p.RequireRole("student"));
});
builder.Services.AddRateLimiter(option=> 
    option.AddPolicy("token", http=>
    {
        var key = http.User.Identity?.Name ?? "anon";
        return RateLimitPartition.GetTokenBucketLimiter(key, _=> new TokenBucketRateLimiterOptions
        {
            TokenLimit=100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            TokensPerPeriod = 5,
            AutoReplenishment = true,
            QueueLimit = 5,
        });  
    })
);
builder.Services.AddHostedService<SetAbsence>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IPasswordHasher<CreateUser>, PasswordHasher<CreateUser>>();

var app = builder.Build();
app.UseExceptionHandler(opt=> opt.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (statusCode, message) = ExceptionHandler.GetError(ex);
    context.Response.StatusCode = statusCode;
    await context.Response.WriteAsJsonAsync(new {Message = message});
}));
app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/roles", async (AppDbContext db) => Results.Ok(await db.Roles.ToArrayAsync()))
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Role");

app.MapPost("/roles/", async (AppDbContext db, CreateRole dto) =>
{
    await db.Roles.AddAsync(new Role(dto.Name.ToLower()));
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Role");

app.MapDelete("/roles/{roleId}", async (AppDbContext db, int roleId) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r => r.Id== roleId);
    if (role == null) return Results.NotFound();
    db.Roles.Remove(role);
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Role");

app.MapGet("/roles/{roleId}", async (AppDbContext db, int roleId) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r=> r.Id==roleId);
    return role != null ? Results.Ok(role) : Results.NotFound();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Role");

app.MapPatch("/roles/{roleId}",async (AppDbContext db,  UpdateRole dto,int roleId ) =>
{
    var role = await db.Roles.FirstOrDefaultAsync(r=> r.Id == roleId);
    if (role == null) return Results.NotFound();
    role.SetFields(dto);
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
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
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("User");

app.MapPost("/users/", async (IPasswordHasher<CreateUser> hasher, AppDbContext db, CreateUser dto) =>
{
    dto.Password = hasher.HashPassword(dto, dto.Password);
    await db.Users.AddAsync(new User (dto.Name, dto.RoleId, dto.Password));
    await db.SaveChangesAsync();
    return Results.Ok("User is created");
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
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
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("User");

app.MapGet("/users/{userId}", async (AppDbContext db, int userId) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

    return user != null
        ? Results.Ok(user)
        : Results.NotFound("User does not exist");
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("User");

app.MapPatch("/users/{userId}", async (AppDbContext db, int userId, UpdateUser dto) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    if (user == null)
        return Results.NotFound("User does not exist");

    user.SetFields(dto);
    await db.SaveChangesAsync();

    return Results.Ok("User is edited");
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("User");

app.MapGet("/groups", async (AppDbContext db) => Results.Ok(await db.Groups.ToArrayAsync()))
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Group");

app.MapPost("/groups/", async (AppDbContext db, CreateGroup dto) =>
{
    await db.Groups.AddAsync(new Group (dto.Name.ToLower()));
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Group");

app.MapDelete("/groups/{groupId}", async (AppDbContext db, int groupId) =>
{
    var group = await db.Groups.FirstOrDefaultAsync(g => g.Id== groupId);
    if (group == null) return Results.NotFound();
    db.Groups.Remove(group);
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Group")
.WithDescription("Request to DELETE group by groupId");


app.MapGet("/groups/{groupId}", async (AppDbContext db, int groupId) =>
{
    var group = await db.Groups.FirstOrDefaultAsync(g=> g.Id==groupId);
    return group != null ? Results.Ok(group) : Results.NotFound();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Group")
.WithDescription("Request to GET group by groupId");


app.MapPatch("/groups/{groupId}",async (AppDbContext db, UpdateGroup dto,int groupId) =>
{
    var group = await db.Groups.FirstOrDefaultAsync(g=> g.Id == groupId);
    if (group == null) return Results.NotFound();
    group.SetFields(dto);
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Group")
.WithDescription("Request to PATCH group by groupId");

app.MapGet("/lessons", async (AppDbContext db) => Results.Ok(await db.Lessons.ToArrayAsync()))
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Lesson")
.WithDescription("Request to GET all lessons ");


app.MapPost("/lessons/", async (AppDbContext db, CreateLesson dto) =>
{
    await db.Lessons.AddAsync(new Lesson (dto.Name.ToLower()));
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Lesson")
.WithDescription("Request to POST (add) lesson");


app.MapDelete("/lessons/{lessonId}", async (AppDbContext db, int lessonId) =>
{
    var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id==lessonId );
    if (lesson == null) return Results.NotFound();
    db.Lessons.Remove(lesson);
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Lesson")
.WithDescription("Request to DELETE lesson by lessonId");


app.MapGet("/lessons/{lessonId}", async (AppDbContext db, int lessonId) =>
{
    var lesson = await db.Lessons.FirstOrDefaultAsync(l=> l.Id==lessonId);
    return lesson != null ? Results.Ok(lesson) : Results.NotFound();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Lesson")
.WithDescription("Request to get lesson by lessonId");


app.MapPatch("/lessons/{lessonId}",async (AppDbContext db, UpdateLesson dto, int lessonId) =>
{
    var lesson = await db.Lessons.FirstOrDefaultAsync(l=> l.Id == lessonId);
    if (lesson == null) return Results.NotFound();
    lesson.SetFields(dto); 
    await db.SaveChangesAsync();
    return Results.Ok();
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Lesson")
.WithDescription("Request to PATCH (edit) lesson by lessonId");


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
})
.RequireRateLimiting("token")
.RequireAuthorization("admin")
.WithTags("Logbook")
.WithDescription("Request to GET all logs grouping by group ");


app.MapPost("/logbooks/", async (AppDbContext db , CreateLogBook dto ) =>
{
    await db.Logbooks.AddAsync(new Logbook
    (
        dto.UserPlaceId, DateTime.Now, dto.UserId, dto.GroupId, dto.LessonId,
        dto.AbsenceDuration, dto.IsPresent, dto.AbsenceReason
    ));
    await db.SaveChangesAsync();
    return Results.Created("/logbook","Record is creared");
})
.RequireRateLimiting("token")
.RequireAuthorization(p=> p.RequireRole("admin","teacher","headman"))
.WithTags("Logbook")
.WithDescription("Request to POST (add) log");


app.MapPatch("/logbooks/{logbookId}", async (AppDbContext db, UpdateLogBook dto, int logbookId) =>
{
    var logbook = await db.Logbooks.FirstOrDefaultAsync(l=> l.Id == logbookId);
    if (logbook == null) return Results.NotFound();
    logbook.SetFields(dto);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization(p=> p.RequireRole("admin","teacher","headman"))
.WithTags("Logbook")
.WithDescription("Request to PATCH (edit) log by groupId");

app.MapDelete("/logbooks/{logbookId}", async (AppDbContext db, int logbookId) =>
{
   var logbook = await db.Logbooks.FirstOrDefaultAsync(l=> l.Id == logbookId);
   if (logbook == null) return Results.NotFound();

    db.Logbooks.Remove(logbook);
    await db.SaveChangesAsync();

    return Results.NoContent(); 
})
.RequireRateLimiting("token")
.RequireAuthorization(p=> p.RequireRole("admin","teaсher","headman"))
.WithTags("Logbook")
.WithDescription("Request to DELETE log by logbooId");


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
})
.RequireRateLimiting("token")
.RequireAuthorization(p=> p.RequireRole("admin","teacher","headman"))
.WithTags("Logbook")
.WithDescription("Request to GET log by groupId");

app.MapPost("/login", async (AppDbContext db, IPasswordHasher<User> hasher, HttpContext http, IConfiguration cfg, Login dto) =>
{
    var user = await db.Users
        .Include(u=>u.Role)
        .FirstOrDefaultAsync(u=> u.Name == dto.Name);
    
    if (user == null) return Results.Unauthorized();

    if (hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) != PasswordVerificationResult.Success) 
        return Results.Unauthorized();

    user.UpdateRefreshInfo(RefreshToken.GenerateRefreshToken(), DateTime.UtcNow.AddDays(7));

    await db.SaveChangesAsync();

    http.AppendCookiesInfo("refreshToken", user.RefreshToken , (DateTime)user.RefreshTokenExpires); 

    return Results.Ok(new { accessToken = JWT.GenToken(user, cfg)});
})
.RequireRateLimiting("token");

app.MapPost("/refresh", async (AppDbContext db, HttpContext http, IConfiguration cfg) =>
{
    var refreshToken = http.Request.Cookies["refreshToken"];
    if (refreshToken == null) return Results.NotFound();

    var user = await db.Users
        .Include(u=>u.Role)
        .FirstOrDefaultAsync(u=> u.RefreshToken == refreshToken);
    if (user==null) return Results.Unauthorized();

    user.UpdateRefreshInfo(RefreshToken.GenerateRefreshToken(), DateTime.UtcNow.AddDays(7));

    await db.SaveChangesAsync();

    http.AppendCookiesInfo("refreshToken", user.RefreshToken , (DateTime)user.RefreshTokenExpires); 
    return Results.Ok(new { accessToken = JWT.GenToken(user, cfg)});
})
.RequireRateLimiting("token");

app.MapGet("/users/me", async (AppDbContext db, ClaimsPrincipal claims, HttpContext http) =>
{
    var success = int.TryParse(claims.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
    if (!success) return Results.Unauthorized();
    var user = await db.Users
        .Include(u=> u.Group)
        .Include(u=> u.Role)
        .FirstOrDefaultAsync(u=> u.Id == userId);
    if (user == null) return Results.Unauthorized();
    var userInfo = new
    {
        Id = user.Id,
        Name = user.Name,
        Group = user.Group?.Name ?? "no group" ,
        Role = user.Role.Name
    };
    var json = JsonSerializer.Serialize(userInfo);
    var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

    http.AppendCookiesInfo("userInfo", base64, (DateTime)user.RefreshTokenExpires);

    return Results.NoContent();
})
.RequireRateLimiting("token")
.RequireAuthorization();

app.Run();

public partial class Program { }

