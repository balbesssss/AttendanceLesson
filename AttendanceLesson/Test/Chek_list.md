| № | Действие | Запрос | Ожидаемый результат | Реальный результат |
|---|---|---|---|---|
| 1 | Создать запись об отсутствии с причиной | `POST /logbooks/` тело: `{UserId=student1, UserPlaceId=headman1, GroupId=k-21, LessonId=&lt;new&gt;, AbsenceDuration="00:15:00", IsPresent=false, AbsenceReason=0 (Overslept)}` | 201 Created. В БД 1 запись: UserId=student1, UserPlaceId=headman1, IsPresent=false, AbsenceReason=Overslept, AbsenceDuration=15m |  |
| 2 | Создать запись о присутствии без причины | `POST /logbooks/` тело: `{UserId=student1, UserPlaceId=headman1, GroupId=k-21, LessonId=&lt;new&gt;, AbsenceDuration="00:00:00", IsPresent=true, AbsenceReason=null}` | 201 Created. В БД: IsPresent=true, AbsenceReason=null (CHECK разрешает) |  |
| 3 | Получить сгруппированный журнал | `GET /logbooks` (Bearer admin) | 200 OK. Массив групп. Для k-21 в info ровно 2 записи с lessonId = lessonA и lessonB |  |
| 4 | Отметить отсутствующего как присутствующего | `PATCH /logbooks/{id}` тело: `{IsPresent=true}` | 204 No Content. В БД IsPresent=true |  |
| 5 | Удалить ошибочную запись | `DELETE /logbooks/{id}` | 204 No Content. Записи нет в БД (FindAsync → null) |  |
| 6 | Отсутствие без причины — CHECK-констрейнт | Прямой `db.Logbooks.Add(new Logbook{IsPresent=false, AbsenceReason=null}) + SaveChangesAsync` | DbUpdateException; inner PostgresException.SqlState = "23514", ConstraintName содержит CK_Logbook_AbsenceReason |  |
| 7 | Вторая запись на ту же пару (UserId, LessonId) | Прямой `db.Logbooks.Add(...)` с LessonId, уже существующим для этого UserId | DbUpdateException; inner PostgresException.SqlState = "23505" (unique violation) |  |
| 8 | Несуществующий UserId | Прямой `db.Logbooks.Add(...)` с UserId=999999 | DbUpdateException; inner PostgresException.SqlState = "23503" (FK violation) |  |
| 9 | Несуществующий GroupId | Прямой `db.Logbooks.Add(...)` с GroupId=999999 | DbUpdateException; inner PostgresException.SqlState = "23503" |  |
| 10 | Несуществующий LessonId | Прямой `db.Logbooks.Add(...)` с LessonId=999999 | DbUpdateException; inner PostgresException.SqlState = "23503" |  |
