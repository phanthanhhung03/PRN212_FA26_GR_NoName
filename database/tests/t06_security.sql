USE SportCenterDB;
SET NOCOUNT ON;
-- Each expected-error case runs in its own batch; REVERT in CATCH and before ROLLBACK
-- so a failing batch can never leave the connection impersonating app_user.

-- ===== Batches 1-6: direct writes are denied (229) =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @err INT = 0;
BEGIN TRY EXECUTE AS USER = 'app_user'; INSERT INTO session DEFAULT VALUES; REVERT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); REVERT; END CATCH
REVERT;
IF @err <> 229 THROW 60000, N'FAIL: INSERT session must be 229', 1;
ROLLBACK;
GO

BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @err INT = 0;
BEGIN TRY EXECUTE AS USER = 'app_user'; UPDATE class SET class_name = N'x'; REVERT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); REVERT; END CATCH
REVERT;
IF @err <> 229 THROW 60000, N'FAIL: UPDATE class must be 229', 1;
ROLLBACK;
GO

BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @err INT = 0;
BEGIN TRY EXECUTE AS USER = 'app_user'; DELETE FROM session; REVERT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); REVERT; END CATCH
REVERT;
IF @err <> 229 THROW 60000, N'FAIL: DELETE session must be 229', 1;
ROLLBACK;
GO

BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @err INT = 0;
BEGIN TRY EXECUTE AS USER = 'app_user'; UPDATE [user] SET full_name = N'x'; REVERT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); REVERT; END CATCH
REVERT;
IF @err <> 229 THROW 60000, N'FAIL: UPDATE [user] must be 229', 1;
ROLLBACK;
GO

BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @err INT = 0;
BEGIN TRY EXECUTE AS USER = 'app_user'; DELETE FROM payment; REVERT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); REVERT; END CATCH
REVERT;
IF @err <> 229 THROW 60000, N'FAIL: DELETE payment must be 229', 1;
ROLLBACK;
GO

BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @err INT = 0;
BEGIN TRY EXECUTE AS USER = 'app_user'; DELETE FROM class_time_slot; REVERT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); REVERT; END CATCH
REVERT;
IF @err <> 229 THROW 60000, N'FAIL: DELETE class_time_slot must be 229', 1;
ROLLBACK;
GO

-- ===== Batch 7: internal helper is not executable (229) =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @err INT = 0;
BEGIN TRY EXECUTE AS USER = 'app_user'; EXEC dbo.usp_check_assignment; REVERT; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); REVERT; END CATCH
REVERT;
IF @err <> 229 THROW 60000, N'FAIL: EXEC usp_check_assignment must be 229', 1;
ROLLBACK;
GO

-- ===== Batch 8: allowed operations succeed as app_user =====
BEGIN TRAN;
:r tests/_fixture.sql
DECLARE @slots dbo.IdList, @classA INT, @n INT, @who SYSNAME, @sA2 INT, @mk INT;
INSERT INTO @slots (id) VALUES (@slotMon), (@slotWed);
DECLARE @free_slots dbo.IdList;
INSERT INTO @free_slots (id) VALUES (@slotMon), (@slotWed);

BEGIN TRY
    EXECUTE AS USER = 'app_user';
    SET @who = USER_NAME();

    SELECT @n = COUNT(*) FROM session;                                   -- SELECT ok

    EXEC dbo.sp_register_member N't6 member', 't6_member@test.local', 'x', '2000-01-01', 'Male';
    EXEC dbo.sp_create_class @mgr, @course, N't6 A', '2026-11-02', 8, @coachA, @roomS, @slots, @classA OUTPUT;

    SELECT @n = COUNT(*) FROM dbo.fn_free_rooms(@sport, 5, '2026-11-02', @free_slots, 4);

    SELECT @sA2 = session_id FROM session WHERE class_id = @classA AND session_no = 2;
    EXEC dbo.sp_reassign_session @mgr, @sA2, @coachB, @roomL;
    EXEC dbo.sp_cancel_session @mgr, @sA2, @mk OUTPUT;
    REVERT;
END TRY
BEGIN CATCH
    REVERT;
    DECLARE @msg NVARCHAR(400) = CONCAT(N'FAIL: app_user allowed op raised ', ERROR_NUMBER(), N': ', ERROR_MESSAGE());
    THROW 60000, @msg, 1;
END CATCH
REVERT;
IF @who IS NULL OR @who <> N'app_user' THROW 60000, N'FAIL: not impersonating app_user', 1;
IF NOT EXISTS (SELECT 1 FROM [user] WHERE email = 't6_member@test.local') THROW 60000, N'FAIL: member not registered', 1;
IF @classA IS NULL OR NOT EXISTS (SELECT 1 FROM class WHERE class_id = @classA) THROW 60000, N'FAIL: class not created', 1;
IF ISNULL(@n, -1) < 1 THROW 60000, N'FAIL: fn_free_rooms must return rows for app_user', 1;
IF NOT EXISTS (SELECT 1 FROM session WHERE session_id = @sA2 AND status = 'Cancelled') THROW 60000, N'FAIL: session not cancelled', 1;
ROLLBACK;
PRINT 'PASS t06 security';
GO
