USE SportCenterDB;
GO

-- 1. ROLE
CREATE TABLE [role] (
    role_id   INT PRIMARY KEY IDENTITY(1,1),
    role_name NVARCHAR(50) NOT NULL UNIQUE          -- 'Manager', 'Staff', 'Coach', 'Member'
);
GO
INSERT INTO [role] (role_name) VALUES (N'Manager'), (N'Staff'), (N'Coach'), (N'Member');
GO

-- 2. SPORT
CREATE TABLE sport (
    sport_id    INT PRIMARY KEY IDENTITY(1,1),
    sport_name  NVARCHAR(100) NOT NULL,
    description NVARCHAR(MAX),
    status      VARCHAR(20) NOT NULL DEFAULT 'Active'
                CHECK (status IN ('Active', 'Inactive'))
);
GO

-- 3. USER (sport_id: chi Coach, duoc ep trong procedure)
CREATE TABLE [user] (
    user_id       INT PRIMARY KEY IDENTITY(1,1),
    role_id       INT NOT NULL,
    full_name     NVARCHAR(100) NOT NULL,
    email         VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    phone         VARCHAR(20),
    birth_date    DATE NULL,
    gender        VARCHAR(10) NULL
                  CHECK (gender IN ('Male', 'Female', 'Other')),
    sport_id      INT NULL,
    status        VARCHAR(20) NOT NULL DEFAULT 'Active'
                  CHECK (status IN ('Active', 'Inactive', 'Banned')),
    created_by    INT NULL,                         -- NULL neu Member tu dang ky
    created_at    DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_User_Role      FOREIGN KEY (role_id)    REFERENCES [role](role_id),
    CONSTRAINT FK_User_Sport     FOREIGN KEY (sport_id)   REFERENCES sport(sport_id),
    CONSTRAINT FK_User_CreatedBy FOREIGN KEY (created_by) REFERENCES [user](user_id)
);
GO

-- 4. COURSE
CREATE TABLE course (
    course_id      INT PRIMARY KEY IDENTITY(1,1),
    sport_id       INT NOT NULL,
    course_name    NVARCHAR(150) NOT NULL,
    level          NVARCHAR(50),
    total_sessions INT NOT NULL CHECK (total_sessions > 0),
    tuition_fee    DECIMAL(18,2) NOT NULL CHECK (tuition_fee >= 0),
    description    NVARCHAR(MAX),
    status         VARCHAR(20) NOT NULL DEFAULT 'Active'
                   CHECK (status IN ('Active', 'Inactive')),
    CONSTRAINT FK_Course_Sport FOREIGN KEY (sport_id) REFERENCES sport(sport_id)
);
GO

-- 5. ROOM (moi phong thuoc dung mot mon)
CREATE TABLE room (
    room_id   INT PRIMARY KEY IDENTITY(1,1),
    sport_id  INT NOT NULL,
    room_name NVARCHAR(100) NOT NULL,
    capacity  INT NOT NULL CHECK (capacity > 0),
    status    VARCHAR(20) NOT NULL DEFAULT 'Active'
              CHECK (status IN ('Active', 'Inactive')),
    CONSTRAINT FK_Room_Sport FOREIGN KEY (sport_id) REFERENCES sport(sport_id),
    CONSTRAINT UQ_Room_Name  UNIQUE (room_name)
);
GO

-- 6. CLASS (coach_id, room_id: coach va phong goc)
CREATE TABLE class (
    class_id     INT PRIMARY KEY IDENTITY(1,1),
    course_id    INT NOT NULL,
    coach_id     INT NULL,
    room_id      INT NOT NULL,
    class_name   NVARCHAR(100) NOT NULL,
    start_date   DATE NOT NULL,
    end_date     DATE NULL,                         -- he thong tinh
    max_capacity INT NOT NULL DEFAULT 20 CHECK (max_capacity > 0),
    status       VARCHAR(20) NOT NULL DEFAULT 'Enrolling'
                 CHECK (status IN ('Enrolling', 'Ongoing', 'Completed', 'Cancelled')),
    CONSTRAINT FK_Class_Course FOREIGN KEY (course_id) REFERENCES course(course_id),
    CONSTRAINT FK_Class_Coach  FOREIGN KEY (coach_id)  REFERENCES [user](user_id),
    CONSTRAINT FK_Class_Room   FOREIGN KEY (room_id)   REFERENCES room(room_id),
    CONSTRAINT CK_Class_Dates  CHECK (end_date IS NULL OR end_date >= start_date)
);
GO

-- 7. CLASS_MEMBERS
CREATE TABLE class_members (
    enrollment_id INT PRIMARY KEY IDENTITY(1,1),
    class_id      INT NOT NULL,
    member_id     INT NOT NULL,
    enrolled_at   DATETIME NOT NULL DEFAULT GETDATE(),
    registered_by INT NULL,
    status        VARCHAR(20) NOT NULL DEFAULT 'Pending'
                  CHECK (status IN ('Pending', 'Active', 'Dropped')),
    CONSTRAINT FK_Enrollment_Class     FOREIGN KEY (class_id)      REFERENCES class(class_id),
    CONSTRAINT FK_Enrollment_Member    FOREIGN KEY (member_id)     REFERENCES [user](user_id),
    CONSTRAINT FK_Enrollment_Registrar FOREIGN KEY (registered_by) REFERENCES [user](user_id),
    CONSTRAINT UQ_Class_Member UNIQUE (class_id, member_id)
);
GO

-- 8. PAYMENT (payOS)
CREATE TABLE payment (
    payment_id      INT PRIMARY KEY IDENTITY(1,1),
    enrollment_id   INT NOT NULL,
    order_code      BIGINT NOT NULL UNIQUE,
    amount          DECIMAL(18,2) NOT NULL CHECK (amount >= 0),
    description     VARCHAR(25) NOT NULL,
    payment_link_id VARCHAR(100) NULL,
    qr_code         NVARCHAR(MAX) NULL,
    checkout_url    NVARCHAR(500) NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'Pending'
                    CHECK (status IN ('Pending', 'Paid', 'Cancelled', 'Expired')),
    paid_at         DATETIME NULL,
    created_by      INT NULL,
    created_at      DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Payment_Enrollment FOREIGN KEY (enrollment_id) REFERENCES class_members(enrollment_id),
    CONSTRAINT FK_Payment_CreatedBy  FOREIGN KEY (created_by)    REFERENCES [user](user_id)
);
GO

-- 9. TIME_SLOT (18 khung co dinh: Thu 2-7 x 3 khung)
CREATE TABLE time_slot (
    time_slot_id INT PRIMARY KEY IDENTITY(1,1),
    slot_name    NVARCHAR(50) NOT NULL,
    day_of_week  TINYINT NOT NULL,                  -- 2=Thu 2 ... 7=Thu 7
    start_time   TIME NOT NULL,
    end_time     TIME NOT NULL,
    CONSTRAINT CK_TimeSlot_Day   CHECK (day_of_week BETWEEN 2 AND 7),
    CONSTRAINT CK_TimeSlot_Hours CHECK (
        (start_time = '07:00' AND end_time = '10:00') OR
        (start_time = '15:00' AND end_time = '18:00') OR
        (start_time = '18:00' AND end_time = '21:00')),
    CONSTRAINT UQ_TimeSlot UNIQUE (day_of_week, start_time, end_time)
);
GO

INSERT INTO time_slot (slot_name, day_of_week, start_time, end_time)
SELECT N'T' + CAST(d.n AS NVARCHAR(1)) + N' ' + LEFT(CONVERT(VARCHAR(8), h.s, 108), 5)
       + N'-' + LEFT(CONVERT(VARCHAR(8), h.e, 108), 5),
       d.n, h.s, h.e
FROM (VALUES (2),(3),(4),(5),(6),(7)) AS d(n)
CROSS JOIN (VALUES (CAST('07:00' AS TIME), CAST('10:00' AS TIME)),
                   (CAST('15:00' AS TIME), CAST('18:00' AS TIME)),
                   (CAST('18:00' AS TIME), CAST('21:00' AS TIME))) AS h(s, e);
GO

-- 10. CLASS_TIME_SLOT
CREATE TABLE class_time_slot (
    class_id     INT NOT NULL,
    time_slot_id INT NOT NULL,
    CONSTRAINT PK_Class_TimeSlot PRIMARY KEY (class_id, time_slot_id),
    CONSTRAINT FK_ClassTS_Class    FOREIGN KEY (class_id)     REFERENCES class(class_id),
    CONSTRAINT FK_ClassTS_TimeSlot FOREIGN KEY (time_slot_id) REFERENCES time_slot(time_slot_id)
);
GO

-- 11. SESSION (buoi hoc cu the; gio lay tu time_slot)
CREATE TABLE session (
    session_id   INT PRIMARY KEY IDENTITY(1,1),
    class_id     INT NOT NULL,
    session_no   INT NOT NULL CHECK (session_no > 0),
    session_date DATE NOT NULL,
    time_slot_id INT NOT NULL,
    coach_id     INT NOT NULL,
    room_id      INT NOT NULL,
    status       VARCHAR(20) NOT NULL DEFAULT 'Scheduled'
                 CHECK (status IN ('Scheduled', 'Completed', 'Cancelled')),
    session_note NVARCHAR(500) NULL,
    CONSTRAINT FK_Session_Class    FOREIGN KEY (class_id)     REFERENCES class(class_id),
    CONSTRAINT FK_Session_TimeSlot FOREIGN KEY (time_slot_id) REFERENCES time_slot(time_slot_id),
    CONSTRAINT FK_Session_Coach    FOREIGN KEY (coach_id)     REFERENCES [user](user_id),
    CONSTRAINT FK_Session_Room     FOREIGN KEY (room_id)      REFERENCES room(room_id),
    CONSTRAINT UQ_Class_Session    UNIQUE (class_id, session_no)
);
GO

-- Buoi da huy khong giu phong/coach
CREATE UNIQUE INDEX UX_Session_Room  ON session (room_id,  session_date, time_slot_id) WHERE status <> 'Cancelled';
CREATE UNIQUE INDEX UX_Session_Coach ON session (coach_id, session_date, time_slot_id) WHERE status <> 'Cancelled';
GO

-- 12. ATTENDANCE
CREATE TABLE attendance (
    attendance_id INT PRIMARY KEY IDENTITY(1,1),
    session_id    INT NOT NULL,
    member_id     INT NOT NULL,
    status        VARCHAR(20) NOT NULL DEFAULT 'Absent'
                  CHECK (status IN ('Present', 'Absent', 'Late', 'Excused')),
    check_in_time DATETIME NULL,
    remarks       NVARCHAR(255) NULL,
    marked_by     INT NULL,
    updated_at    DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Attendance_Session FOREIGN KEY (session_id) REFERENCES session(session_id),
    CONSTRAINT FK_Attendance_Member  FOREIGN KEY (member_id)  REFERENCES [user](user_id),
    CONSTRAINT FK_Attendance_Marker  FOREIGN KEY (marked_by)  REFERENCES [user](user_id),
    CONSTRAINT UQ_Session_Member UNIQUE (session_id, member_id)
);
GO

-- 13. Kieu bang truyen danh sach id vao procedure
CREATE TYPE dbo.IdList AS TABLE (id INT PRIMARY KEY);
GO
