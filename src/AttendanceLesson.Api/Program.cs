using AttendanceLesson.Api.Service;
using AttendanceLesson.Application.DTO.Users;
using AttendanceLesson.Application.Interfaces;
using AttendanceLesson.Application.UseCases.Users;
using AttendanceLesson.Domain.Models;
using AttendanceLesson.Infrastructure;
using AttendanceLesson.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("psql")));
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<PasswordHasher <User>>();
builder.Services.AddScoped<IPasswordHasher, AdapterPasswordHasher>();
builder.Services.AddScoped<IUserRepository, UserRepositories>();

builder.Services.AddScoped<CreateUser>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();

app.MapPost("/users", async (CreateUserDTO dto, CreateUser useCase) =>
{
    var user = await useCase.ExecuteAsync(dto);
    return Results.Created($"/users/{user.Id}", user);
});

app.Run();