USE SportCenterDB;
SET NOCOUNT ON;
-- Session procedures use XACT_ABORT ON, so an expected error dooms the test transaction.
-- Each expected-error case runs in its own batch (own BEGIN TRAN / fixture / ROLLBACK).
-- Class A: coachA, roomS, slots Mon+Wed, start 2026-11-02, 4 sessions:
--   1 = 11-02 Mon, 2 = 11-04 Wed, 3 = 11-09 Mon, 4 = 11-11 Wed.

-- ===== Batch 1: reassign happy path =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @sA3 INT, @n INT, @c INT, @r INT;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
SELECT @sA3 = session_id FROM session WHERE class_id = @classA AND session_no = 3;

EXEC dbo.sp_reassign_session @mgr, @sA2, @coachB, @roomL;
SELECT @c = coach_id, @r = room_id FROM session WHERE session_id = @sA2 AND status = 'Scheduled';
IF @c IS NULL OR @c <> @coachB OR @r IS NULL OR @r <> @roomL
    THROW 60000, N'FAIL: session 2 must move to coachB/roomL', 1;
SET @c = NULL;
SELECT @c = coach_id FROM class WHERE class_id = @classA AND room_id = @roomS;
IF @c IS NULL OR @c <> @coachA THROW 60000, N'FAIL: class coach/room must stay coachA/roomS', 1;
SELECT @n = COUNT(*) FROM session
WHERE class_id = @classA AND session_id <> @sA2 AND coach_id = @coachA AND room_id = @roomS AND status = 'Scheduled';
IF ISNULL(@n, 0) <> 3 THROW 60000, N'FAIL: other sessions must be unchanged', 1;

-- NULL parameter keeps the current value
EXEC dbo.sp_reassign_session @mgr, @sA3, NULL, @roomL;
SET @c = NULL; SET @r = NULL;
SELECT @c = coach_id, @r = room_id FROM session WHERE session_id = @sA3;
IF @c IS NULL OR @c <> @coachA OR @r IS NULL OR @r <> @roomL
    THROW 60000, N'FAIL: NULL coach must keep coachA, room must be roomL', 1;
ROLLBACK;
GO

-- ===== Batch 2: reassign coach of other sport -> 50003 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
BEGIN TRY EXEC dbo.sp_reassign_session @mgr, @sA2, @coachX, NULL; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50003 THROW 60000, N'FAIL: reassign to coachX must be 50003', 1;
ROLLBACK;
GO

-- ===== Batch 3: reassign room of other sport -> 50004 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
BEGIN TRY EXEC dbo.sp_reassign_session @mgr, @sA2, NULL, @roomX; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50004 THROW 60000, N'FAIL: reassign to roomX must be 50004', 1;
ROLLBACK;
GO

-- ===== Batch 4: reassign by Member -> 50001 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
BEGIN TRY EXEC dbo.sp_reassign_session @member, @sA2, @coachB, @roomL; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50001 THROW 60000, N'FAIL: member reassign must be 50001', 1;
ROLLBACK;
GO

-- ===== Batch 5: reassign to busy coach -> 50006 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @classB INT, @sA2 INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
-- coachB teaches 11-04 Wed (same day/slot as session 2 of A) in roomL
EXEC dbo.sp_create_class @mgr, @course, N't5 B', '2026-11-04', 8, @coachB, @roomL, @slots, @classB OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
BEGIN TRY EXEC dbo.sp_reassign_session @mgr, @sA2, @coachB, NULL; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50006 THROW 60000, N'FAIL: busy coach reassign must be 50006', 1;
ROLLBACK;
GO

-- ===== Batch 6: reassign to busy room -> 50007 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @classB INT, @sA2 INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
EXEC dbo.sp_create_class @mgr, @course, N't5 B', '2026-11-04', 8, @coachB, @roomL, @slots, @classB OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
BEGIN TRY EXEC dbo.sp_reassign_session @mgr, @sA2, NULL, @roomL; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50007 THROW 60000, N'FAIL: busy room reassign must be 50007', 1;
ROLLBACK;
GO

-- ===== Batch 7: reassign to room smaller than max_capacity -> 50005 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @roomT INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
INSERT INTO room (sport_id, room_name, capacity) VALUES (@sport, N'fx_roomT', 5);
SET @roomT = SCOPE_IDENTITY();
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
BEGIN TRY EXEC dbo.sp_reassign_session @mgr, @sA2, NULL, @roomT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50005 THROW 60000, N'FAIL: small room reassign must be 50005', 1;
ROLLBACK;
GO

-- ===== Batch 8: reassign a Cancelled session -> 50009 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
UPDATE session SET status = 'Cancelled' WHERE session_id = @sA2;
BEGIN TRY EXEC dbo.sp_reassign_session @mgr, @sA2, @coachB, NULL; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50009 THROW 60000, N'FAIL: reassign of cancelled session must be 50009', 1;
ROLLBACK;
GO

-- ===== Batch 9: cancel with free makeup slot =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @makeup INT, @n INT, @d DATE, @st VARCHAR(20);
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
EXEC dbo.sp_cancel_session @mgr, @sA2, @makeup OUTPUT;
IF @makeup IS NULL THROW 60000, N'FAIL: makeup must be created when free', 1;
SELECT @st = status FROM session WHERE session_id = @sA2;
IF ISNULL(@st, '') <> 'Cancelled' THROW 60000, N'FAIL: session 2 must be Cancelled', 1;
SELECT @n = COUNT(*) FROM session
WHERE session_id = @makeup AND class_id = @classA AND session_no = 5 AND session_date = '2026-11-16'
  AND time_slot_id = @slotMon AND coach_id = @coachA AND room_id = @roomS AND status = 'Scheduled';
IF ISNULL(@n, 0) <> 1 THROW 60000, N'FAIL: makeup must be no 5, 2026-11-16, slotMon, coachA, roomS', 1;
SELECT @n = COUNT(*) FROM session WHERE class_id = @classA AND status <> 'Cancelled';
IF ISNULL(@n, 0) <> 4 THROW 60000, N'FAIL: class A must have 4 valid sessions', 1;
SELECT @d = end_date FROM class WHERE class_id = @classA;
IF @d IS NULL OR @d <> '2026-11-16' THROW 60000, N'FAIL: end_date must be 2026-11-16', 1;
ROLLBACK;
GO

-- ===== Batch 10: cancel the same session twice -> 50009 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @makeup INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
EXEC dbo.sp_cancel_session @mgr, @sA2, @makeup OUTPUT;
BEGIN TRY EXEC dbo.sp_cancel_session @mgr, @sA2, @makeup OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50009 THROW 60000, N'FAIL: second cancel must be 50009', 1;
ROLLBACK;
GO

-- ===== Batch 11: cancel a Completed session -> 50009 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA1 INT, @makeup INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA1 = session_id FROM session WHERE class_id = @classA AND session_no = 1;
UPDATE session SET status = 'Completed' WHERE session_id = @sA1;
BEGIN TRY EXEC dbo.sp_cancel_session @mgr, @sA1, @makeup OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50009 THROW 60000, N'FAIL: cancel of Completed session must be 50009', 1;
ROLLBACK;
GO

-- ===== Batch 12: cancel by Member -> 50001 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @makeup INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
BEGIN TRY EXEC dbo.sp_cancel_session @member, @sA2, @makeup OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50001 THROW 60000, N'FAIL: member cancel must be 50001', 1;
ROLLBACK;
GO

-- ===== Batch 13: cancel when makeup slot is busy, then manual makeup =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @slotsMon dbo.IdList, @classA INT, @classB INT, @sA2 INT, @makeup INT, @sid INT;
DECLARE @n INT, @d DATE, @st VARCHAR(20);
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
INSERT INTO @slotsMon (id) VALUES (@slotMon);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
-- class B takes roomS on Mon 2026-11-16 (the auto makeup date of A)
EXEC dbo.sp_create_class @mgr, @course, N't5 B', '2026-11-16', 8, @coachB, @roomS, @slotsMon, @classB OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
SET @makeup = -1;
EXEC dbo.sp_cancel_session @mgr, @sA2, @makeup OUTPUT;
IF @makeup IS NOT NULL THROW 60000, N'FAIL: makeup must be NULL when busy', 1;
SELECT @st = status FROM session WHERE session_id = @sA2;
IF ISNULL(@st, '') <> 'Cancelled' THROW 60000, N'FAIL: session 2 must still be Cancelled', 1;
SELECT @n = COUNT(*) FROM session WHERE class_id = @classA AND status <> 'Cancelled';
IF ISNULL(@n, 0) <> 3 THROW 60000, N'FAIL: class A must have 3 valid sessions', 1;
SELECT @n = COUNT(*) FROM session WHERE class_id = @classA;
IF ISNULL(@n, 0) <> 4 THROW 60000, N'FAIL: no session row may be added when busy', 1;
SELECT @d = end_date FROM class WHERE class_id = @classA;
IF @d IS NULL OR @d <> '2026-11-11' THROW 60000, N'FAIL: end_date must stay 2026-11-11', 1;

EXEC dbo.sp_add_makeup_session @mgr, @classA, '2026-11-16', @slotMon, @coachA, @roomL, @sid OUTPUT;
IF @sid IS NULL THROW 60000, N'FAIL: manual makeup id is NULL', 1;
SELECT @n = COUNT(*) FROM session
WHERE session_id = @sid AND class_id = @classA AND session_no = 5 AND session_date = '2026-11-16'
  AND time_slot_id = @slotMon AND coach_id = @coachA AND room_id = @roomL AND status = 'Scheduled';
IF ISNULL(@n, 0) <> 1 THROW 60000, N'FAIL: manual makeup must be no 5, 11-16, slotMon, coachA, roomL', 1;
SELECT @n = COUNT(*) FROM session WHERE class_id = @classA AND status <> 'Cancelled';
IF ISNULL(@n, 0) <> 4 THROW 60000, N'FAIL: class A must have 4 valid sessions after makeup', 1;
SET @d = NULL;
SELECT @d = end_date FROM class WHERE class_id = @classA;
IF @d IS NULL OR @d <> '2026-11-16' THROW 60000, N'FAIL: end_date must be 2026-11-16 after makeup', 1;
ROLLBACK;
GO

-- ===== Batch 14: manual makeup when class already full -> 50011 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @slotsMon dbo.IdList, @classA INT, @classB INT, @sA2 INT, @makeup INT, @sid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
INSERT INTO @slotsMon (id) VALUES (@slotMon);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
EXEC dbo.sp_create_class @mgr, @course, N't5 B', '2026-11-16', 8, @coachB, @roomS, @slotsMon, @classB OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
EXEC dbo.sp_cancel_session @mgr, @sA2, @makeup OUTPUT;
EXEC dbo.sp_add_makeup_session @mgr, @classA, '2026-11-16', @slotMon, @coachA, @roomL, @sid OUTPUT;
-- second call on a free date/slot: only the "class full" check can fail
BEGIN TRY EXEC dbo.sp_add_makeup_session @mgr, @classA, '2026-11-23', @slotMonPm, @coachA, @roomL, @sid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50011 THROW 60000, N'FAIL: makeup on full class must be 50011', 1;
ROLLBACK;
GO

-- ===== Batch 15: manual makeup on wrong weekday -> 50010 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @slotsMon dbo.IdList, @classA INT, @classB INT, @sA2 INT, @makeup INT, @sid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
INSERT INTO @slotsMon (id) VALUES (@slotMon);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
EXEC dbo.sp_create_class @mgr, @course, N't5 B', '2026-11-16', 8, @coachB, @roomS, @slotsMon, @classB OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
EXEC dbo.sp_cancel_session @mgr, @sA2, @makeup OUTPUT;
-- 2026-11-17 is Tuesday, slotMon is Monday
BEGIN TRY EXEC dbo.sp_add_makeup_session @mgr, @classA, '2026-11-17', @slotMon, @coachA, @roomL, @sid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50010 THROW 60000, N'FAIL: wrong weekday makeup must be 50010', 1;
ROLLBACK;
GO

-- ===== Batch 16: manual makeup with busy coach -> 50006 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @slotsMon dbo.IdList, @classA INT, @classB INT, @sA2 INT, @makeup INT, @sid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
INSERT INTO @slotsMon (id) VALUES (@slotMon);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
EXEC dbo.sp_create_class @mgr, @course, N't5 B', '2026-11-16', 8, @coachB, @roomS, @slotsMon, @classB OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
EXEC dbo.sp_cancel_session @mgr, @sA2, @makeup OUTPUT;
-- coachB teaches class B on 2026-11-16 slotMon; roomL is free
BEGIN TRY EXEC dbo.sp_add_makeup_session @mgr, @classA, '2026-11-16', @slotMon, @coachB, @roomL, @sid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50006 THROW 60000, N'FAIL: makeup with busy coach must be 50006', 1;
ROLLBACK;
GO

-- ===== Batch 17: manual makeup by Member -> 50001 =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @slotsMon dbo.IdList, @classA INT, @classB INT, @sA2 INT, @makeup INT, @sid INT, @err INT = 0;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
INSERT INTO @slotsMon (id) VALUES (@slotMon);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
EXEC dbo.sp_create_class @mgr, @course, N't5 B', '2026-11-16', 8, @coachB, @roomS, @slotsMon, @classB OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
EXEC dbo.sp_cancel_session @mgr, @sA2, @makeup OUTPUT;
BEGIN TRY EXEC dbo.sp_add_makeup_session @member, @classA, '2026-11-16', @slotMon, @coachA, @roomL, @sid OUTPUT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50001 THROW 60000, N'FAIL: member makeup must be 50001', 1;
ROLLBACK;
GO

-- ===== Batch 18: a Cancelled session releases its coach and room =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @classB INT, @sA2 INT, @sB1 INT, @makeup INT, @n INT;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
-- class B (coachB, roomL): session 1 = 2026-11-04 Wed slotWed, same date/slot as A's session 2
EXEC dbo.sp_create_class @mgr, @course, N't5 B', '2026-11-04', 8, @coachB, @roomL, @slots, @classB OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
SELECT @sB1 = session_id FROM session
WHERE class_id = @classB AND session_no = 1 AND session_date = '2026-11-04' AND time_slot_id = @slotWed;
IF @sA2 IS NULL OR @sB1 IS NULL THROW 60000, N'FAIL: setup sessions not found', 1;
EXEC dbo.sp_cancel_session @mgr, @sA2, @makeup OUTPUT;
-- coachA and roomS are now free on 2026-11-04 slotWed: moving B's session there must succeed
EXEC dbo.sp_reassign_session @mgr, @sB1, @coachA, @roomS;
SELECT @n = COUNT(*) FROM session
WHERE session_id = @sB1 AND class_id = @classB AND coach_id = @coachA AND room_id = @roomS
  AND session_date = '2026-11-04' AND time_slot_id = @slotWed AND status = 'Scheduled';
IF ISNULL(@n, 0) <> 1 THROW 60000, N'FAIL: cancelled session must release coachA/roomS on 2026-11-04', 1;
ROLLBACK;
GO

-- ===== Batch 19: reassign to the current coach/room is not a conflict with itself =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @n INT;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
EXEC dbo.sp_reassign_session @mgr, @sA2, @coachA, @roomS;
SELECT @n = COUNT(*) FROM session
WHERE session_id = @sA2 AND class_id = @classA AND session_no = 2 AND coach_id = @coachA AND room_id = @roomS
  AND session_date = '2026-11-04' AND time_slot_id = @slotWed AND status = 'Scheduled';
IF ISNULL(@n, 0) <> 1 THROW 60000, N'FAIL: self-reassign must succeed and leave the row unchanged', 1;
ROLLBACK;
GO

-- ===== Batch 20: class room Inactive -> no automatic makeup, cancel still stands =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @makeup INT, @n INT, @st VARCHAR(20);
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
UPDATE room SET status = 'Inactive' WHERE room_id = @roomS;
SET @makeup = -1;
EXEC dbo.sp_cancel_session @mgr, @sA2, @makeup OUTPUT;
IF @makeup IS NOT NULL THROW 60000, N'FAIL: inactive class room must give no automatic makeup', 1;
SELECT @st = status FROM session WHERE session_id = @sA2;
IF ISNULL(@st, '') <> 'Cancelled' THROW 60000, N'FAIL: session 2 must be Cancelled', 1;
SELECT @n = COUNT(*) FROM session WHERE class_id = @classA;
IF ISNULL(@n, 0) <> 4 THROW 60000, N'FAIL: no makeup row may be created for inactive room', 1;
ROLLBACK;
GO

-- ===== Batch 21: class coach Inactive -> no automatic makeup, cancel still stands =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @sA2 INT, @makeup INT, @n INT, @st VARCHAR(20);
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
EXEC dbo.sp_create_class @mgr, @course, N't5 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;
SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
UPDATE [user] SET status = 'Inactive' WHERE user_id = @coachA;
SET @makeup = -1;
EXEC dbo.sp_cancel_session @mgr, @sA2, @makeup OUTPUT;
IF @makeup IS NOT NULL THROW 60000, N'FAIL: inactive class coach must give no automatic makeup', 1;
SELECT @st = status FROM session WHERE session_id = @sA2;
IF ISNULL(@st, '') <> 'Cancelled' THROW 60000, N'FAIL: session 2 must be Cancelled (coach case)', 1;
SELECT @n = COUNT(*) FROM session WHERE class_id = @classA;
IF ISNULL(@n, 0) <> 4 THROW 60000, N'FAIL: no makeup row may be created for inactive coach', 1;
ROLLBACK;
GO

PRINT 'PASS t05 session';
GO
