# AttendanceLesson

![CI](https://github.com/balbesssss/AttendanceLesson/actions/workflows/dotnet.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![License](https://img.shields.io/badge/license-MIT-green)

Система учёта посещаемости занятий: роли (администратор, преподаватель, студент), расписание, журнал посещаемости, автоматическая фиксация отсутствующих по окончании урока.


**Ключевые сущности:**

| Сущность | Описание |
|----------|----------|
| `User` | Пользователь (студент/преподаватель/админ), привязка к `Role` и `Group` |
| `Role` | Роль пользователя |
| `Group` | Учебная группа |
| `Lesson` | Дисциплина/урок, привязка к преподавателю (`TeacherId`) |
| `Schedule` | Расписание: группа + урок + дата + время начала/конца |
| `Logbook` | Запись посещаемости: студент + урок + группа + причина отсутствия |

**Фоновая задача `SetAbsence`** каждые 15 минут проверяет завершившиеся уроки и автоматически создаёт записи `Logbook` с `IsPresent = false` для студентов, которые не были отмечены.

---

## 🛠 Стек технологий

- **Платформа:** .NET 10 / ASP.NET Core Web API
- **ORM:** Entity Framework Core + Npgsql (PostgreSQL)
- **Аутентификация:** JWT (access + refresh token)
- **Фоновые задачи:** `BackgroundService`
- **Контейнеризация:** Docker
- **CI:** GitHub Actions (`.github/workflows/dotnet.yml`)
- **Тестирование:** xUnit *(планируется)*

---

## 🚀 Быстрый старт
Клонировать репозиторий
git clone https://github.com/balbesssss/AttendanceLesson.git
cd AttendanceLesson

Сделать контейнер
sudo docker run --name lesson -e POSTGRES_DB=lesson -e POSTGRES_USER=admin -e POSTGRES_PASSWORD=admin -p 5432:5432 -d postgres:16 
Восстановить пакеты
dotnet restore AttendanceLesson/AttendanceLesson.csproj
Применить миграции
dotnet ef database update --project AttendanceLesson/AttendanceLesson.csproj
dotnet run --project AttendanceLesson/AttendanceLesson.csproj
Запустить 
