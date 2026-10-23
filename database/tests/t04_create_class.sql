USE SportCenterDB;
SET NOCOUNT ON;
-- sp_create_class uses XACT_ABORT ON, so an expected error dooms the test transaction.
-- Each expected-error case therefore runs in its own batch (own BEGIN TRAN / fixture / ROLLBACK).

-- ===== Batch 1: happy paths =====
BEGIN TRAN;
:r tests/_fixture.sql

DECLARE @slots dbo.IdList, @slotsMon dbo.IdList;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
INSERT INTO @slotsMon (id) VALUES (@slotMon);
DECLARE @c1 INT, @c2 INT, @c3 INT, @c4 INT, @n INT, @d DATE, @st VARCHAR(20);

-- Class 1: start Mon 2026-11-02, slots Mon+Wed, 4 sessions
EXEC dbo.sp_create_class @mgr, @course, N't4 class 1', '2026-11-02', 8, @coachA, @roomS, @slots, @c1 OUTPUT;
IF @c1 IS NULL THROW 60000, N'FAIL: class_id output is NULL', 1;

SELECT @n = COUNT(*) FROM session WHERE class_id = @c1;
IF ISNULL(@n, 0) <> 4 THROW 60000, N'FAIL: class 1 must have 4 sessions', 1;

SELECT @n = COUNT(*) FROM session
WHERE class_id = @c1
  AND coach_id = @coachA AND room_id = @roomS AND status = 'Scheduled'
  AND ((session_no = 1 AND session_date = '2026-11-02' AND time_slot_id = @slotMon)
    OR (session_no = 2 AND session_date = '2026-11-04' AND time_slot_id = @slotWed)
    OR (session_no = 3 AND session_date = '2026-11-09' AND time_slot_id = @slotMon)
    OR (session_no = 4 AND session_date = '2026-11-11' AND time_slot_id = @slotWed));
IF ISNULL(@n, 0) <> 4 THROW 60000, N'FAIL: class 1 session dates/slots/coach/room/status', 1;

SELECT @d = end_date, @st = status FROM class
WHERE class_id = @c1 AND course_id = @course AND coach_id = @coachA AND room_id = @roomS
  AND start_date = '2026-11-02' AND max_capacity = 8 AND class_name = N't4 class 1';
IF @d IS NULL OR @d <> '2026-11-11' THROW 60000, N'FAIL: class 1 end_date must be 2026-11-11', 1;
IF ISNULL(@st, '') <> 'Enrolling' THROW 60000, N'FAIL: class 1 status must be Enrolling', 1;

SELECT @n = COUNT(*) FROM class_time_slot WHERE class_id = @c1 AND time_slot_id IN (@slotMon, @slotWed);
IF ISNULL(@n, 0) <> 2 THROW 60000, N'FAIL: class 1 must have 2 class_time_slot rows', 1;
SELECT @n = COUNT(*) FROM class_time_slot WHERE class_id = @c1;
IF ISNULL(@n, 0) <> 2 THROW 60000, N'FAIL: class 1 must have exactly 2 class_time_slot rows', 1;

-- Class 2: start Tue 2026-11-03 with Mon slot -> first session is Mon 2026-11-09
EXEC dbo.sp_create_class @mgr, @course, N't4 class 2', '2026-11-03', 8, @coachB, @roomL, @slotsMon, @c2 OUTPUT;
SET @d = NULL;
SELECT @d = session_date FROM session WHERE class_id = @c2 AND session_no = 1;
IF @d IS NULL OR @d <> '2026-11-09' THROW 60000, N'FAIL: class 2 first session must be 2026-11-09', 1;
SELECT @n = COUNT(*) FROM session WHERE class_id = @c2 AND session_date < '2026-11-09';
IF ISNULL(@n, -1) <> 0 THROW 60000, N'FAIL: class 2 has session before 2026-11-09', 1;
SET @d = NULL;
SELECT @d = end_date FROM class WHERE class_id = @c2;
IF @d IS NULL OR @d <> '2026-11-30' THROW 60000, N'FAIL: class 2 end_date must be 2026-11-30', 1;

-- Class 3: same slots/room/coach as class 1, starts after class 1 ends -> no conflict
EXEC dbo.sp_create_class @mgr, @course, N't4 class 3', '2026-11-16', 8, @coachA, @roomS, @slots, @c3 OUTPUT;
SELECT @n = COUNT(*) FROM session WHERE class_id = @c3 AND session_date BETWEEN '2026-11-16' AND '2026-11-25';
IF @c3 IS NULL OR ISNULL(@n, 0) <> 4 THROW 60000, N'FAIL: class 3 (non-overlapping period) must succeed with 4 sessions', 1;

-- Cancelled session frees coach and room: cancel class 1 session on 2026-11-02,
-- then a 1-session course class on that exact date/slot/coach/room succeeds.
DECLARE @course1 INT;
INSERT INTO course (sport_id, course_name, total_sessions, tuition_fee) VALUES (@sport, N't4_one_session', 1, 0);
SET @course1 = SCOPE_IDENTITY();
UPDATE session SET status = 'Cancelled' WHERE class_id = @c1 AND session_date = '2026-11-02';
EXEC dbo.sp_create_class @mgr, @course1, N't4 class 4', '2026-11-02', 8, @coachA, @roomS, @slotsMon, @c4 OUTPUT;
SELECT @n = COUNT(*) FROM session
WHERE class_id = @c4 AND session_date = '2026-11-02' AND time_slot_id = @slotMon
  AND coach_id = @coachA AND room_id = @roomS AND status = 'Scheduled';
IF @c4 IS NULL OR ISNULL(@n, 0) <> 1 THROW 60000, N'FAIL: cancelled session must free coach/room', 1;

ROLLBACK;
GO

-- ===== Batch 2: coach busy (50006), nothing written =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @c1 INT, @cid INT, @err INT;
DECLARE @cls0 INT, @ses0 INT, @cts0 INT, @cls1 INT, @ses1 INT, @cts1 INT;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't4 base', '2026-11-02', 8, @coachA, @roomS, @slots, @c1 OUTPUT;
SELECT @cls0 = (SELECT COUNT(*) FROM class), @ses0 = (SELECT COUNT(*) FROM session), @cts0 = (SELECT COUNT(*) FROM class_time_slot);
SET @err = 0;
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N't4 busy coach', '2026-11-02', 8, @coachA, @roomL, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
SELECT @cls1 = (SELECT COUNT(*) FROM class), @ses1 = (SELECT COUNT(*) FROM session), @cts1 = (SELECT COUNT(*) FROM class_time_slot);
IF @err <> 50006 THROW 60000, N'FAIL: busy coach must be 50006', 1;
IF @cls1 <> @cls0 OR @ses1 <> @ses0 OR @cts1 <> @cts0 THROW 60000, N'FAIL: 50006 must write nothing', 1;
ROLLBACK;
GO

-- ===== Batch 3: room busy (50007), nothing written =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @c1 INT, @cid INT, @err INT;
DECLARE @cls0 INT, @ses0 INT, @cts0 INT, @cls1 INT, @ses1 INT, @cts1 INT;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't4 base', '2026-11-02', 8, @coachA, @roomS, @slots, @c1 OUTPUT;
SELECT @cls0 = (SELECT COUNT(*) FROM class), @ses0 = (SELECT COUNT(*) FROM session), @cts0 = (SELECT COUNT(*) FROM class_time_slot);
SET @err = 0;
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N't4 busy room', '2026-11-02', 8, @coachB, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
SELECT @cls1 = (SELECT COUNT(*) FROM class), @ses1 = (SELECT COUNT(*) FROM session), @cts1 = (SELECT COUNT(*) FROM class_time_slot);
IF @err <> 50007 THROW 60000, N'FAIL: busy room must be 50007', 1;
IF @cls1 <> @cls0 OR @ses1 <> @ses0 OR @cts1 <> @cts0 THROW 60000, N'FAIL: 50007 must write nothing', 1;
ROLLBACK;
GO

-- ===== Batch 4: actor is Member -> 50001 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
BEGIN TRY EXEC dbo.sp_create_class @member, @course, N'x', '2026-11-02', 8, @coachA, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50001 THROW 60000, N'FAIL: member actor must be 50001', 1;
IF (SELECT COUNT(*) FROM class WHERE class_name = N'x') <> 0 THROW 60000, N'FAIL: 50001 must write nothing', 1;
ROLLBACK;
GO

-- ===== Batch 5: inactive Manager -> 50001 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
UPDATE [user] SET status = 'Inactive' WHERE user_id = @mgr;
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 8, @coachA, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50001 THROW 60000, N'FAIL: inactive manager must be 50001', 1;
ROLLBACK;
GO

-- ===== Batch 6: course Inactive -> 50002 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
UPDATE course SET status = 'Inactive' WHERE course_id = @course;
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 8, @coachA, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50002 THROW 60000, N'FAIL: inactive course must be 50002', 1;
ROLLBACK;
GO

-- ===== Batch 7: nonexistent course -> 50002 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
BEGIN TRY EXEC dbo.sp_create_class @mgr, -1, N'x', '2026-11-02', 8, @coachA, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50002 THROW 60000, N'FAIL: nonexistent course must be 50002', 1;
ROLLBACK;
GO

-- ===== Batch 8: empty slot list -> 50008 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 8, @coachA, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50008 THROW 60000, N'FAIL: empty slots must be 50008', 1;
ROLLBACK;
GO

-- ===== Batch 9: nonexistent time_slot_id -> 50008 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (-1);
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 8, @coachA, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50008 THROW 60000, N'FAIL: unknown slot id must be 50008', 1;
ROLLBACK;
GO

-- ===== Batch 10: coach of another sport -> 50003 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 8, @coachX, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50003 THROW 60000, N'FAIL: coach of other sport must be 50003', 1;
ROLLBACK;
GO

-- ===== Batch 11: coach_id is not a Coach (Member) -> 50003 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 8, @member, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50003 THROW 60000, N'FAIL: non-coach must be 50003', 1;
ROLLBACK;
GO

-- ===== Batch 12: room of another sport -> 50004 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 8, @coachA, @roomX, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50004 THROW 60000, N'FAIL: room of other sport must be 50004', 1;
ROLLBACK;
GO

-- ===== Batch 13: inactive room -> 50004 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
UPDATE room SET status = 'Inactive' WHERE room_id = @roomS;
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 8, @coachA, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50004 THROW 60000, N'FAIL: inactive room must be 50004', 1;
ROLLBACK;
GO

-- ===== Batch 14: max_capacity 11 > roomS capacity 10 -> 50005 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 11, @coachA, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50005 THROW 60000, N'FAIL: capacity over room must be 50005', 1;
ROLLBACK;
GO

-- ===== Order checks: earlier code wins when two checks fail =====
-- 50002 before 50008 (inactive course + empty slots)
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
UPDATE course SET status = 'Inactive' WHERE course_id = @course;
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 8, @coachA, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50002 THROW 60000, N'FAIL: order 50002 before 50008', 1;
ROLLBACK;
GO

-- 50008 before 50003 (empty slots + coach of other sport)
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @cid INT, @err INT = 0;
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 8, @coachX, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50008 THROW 60000, N'FAIL: order 50008 before 50003', 1;
ROLLBACK;
GO

-- 50005 before 50006 (coach busy + over capacity)
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @c1 INT, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't4 base', '2026-11-02', 8, @coachA, @roomL, @slots, @c1 OUTPUT;
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 11, @coachA, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50005 THROW 60000, N'FAIL: order 50005 before 50006', 1;
ROLLBACK;
GO

-- 50006 before 50007 (coach and room both busy)
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @c1 INT, @cid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't4 base', '2026-11-02', 8, @coachA, @roomS, @slots, @c1 OUTPUT;
BEGIN TRY EXEC dbo.sp_create_class @mgr, @course, N'x', '2026-11-02', 8, @coachA, @roomS, @slots, @cid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50006 THROW 60000, N'FAIL: order 50006 before 50007', 1;
ROLLBACK;
GO

PRINT 'PASS t04 create_class';
GO
