USE SportCenterDB;
SET NOCOUNT ON;
BEGIN TRAN;
:r tests/_fixture.sql

DECLARE @s1 dbo.IdList, @s2 dbo.IdList, @s3 dbo.IdList;
INSERT INTO @s1 VALUES (@slotMon), (@slotWed);
INSERT INTO @s2 VALUES (@slotMon);
INSERT INTO @s3 VALUES (@slotMon), (@slotMonPm);

DECLARE @txt NVARCHAR(500);

-- Mon+Wed from Monday 2026-11-02, 4 sessions
SELECT @txt = STRING_AGG(CONCAT(session_no, N'=', CONVERT(CHAR(10), session_date, 120), N'/', time_slot_id), ',')
    WITHIN GROUP (ORDER BY session_no)
FROM dbo.fn_planned_dates('2026-11-02', @s1, 4);
IF @txt <> CONCAT(N'1=2026-11-02/', @slotMon, N',2=2026-11-04/', @slotWed, N',3=2026-11-09/', @slotMon, N',4=2026-11-11/', @slotWed)
    THROW 60000, N'FAIL: planned dates Mon+Wed', 1;

-- Start on Tuesday, Mon slot: first session is the next Monday
SELECT @txt = STRING_AGG(CONVERT(CHAR(10), session_date, 120), ',') WITHIN GROUP (ORDER BY session_no)
FROM dbo.fn_planned_dates('2026-11-03', @s2, 2);
IF @txt <> N'2026-11-09,2026-11-16' THROW 60000, N'FAIL: planned dates skip start date', 1;

-- Two slots on same day ordered by start_time
SELECT @txt = STRING_AGG(CONCAT(CONVERT(CHAR(10), session_date, 120), N'/', time_slot_id), ',') WITHIN GROUP (ORDER BY session_no)
FROM dbo.fn_planned_dates('2026-11-02', @s3, 3);
IF @txt <> CONCAT(N'2026-11-02/', @slotMon, N',2026-11-02/', @slotMonPm, N',2026-11-09/', @slotMon)
    THROW 60000, N'FAIL: planned dates two slots same day', 1;

-- Existing active session for coachA / roomS
DECLARE @class INT;
INSERT INTO class (course_id, coach_id, room_id, class_name, start_date)
VALUES (@course, @coachA, @roomS, N'fx_class', '2026-11-02');
SET @class = SCOPE_IDENTITY();
INSERT INTO session (class_id, session_no, session_date, time_slot_id, coach_id, room_id)
VALUES (@class, 1, '2026-11-09', @slotMon, @coachA, @roomS);

IF EXISTS (SELECT 1 FROM dbo.fn_free_coaches(@sport, '2026-11-02', @s1, 4) WHERE user_id = @coachA)
    THROW 60000, N'FAIL: busy coachA must be excluded', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.fn_free_coaches(@sport, '2026-11-02', @s1, 4) WHERE user_id = @coachB)
    THROW 60000, N'FAIL: coachB must be free', 1;
IF EXISTS (SELECT 1 FROM dbo.fn_free_coaches(@sport, '2026-11-02', @s1, 4) WHERE user_id = @coachX)
    THROW 60000, N'FAIL: coachX (other sport) must be excluded', 1;

IF EXISTS (SELECT 1 FROM dbo.fn_free_rooms(@sport, 5, '2026-11-02', @s1, 4) WHERE room_id = @roomS)
    THROW 60000, N'FAIL: busy roomS must be excluded', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.fn_free_rooms(@sport, 5, '2026-11-02', @s1, 4) WHERE room_id = @roomL)
    THROW 60000, N'FAIL: roomL must be free', 1;
IF (SELECT COUNT(*) FROM dbo.fn_free_rooms(@sport, 20, '2026-11-02', @s1, 4) WHERE room_id IN (@roomS, @roomL)) <> 1
   OR NOT EXISTS (SELECT 1 FROM dbo.fn_free_rooms(@sport, 20, '2026-11-02', @s1, 4) WHERE room_id = @roomL)
    THROW 60000, N'FAIL: capacity 20 must give only roomL', 1;
IF EXISTS (SELECT 1 FROM dbo.fn_free_rooms(@sport, 5, '2026-11-02', @s1, 4) WHERE room_id = @roomX)
    THROW 60000, N'FAIL: roomX (other sport) must be excluded', 1;

-- Cancelled session frees them
UPDATE session SET status = 'Cancelled' WHERE class_id = @class;
IF NOT EXISTS (SELECT 1 FROM dbo.fn_free_coaches(@sport, '2026-11-02', @s1, 4) WHERE user_id = @coachA)
    THROW 60000, N'FAIL: coachA free after cancel', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.fn_free_rooms(@sport, 5, '2026-11-02', @s1, 4) WHERE room_id = @roomS)
    THROW 60000, N'FAIL: roomS free after cancel', 1;

-- Non-overlapping period
UPDATE session SET status = 'Scheduled' WHERE class_id = @class;
IF NOT EXISTS (SELECT 1 FROM dbo.fn_free_coaches(@sport, '2026-12-07', @s1, 4) WHERE user_id = @coachA)
    THROW 60000, N'FAIL: coachA free in later period', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.fn_free_rooms(@sport, 5, '2026-12-07', @s1, 4) WHERE room_id = @roomS)
    THROW 60000, N'FAIL: roomS free in later period', 1;

ROLLBACK;
PRINT 'PASS t02 functions';
GO
