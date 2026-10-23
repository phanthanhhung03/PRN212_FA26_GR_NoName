USE SportCenterDB;
GO

-- Member self-registration. created_by stays NULL.
CREATE PROCEDURE dbo.sp_register_member
    @full_name     NVARCHAR(100),
    @email         VARCHAR(100),
    @password_hash VARCHAR(255),
    @birth_date    DATE,
    @gender        VARCHAR(10),
    @phone         VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @member_role INT = (SELECT role_id FROM [role] WHERE role_name = N'Member');

    INSERT INTO [user] (role_id, full_name, email, password_hash, phone, birth_date, gender, created_by)
    VALUES (@member_role, @full_name, @email, @password_hash, @phone, @birth_date, @gender, NULL);
END;
GO

-- Manager creates a Staff or Coach account. Coach requires a valid sport; Staff must have none.
CREATE PROCEDURE dbo.sp_create_staff_coach
    @actor_id      INT,
    @role_name     NVARCHAR(50),
    @full_name     NVARCHAR(100),
    @email         VARCHAR(100),
    @password_hash VARCHAR(255),
    @phone         VARCHAR(20) = NULL,
    @sport_id      INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM [user] u
        JOIN [role] r ON r.role_id = u.role_id
        WHERE u.user_id = @actor_id AND r.role_name = N'Manager' AND u.status = 'Active')
        THROW 50001, N'Chỉ Manager đang hoạt động mới được tạo tài khoản.', 1;

    IF @role_name IS NULL OR @role_name NOT IN (N'Staff', N'Coach')
        THROW 50012, N'Chỉ được tạo tài khoản Staff hoặc Coach.', 1;

    IF @role_name = N'Coach'
       AND (@sport_id IS NULL OR NOT EXISTS (SELECT 1 FROM sport WHERE sport_id = @sport_id))
        THROW 50012, N'Coach phải có môn thể thao hợp lệ.', 1;

    IF @role_name = N'Staff' AND @sport_id IS NOT NULL
        THROW 50012, N'Staff không được gắn môn thể thao.', 1;

    DECLARE @role_id INT = (SELECT role_id FROM [role] WHERE role_name = @role_name);

    INSERT INTO [user] (role_id, full_name, email, password_hash, phone, sport_id, created_by)
    VALUES (@role_id, @full_name, @email, @password_hash, @phone, @sport_id, @actor_id);
END;
GO
