USE SportCenterDB;
SET NOCOUNT ON;
-- sp_busy_slots is read-only; fixture is rolled back at the end of each batch.
-- Calendar: 2026-11-02 is a Monday, so 11-04 is Wed, 11-09 Mon, 11-11 Wed, 11-16 Mon, 11-18 Wed.

-- ===== Batch 1: room / coach / both, free slot, class end =====
BEGIN TRAN;
:r tests/_fixture.sql

DECLARE @none dbo.IdList, @slots dbo.IdList, @classA INT;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
-- Class A: coachA + roomS, Mon+Wed 07:00 from 2026-11-02, 4 sessions: 11-02, 11-04, 11-09, 11-11
EXEC dbo.sp_create_class @mgr, @course, N't8 class A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
IF @classA IS NULL THROW 60000, N'FAIL: class A not created', 1;

DECLARE @busy TABLE (time_slot_id INT, reason VARCHAR(10));
DECLARE @r VARCHAR(10);

-- Other coach, same room: Mon 07:00 and Wed 07:00 busy because of the Room; Mon 15:00 free
INSERT INTO @busy EXEC dbo.sp_busy_slots @roomS, @coachB, '2026-11-02', @none, 4;
SELECT @r = reason FROM @busy WHERE time_slot_id = @slotMon;
IF ISNULL(@r, '') <> 'Room' THROW 60000, N'FAIL: Mon 07:00 must be busy by Room', 1;
SELECT @r = reason FROM @busy WHERE time_slot_id = @slotWed;
IF ISNULL(@r, '') <> 'Room' THROW 60000, N'FAIL: Wed 07:00 must be busy by Room', 1;
IF EXISTS (SELECT 1 FROM @busy WHERE time_slot_id = @slotMonPm) THROW 60000, N'FAIL: Mon 15:00 must be free', 1;
IF (SELECT COUNT(*) FROM @busy) <> 2 THROW 60000, N'FAIL: exactly 2 busy slots expected (room case)', 1;

-- Same coach, other room: busy by Coach
DELETE FROM @busy;
INSERT INTO @busy EXEC dbo.sp_busy_slots @roomL, @coachA, '2026-11-02', @none, 4;
SELECT @r = reason FROM @busy WHERE time_slot_id = @slotMon;
IF ISNULL(@r, '') <> 'Coach' THROW 60000, N'FAIL: Mon 07:00 must be busy by Coach', 1;

-- Same coach and same room: Both
DELETE FROM @busy;
INSERT INTO @busy EXEC dbo.sp_busy_slots @roomS, @coachA, '2026-11-02', @none, 4;
SELECT @r = reason FROM @busy WHERE time_slot_id = @slotMon;
IF ISNULL(@r, '') <> 'Both' THROW 60000, N'FAIL: Mon 07:00 must be busy by Both', 1;

-- Start after class A ended (last session 11-11): nothing is busy
DELETE FROM @busy;
INSERT INTO @busy EXEC dbo.sp_busy_slots @roomS, @coachA, '2026-11-16', @none, 4;
IF EXISTS (SELECT 1 FROM @busy) THROW 60000, N'FAIL: nothing may be busy after class A ends', 1;

-- A Cancelled session releases its slot: cancel the Mon 11-02 session, Mon 07:00 still busy
-- (11-09 remains), so cancel every Monday session of A and Mon 07:00 must become free for roomS
UPDATE session SET status = 'Cancelled' WHERE class_id = @classA AND time_slot_id = @slotMon;
DELETE FROM @busy;
INSERT INTO @busy EXEC dbo.sp_busy_slots @roomS, @coachB, '2026-11-02', @none, 4;
IF EXISTS (SELECT 1 FROM @busy WHERE time_slot_id = @slotMon) THROW 60000, N'FAIL: Cancelled sessions must not make Mon 07:00 busy', 1;
IF NOT EXISTS (SELECT 1 FROM @busy WHERE time_slot_id = @slotWed) THROW 60000, N'FAIL: Wed 07:00 must stay busy', 1;
ROLLBACK;
GO

-- ===== Batch 2: exact recalculation (adding a slot shortens the schedule) =====
BEGIN TRAN;
:r tests/_fixture.sql

DECLARE @none dbo.IdList, @monOnly dbo.IdList, @other INT;
INSERT INTO @monOnly (id) VALUES (@slotMon);

-- One existing session of roomL on Wed 2026-11-18 07:00 (belongs to another class)
INSERT INTO class (course_id, coach_id, room_id, class_name, start_date, max_capacity)
VALUES (@course, @coachB, @roomL, N't8 other', '2026-11-18', 5);
SET @other = SCOPE_IDENTITY();
INSERT INTO session (class_id, session_no, session_date, time_slot_id, coach_id, room_id, status)
VALUES (@other, 1, '2026-11-18', @slotWed, @coachB, @roomL, 'Scheduled');

DECLARE @busy TABLE (time_slot_id INT, reason VARCHAR(10));

-- Nothing selected, total 4: Wed alone runs 11-04, 11-11, 11-18, 11-25 -> hits 11-18 -> busy;
-- Mon alone runs 11-02, 11-09, 11-16, 11-23 -> free
INSERT INTO @busy EXEC dbo.sp_busy_slots @roomL, @coachA, '2026-11-02', @none, 4;
IF NOT EXISTS (SELECT 1 FROM @busy WHERE time_slot_id = @slotWed AND reason = 'Room') THROW 60000, N'FAIL: Wed alone must be busy by Room', 1;
IF EXISTS (SELECT 1 FROM @busy WHERE time_slot_id = @slotMon) THROW 60000, N'FAIL: Mon alone must be free', 1;

-- Mon already selected: Mon+Wed over total 4 = 11-02 Mon, 11-04 Wed, 11-09 Mon, 11-11 Wed -> Wed is free now
DELETE FROM @busy;
INSERT INTO @busy EXEC dbo.sp_busy_slots @roomL, @coachA, '2026-11-02', @monOnly, 4;
IF EXISTS (SELECT 1 FROM @busy WHERE time_slot_id = @slotWed) THROW 60000, N'FAIL: Wed must be free once Mon is selected (schedule shortens)', 1;
IF EXISTS (SELECT 1 FROM @busy WHERE time_slot_id = @slotMon) THROW 60000, N'FAIL: the selected Mon slot must be free', 1;
ROLLBACK;
GO

-- ===== Batch 3: a selected slot that is itself busy is reported; start on a non-slot weekday =====
BEGIN TRAN;
:r tests/_fixture.sql

DECLARE @wedOnly dbo.IdList, @other INT, @slotsMonWed dbo.IdList;
INSERT INTO @wedOnly (id) VALUES (@slotWed);
INSERT INTO @slotsMonWed (id) VALUES (@slotMon), (@slotWed);
INSERT INTO class (course_id, coach_id, room_id, class_name, start_date, max_capacity)
VALUES (@course, @coachB, @roomL, N't8 other 2', '2026-11-04', 5);
SET @other = SCOPE_IDENTITY();
INSERT INTO session (class_id, session_no, session_date, time_slot_id, coach_id, room_id, status)
VALUES (@other, 1, '2026-11-04', @slotWed, @coachB, @roomL, 'Scheduled');

DECLARE @busy TABLE (time_slot_id INT, reason VARCHAR(10));

-- Wed is selected and clashes with the existing 11-04 session: it must be reported as busy
INSERT INTO @busy EXEC dbo.sp_busy_slots @roomL, @coachA, '2026-11-02', @wedOnly, 4;
IF NOT EXISTS (SELECT 1 FROM @busy WHERE time_slot_id = @slotWed AND reason = 'Room') THROW 60000, N'FAIL: selected busy slot must be reported', 1;
-- Only the tested slot's own dates count: Mon (dates 11-02, 11-09 in the Mon+Wed plan) is free
IF EXISTS (SELECT 1 FROM @busy WHERE time_slot_id <> @slotWed) THROW 60000, N'FAIL: only the tested slot own dates may count', 1;

-- Start on a Tuesday (2026-11-03): the Wed 11-04 session is still reached by a Wed slot
DELETE FROM @busy;
INSERT INTO @busy EXEC dbo.sp_busy_slots @roomL, @coachA, '2026-11-03', @slotsMonWed, 4;
IF NOT EXISTS (SELECT 1 FROM @busy WHERE time_slot_id = @slotWed) THROW 60000, N'FAIL: Tuesday start must still hit Wed 11-04', 1;

-- Empty room/coach that match nothing and total 1: no busy slots
DELETE FROM @busy;
DECLARE @none dbo.IdList;
INSERT INTO @busy EXEC dbo.sp_busy_slots @roomS, @coachB, '2030-01-07', @none, 1;
IF EXISTS (SELECT 1 FROM @busy) THROW 60000, N'FAIL: free calendar must have no busy slots', 1;
ROLLBACK;
GO

-- ===== Batch 4: callable by app_user =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @err INT = 0;
EXECUTE AS USER = 'app_user';
BEGIN TRY
    DECLARE @none dbo.IdList;                      -- app_user must also be able to build the table type itself
    CREATE TABLE #b (time_slot_id INT, reason VARCHAR(10));
    INSERT INTO #b EXEC dbo.sp_busy_slots @roomS, @coachA, '2030-01-07', @none, 4;
END TRY
BEGIN CATCH
    SET @err = ERROR_NUMBER();
END CATCH
REVERT;
IF @err <> 0 THROW 60000, N'FAIL: app_user must be able to EXEC sp_busy_slots', 1;
ROLLBACK;
GO

PRINT 'PASS t08 busy_slots';
GO
