USE SportCenterDB;
SET NOCOUNT ON;
BEGIN TRAN;
:r tests/_fixture.sql

DECLARE @err INT, @n INT, @class INT;

IF (SELECT COUNT(*) FROM time_slot) <> 18 THROW 60000, N'FAIL: time_slot must have 18 rows', 1;
IF (SELECT COUNT(DISTINCT CONVERT(VARCHAR(5), start_time) + '-' + CONVERT(VARCHAR(5), end_time)) FROM time_slot) <> 3
    THROW 60000, N'FAIL: time_slot must have 3 distinct time ranges', 1;

-- Sunday slot rejected
SET @err = 0;
BEGIN TRY INSERT INTO time_slot (slot_name, day_of_week, start_time, end_time) VALUES (N'CN', 8, '07:00', '10:00'); END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err = 0 THROW 60000, N'FAIL: Sunday slot must be rejected', 1;

-- 08:00-09:30 rejected
SET @err = 0;
BEGIN TRY INSERT INTO time_slot (slot_name, day_of_week, start_time, end_time) VALUES (N'bad', 2, '08:00', '09:30'); END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err = 0 THROW 60000, N'FAIL: 08:00-09:30 slot must be rejected', 1;

-- room capacity 0 rejected
SET @err = 0;
BEGIN TRY INSERT INTO room (sport_id, room_name, capacity) VALUES (@sport, N'fx_zero', 0); END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err = 0 THROW 60000, N'FAIL: room capacity 0 must be rejected', 1;

-- room without sport rejected
SET @err = 0;
BEGIN TRY INSERT INTO room (sport_id, room_name, capacity) VALUES (NULL, N'fx_nosport', 5); END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err = 0 THROW 60000, N'FAIL: room without sport must be rejected', 1;

-- user gender
SET @err = 0;
BEGIN TRY INSERT INTO [user] (role_id, full_name, email, password_hash, gender)
    VALUES ((SELECT role_id FROM [role] WHERE role_name = N'Member'), N'fx_g', 'fx_g@test.local', 'x', 'X'); END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err = 0 THROW 60000, N'FAIL: gender X must be rejected', 1;

-- sessions: need two classes (UNIQUE class_id, session_no)
INSERT INTO class (course_id, coach_id, room_id, class_name, start_date)
VALUES (@course, @coachA, @roomS, N'fx_class1', '2026-11-02');
SET @class = SCOPE_IDENTITY();
DECLARE @class2 INT;
INSERT INTO class (course_id, coach_id, room_id, class_name, start_date)
VALUES (@course, @coachB, @roomS, N'fx_class2', '2026-11-02');
SET @class2 = SCOPE_IDENTITY();

INSERT INTO session (class_id, session_no, session_date, time_slot_id, coach_id, room_id)
VALUES (@class, 1, '2026-11-02', @slotMon, @coachA, @roomS);

-- same room/date/slot, different coach -> reject
SET @err = 0;
BEGIN TRY INSERT INTO session (class_id, session_no, session_date, time_slot_id, coach_id, room_id)
    VALUES (@class2, 1, '2026-11-02', @slotMon, @coachB, @roomS); END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err = 0 THROW 60000, N'FAIL: duplicate room session must be rejected', 1;

-- same coach/date/slot, different room -> reject
SET @err = 0;
BEGIN TRY INSERT INTO session (class_id, session_no, session_date, time_slot_id, coach_id, room_id)
    VALUES (@class2, 1, '2026-11-02', @slotMon, @coachA, @roomL); END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err = 0 THROW 60000, N'FAIL: duplicate coach session must be rejected', 1;

-- cancelled one does not hold the room/coach
INSERT INTO session (class_id, session_no, session_date, time_slot_id, coach_id, room_id, status)
VALUES (@class2, 1, '2026-11-02', @slotMon, @coachA, @roomS, 'Cancelled');
SELECT @n = COUNT(*) FROM session WHERE class_id = @class2 AND status = 'Cancelled';
IF @n <> 1 THROW 60000, N'FAIL: cancelled session should be insertable', 1;

-- and a Scheduled one is blocked again only by the Scheduled one, not Cancelled
UPDATE session SET status = 'Cancelled' WHERE class_id = @class;
INSERT INTO session (class_id, session_no, session_date, time_slot_id, coach_id, room_id)
VALUES (@class2, 2, '2026-11-02', @slotMon, @coachA, @roomS);

-- attendance column
IF COL_LENGTH('attendance', 'session_id') IS NULL THROW 60000, N'FAIL: attendance.session_id missing', 1;
IF COL_LENGTH('attendance', 'schedule_id') IS NOT NULL THROW 60000, N'FAIL: attendance.schedule_id must be gone', 1;

ROLLBACK;
PRINT 'PASS t01 schema';
GO
