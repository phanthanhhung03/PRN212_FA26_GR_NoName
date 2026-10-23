USE SportCenterDB;
SET NOCOUNT ON;
BEGIN TRAN;
:r tests/_fixture.sql

DECLARE @err INT, @uid INT, @bd DATE, @g VARCHAR(10), @rn NVARCHAR(50), @sp INT, @cb INT, @n INT;

-- Register member
EXEC dbo.sp_register_member N'T3 Member', 't3_member@test.local', 'h', '2000-05-17', 'Female', '0900000001';
SELECT @uid = u.user_id, @bd = u.birth_date, @g = u.gender, @rn = r.role_name, @sp = u.sport_id, @cb = u.created_by
FROM [user] u JOIN [role] r ON r.role_id = u.role_id WHERE u.email = 't3_member@test.local';
IF @uid IS NULL THROW 60000, N'FAIL: member not created', 1;
IF @bd IS NULL OR @bd <> '2000-05-17' THROW 60000, N'FAIL: birth_date', 1;
IF @g IS NULL OR @g <> 'Female' THROW 60000, N'FAIL: gender', 1;
IF @rn IS NULL OR @rn <> N'Member' THROW 60000, N'FAIL: role Member', 1;
IF @sp IS NOT NULL THROW 60000, N'FAIL: member sport_id must be NULL', 1;
IF @cb IS NOT NULL THROW 60000, N'FAIL: member created_by must be NULL', 1;

-- Duplicate email
SET @err = 0;
BEGIN TRY EXEC dbo.sp_register_member N'Dup', 't3_member@test.local', 'h', '2000-01-01', 'Male'; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 2627 THROW 60000, N'FAIL: duplicate email must raise 2627', 1;

-- Coach with sport
EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'T3 Coach', 't3_coach@test.local', 'h', NULL, @sport;
SELECT @sp = u.sport_id, @rn = r.role_name, @cb = u.created_by
FROM [user] u JOIN [role] r ON r.role_id = u.role_id WHERE u.email = 't3_coach@test.local';
IF @rn IS NULL OR @rn <> N'Coach' THROW 60000, N'FAIL: coach role', 1;
IF @sp IS NULL OR @sp <> @sport THROW 60000, N'FAIL: coach sport_id', 1;
IF @cb IS NULL OR @cb <> @mgr THROW 60000, N'FAIL: coach created_by', 1;

-- Staff without sport
EXEC dbo.sp_create_staff_coach @mgr, N'Staff', N'T3 Staff', 't3_staff@test.local', 'h';
SELECT @sp = u.sport_id, @rn = r.role_name
FROM [user] u JOIN [role] r ON r.role_id = u.role_id WHERE u.email = 't3_staff@test.local';
IF @rn IS NULL OR @rn <> N'Staff' THROW 60000, N'FAIL: staff role', 1;
IF @sp IS NOT NULL THROW 60000, N'FAIL: staff sport_id must be NULL', 1;

-- Coach without sport -> 50012
SET @err = 0;
BEGIN TRY EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'X', 't3_x1@test.local', 'h'; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50012 THROW 60000, N'FAIL: coach without sport must be 50012', 1;

-- Coach with nonexistent sport -> 50012
SET @err = 0;
BEGIN TRY EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'X', 't3_x2@test.local', 'h', NULL, -1; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50012 THROW 60000, N'FAIL: coach with invalid sport must be 50012', 1;

-- Staff with sport -> 50012
SET @err = 0;
BEGIN TRY EXEC dbo.sp_create_staff_coach @mgr, N'Staff', N'X', 't3_x3@test.local', 'h', NULL, @sport; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50012 THROW 60000, N'FAIL: staff with sport must be 50012', 1;

-- Actor is Member -> 50001
SET @err = 0;
BEGIN TRY EXEC dbo.sp_create_staff_coach @member, N'Staff', N'X', 't3_x4@test.local', 'h'; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50001 THROW 60000, N'FAIL: member actor must be 50001', 1;

-- Inactive manager -> 50001
UPDATE [user] SET status = 'Inactive' WHERE user_id = @mgr;
SET @err = 0;
BEGIN TRY EXEC dbo.sp_create_staff_coach @mgr, N'Staff', N'X', 't3_x5@test.local', 'h'; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50001 THROW 60000, N'FAIL: inactive manager must be 50001', 1;
UPDATE [user] SET status = 'Active' WHERE user_id = @mgr;

-- Role Manager -> 50012
SET @err = 0;
BEGIN TRY EXEC dbo.sp_create_staff_coach @mgr, N'Manager', N'X', 't3_x6@test.local', 'h'; END TRY
BEGIN CATCH SET @err = ERROR_NUMBER(); END CATCH
IF @err <> 50012 THROW 60000, N'FAIL: role Manager must be 50012', 1;

-- Nothing leaked from failed calls
SELECT @n = COUNT(*) FROM [user] WHERE email LIKE 't3[_]x%';
IF @n <> 0 THROW 60000, N'FAIL: rejected calls must not insert', 1;

ROLLBACK;
PRINT 'PASS t03 accounts';
GO
