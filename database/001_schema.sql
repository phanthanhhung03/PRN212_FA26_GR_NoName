CREATE DATABASE SportCenterDB;
GO
USE SportCenterDB;
GO

-- =====================================================
-- 1. ROLE
-- =====================================================
CREATE TABLE [role] (
    role_id   INT PRIMARY KEY IDENTITY(1,1),
    role_name NVARCHAR(50) NOT NULL UNIQUE          -- 'Manager', 'Staff', 'Coach', 'Member'
);
GO

INSERT INTO [role] (role_name)
VALUES (N'Manager'), (N'Staff'), (N'Coach'), (N'Member');
GO

-- =====================================================
-- 2. USER
-- =====================================================
CREATE TABLE [user] (
    user_id       INT PRIMARY KEY IDENTITY(1,1),
    role_id       INT NOT NULL,
    full_name     NVARCHAR(100) NOT NULL,
    email         VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    phone         VARCHAR(20),
    status        VARCHAR(20) NOT NULL DEFAULT 'Active'
                  CHECK (status IN ('Active', 'Inactive', 'Banned')),
    created_by    INT NULL,                         -- NULL nếu Member tự đăng ký
    created_at    DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_User_Role      FOREIGN KEY (role_id)    REFERENCES [role](role_id),
    CONSTRAINT FK_User_CreatedBy FOREIGN KEY (created_by) REFERENCES [user](user_id)
);
GO

-- =====================================================
-- 3. SPORT
-- =====================================================
CREATE TABLE sport (
    sport_id    INT PRIMARY KEY IDENTITY(1,1),
    sport_name  NVARCHAR(100) NOT NULL,
    description NVARCHAR(MAX),
    status      VARCHAR(20) NOT NULL DEFAULT 'Active'
                CHECK (status IN ('Active', 'Inactive'))
);
GO

-- =====================================================
-- 4. COURSE
-- =====================================================
CREATE TABLE course (
    course_id      INT PRIMARY KEY IDENTITY(1,1),
    sport_id       INT NOT NULL,
    course_name    NVARCHAR(150) NOT NULL,
    level          NVARCHAR(50),                    -- 'Beginner', 'Intermediate', 'Advanced'
    total_sessions INT NOT NULL CHECK (total_sessions > 0),
    tuition_fee    DECIMAL(18,2) NOT NULL CHECK (tuition_fee >= 0),
    description    NVARCHAR(MAX),
    status         VARCHAR(20) NOT NULL DEFAULT 'Active'
                   CHECK (status IN ('Active', 'Inactive')),
    CONSTRAINT FK_Course_Sport FOREIGN KEY (sport_id) REFERENCES sport(sport_id)
);
GO

-- =====================================================
-- 5. CLASS (quan hệ 'teach': coach_id)
-- =====================================================
CREATE TABLE class (
    class_id     INT PRIMARY KEY IDENTITY(1,1),
    course_id    INT NOT NULL,
    coach_id     INT NULL,                          -- 1 Coach dạy nhiều lớp
    class_name   NVARCHAR(100) NOT NULL,
    start_date   DATE NOT NULL,
    end_date     DATE NULL,
    max_capacity INT NOT NULL DEFAULT 20 CHECK (max_capacity > 0),
    status       VARCHAR(20) NOT NULL DEFAULT 'Enrolling'
                 CHECK (status IN ('Enrolling', 'Ongoing', 'Completed', 'Cancelled')),
    CONSTRAINT FK_Class_Course FOREIGN KEY (course_id) REFERENCES course(course_id),
    CONSTRAINT FK_Class_Coach  FOREIGN KEY (coach_id)  REFERENCES [user](user_id),
    CONSTRAINT CK_Class_Dates  CHECK (end_date IS NULL OR end_date >= start_date)
);
GO

-- =====================================================
-- 6. CLASS_MEMBERS (quan hệ 'join')
-- =====================================================
CREATE TABLE class_members (
    enrollment_id INT PRIMARY KEY IDENTITY(1,1),
    class_id      INT NOT NULL,
    member_id     INT NOT NULL,
    enrolled_at   DATETIME NOT NULL DEFAULT GETDATE(),
    registered_by INT NULL,                         -- NULL nếu tự đăng ký, ID Staff nếu đăng ký hộ
    status        VARCHAR(20) NOT NULL DEFAULT 'Pending'
                  CHECK (status IN ('Pending', 'Active', 'Dropped')),  -- Pending: chờ thanh toán
    CONSTRAINT FK_Enrollment_Class     FOREIGN KEY (class_id)      REFERENCES class(class_id),
    CONSTRAINT FK_Enrollment_Member    FOREIGN KEY (member_id)     REFERENCES [user](user_id),
    CONSTRAINT FK_Enrollment_Registrar FOREIGN KEY (registered_by) REFERENCES [user](user_id),
    CONSTRAINT UQ_Class_Member UNIQUE (class_id, member_id)
);
GO

-- =====================================================
-- 7. PAYMENT (thanh toán qua payOS)
-- =====================================================
CREATE TABLE payment (
    payment_id      INT PRIMARY KEY IDENTITY(1,1),
    enrollment_id   INT NOT NULL,                   -- thanh toán cho lượt ghi danh nào
    order_code      BIGINT NOT NULL UNIQUE,         -- mã đơn gửi payOS, phải duy nhất
    amount          DECIMAL(18,2) NOT NULL CHECK (amount >= 0),  -- lấy từ course.tuition_fee
    description     VARCHAR(25) NOT NULL,           -- payOS giới hạn độ dài mô tả
    payment_link_id VARCHAR(100) NULL,              -- id yêu cầu thanh toán do payOS trả về
    qr_code         NVARCHAR(MAX) NULL,             -- chuỗi QR để render
    checkout_url    NVARCHAR(500) NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'Pending'
                    CHECK (status IN ('Pending', 'Paid', 'Cancelled', 'Expired')),
    paid_at         DATETIME NULL,
    created_by      INT NULL,                       -- Staff tạo hộ, NULL nếu Member tự tạo
    created_at      DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Payment_Enrollment FOREIGN KEY (enrollment_id) REFERENCES class_members(enrollment_id),
    CONSTRAINT FK_Payment_CreatedBy  FOREIGN KEY (created_by)    REFERENCES [user](user_id)
);
GO

-- =====================================================
-- 8. SCHEDULE
-- =====================================================
CREATE TABLE schedule (
    schedule_id   INT PRIMARY KEY IDENTITY(1,1),
    class_id      INT NOT NULL,
    session_no    INT NOT NULL CHECK (session_no > 0),
    session_date  DATE NOT NULL,
    start_time    TIME NOT NULL,
    end_time      TIME NOT NULL,
    room_location NVARCHAR(100),
    session_note  NVARCHAR(500),
    status        VARCHAR(20) NOT NULL DEFAULT 'Scheduled'
                  CHECK (status IN ('Scheduled', 'Completed', 'Cancelled')),
    CONSTRAINT FK_Schedule_Class FOREIGN KEY (class_id) REFERENCES class(class_id),
    CONSTRAINT UQ_Class_Session  UNIQUE (class_id, session_no),
    CONSTRAINT CK_Schedule_Time  CHECK (end_time > start_time)
);
GO

-- =====================================================
-- 9. ATTENDANCE
-- =====================================================
CREATE TABLE attendance (
    attendance_id INT PRIMARY KEY IDENTITY(1,1),
    schedule_id   INT NOT NULL,
    member_id     INT NOT NULL,
    status        VARCHAR(20) NOT NULL DEFAULT 'Absent'
                  CHECK (status IN ('Present', 'Absent', 'Late', 'Excused')),
    check_in_time DATETIME NULL,
    remarks       NVARCHAR(255) NULL,
    marked_by     INT NULL,
    updated_at    DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Attendance_Schedule FOREIGN KEY (schedule_id) REFERENCES schedule(schedule_id),
    CONSTRAINT FK_Attendance_Member   FOREIGN KEY (member_id)   REFERENCES [user](user_id),
    CONSTRAINT FK_Attendance_Marker   FOREIGN KEY (marked_by)   REFERENCES [user](user_id),
    CONSTRAINT UQ_Schedule_Member UNIQUE (schedule_id, member_id)
);
GO

-- =====================================================
-- 10. PROCEDURE: Member tự đăng ký tài khoản
-- =====================================================
CREATE PROCEDURE sp_register_member
    @full_name     NVARCHAR(100),
    @email         VARCHAR(100),
    @password_hash VARCHAR(255),
    @phone         VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @member_role INT = (SELECT role_id FROM [role] WHERE role_name = N'Member');

    INSERT INTO [user] (role_id, full_name, email, password_hash, phone, created_by)
    VALUES (@member_role, @full_name, @email, @password_hash, @phone, NULL);
END;
GO

-- =====================================================
-- 11. PROCEDURE: Manager tạo tài khoản Staff hoặc Coach
-- =====================================================
CREATE PROCEDURE sp_create_staff_coach
    @actor_id      INT,            -- ID Manager đang thực hiện
    @role_name     NVARCHAR(50),   -- 'Staff' hoặc 'Coach'
    @full_name     NVARCHAR(100),
    @email         VARCHAR(100),
    @password_hash VARCHAR(255),
    @phone         VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM [user] u
        JOIN [role] r ON r.role_id = u.role_id
        WHERE u.user_id = @actor_id
          AND r.role_name = N'Manager'
          AND u.status = 'Active'
    )
    BEGIN
        RAISERROR(N'Chỉ Manager đang hoạt động mới được tạo tài khoản.', 16, 1);
        RETURN;
    END

    IF @role_name NOT IN (N'Staff', N'Coach')
    BEGIN
        RAISERROR(N'Chỉ được tạo tài khoản Staff hoặc Coach.', 16, 1);
        RETURN;
    END

    DECLARE @role_id INT = (SELECT role_id FROM [role] WHERE role_name = @role_name);

    INSERT INTO [user] (role_id, full_name, email, password_hash, phone, created_by)
    VALUES (@role_id, @full_name, @email, @password_hash, @phone, @actor_id);
END;
GO

-- =====================================================
-- 12. PHÂN QUYỀN: ứng dụng chỉ ghi qua procedure
-- =====================================================
CREATE USER app_user WITHOUT LOGIN;   -- thay bằng login thật của ứng dụng khi triển khai
GO

DENY INSERT, UPDATE, DELETE ON [user]   TO app_user;
DENY INSERT, UPDATE, DELETE ON payment  TO app_user;
GRANT EXECUTE ON sp_register_member    TO app_user;
GRANT EXECUTE ON sp_create_staff_coach TO app_user;
GO

-- =====================================================
-- 13. MANAGER ĐẦU TIÊN (chạy một lần khi cài đặt)
-- =====================================================
INSERT INTO [user] (role_id, full_name, email, password_hash, phone, created_by)
VALUES ((SELECT role_id FROM [role] WHERE role_name = N'Manager'),
        N'Quản lý trung tâm',
        'manager@sportcenter.local',
        '<hash_bcrypt_đã_băm>',    -- thay bằng hash thật từ ứng dụng
        NULL,
        NULL);
GO