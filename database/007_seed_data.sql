USE SportCenterDB;
GO
SET NOCOUNT ON;

-- =====================================================
-- 007: Seed data (sample data for development and demos)
-- Must run after 001-006, as an administrator (dbo/sa), never as app_user.
--
-- ENCODING: this file contains Vietnamese text and is saved as UTF-8. Always run
-- sqlcmd with the UTF-8 code page or the names are corrupted:
--     sqlcmd -S . -E -I -b -f 65001 -i build.sql
--
-- PASSWORDS: every account shares ONE fixed, fake BCrypt-format string. It is a
-- SAMPLE VALUE, not the hash of a real password, so nobody can log in with it.
-- To log in during development, replace password_hash with a real hash generated
-- by BCrypt.Net-Next in the app.
--
-- No error is caught here on purpose: the procedures use XACT_ABORT, so any
-- failure aborts the batch and sqlcmd -b stops with the error.
-- =====================================================

DECLARE @hash VARCHAR(255) = '$2a$11$SampleFakeHashNotARealPasswordSampleFakeHashNotARealP';
DECLARE @mgr INT, @r_mgr INT = (SELECT role_id FROM [role] WHERE role_name = N'Manager');

-- 1. Manager (inserted directly: no one exists yet to create it)
INSERT INTO [user] (role_id, full_name, email, password_hash, phone)
VALUES (@r_mgr, N'Quản trị viên', 'manager@sportcenter.local', @hash, '0900000001');
SET @mgr = SCOPE_IDENTITY();

-- 2. Sports (5), rooms (2 per sport, different capacities), courses (2 per sport)
INSERT INTO sport (sport_name, description) VALUES
(N'Cầu lông',    N'Bộ môn cầu lông trong nhà'),
(N'Bóng rổ',     N'Bộ môn bóng rổ'),
(N'Bóng đá',     N'Bộ môn bóng đá sân mini'),
(N'Bơi lội',     N'Bộ môn bơi lội, bể bơi trong nhà'),
(N'Bóng chuyền', N'Bộ môn bóng chuyền');

INSERT INTO room (sport_id, room_name, capacity)
SELECT s.sport_id, v.room_name, v.capacity
FROM (VALUES
    (N'Cầu lông',    N'Sân cầu lông A',   8),
    (N'Cầu lông',    N'Sân cầu lông B',  16),
    (N'Bóng rổ',     N'Sân bóng rổ 1',   12),
    (N'Bóng rổ',     N'Sân bóng rổ 2',   24),
    (N'Bóng đá',     N'Sân mini 1',      10),
    (N'Bóng đá',     N'Sân mini 2',      22),
    (N'Bơi lội',     N'Bể bơi nhỏ',       6),
    (N'Bơi lội',     N'Bể bơi lớn',      20),
    (N'Bóng chuyền', N'Sân bóng chuyền 1', 12),
    (N'Bóng chuyền', N'Sân bóng chuyền 2', 18)
) AS v(sport_name, room_name, capacity)
JOIN sport s ON s.sport_name = v.sport_name;

INSERT INTO course (sport_id, course_name, level, total_sessions, tuition_fee, description)
SELECT s.sport_id, v.course_name, v.level, v.total_sessions, v.tuition_fee, v.description
FROM (VALUES
    (N'Cầu lông',    N'Cầu lông Beginner',         N'Beginner',     24, 1500000, N'Nhập môn cầu lông'),
    (N'Cầu lông',    N'Cầu lông Advanced',         N'Advanced',     24, 2200000, N'Nâng cao kỹ thuật'),
    (N'Bóng rổ',     N'Bóng rổ Beginner',          N'Beginner',     24, 1800000, N'Nhập môn bóng rổ'),
    (N'Bóng rổ',     N'Bóng rổ Intermediate',      N'Intermediate', 24, 2000000, N'Trung cấp bóng rổ'),
    (N'Bóng đá',     N'Bóng đá Beginner',          N'Beginner',     20, 1700000, N'Nhập môn bóng đá'),
    (N'Bóng đá',     N'Bóng đá Intermediate',      N'Intermediate', 20, 2100000, N'Trung cấp bóng đá'),
    (N'Bơi lội',     N'Bơi lội Beginner',          N'Beginner',     16, 1200000, N'Học bơi cơ bản'),
    (N'Bơi lội',     N'Bơi lội Intermediate',      N'Intermediate', 16, 1400000, N'Bơi kỹ thuật'),
    (N'Bóng chuyền', N'Bóng chuyền Beginner',      N'Beginner',     20, 1300000, N'Nhập môn bóng chuyền'),
    (N'Bóng chuyền', N'Bóng chuyền Intermediate',  N'Intermediate', 20, 1600000, N'Trung cấp bóng chuyền')
) AS v(sport_name, course_name, level, total_sessions, tuition_fee, description)
JOIN sport s ON s.sport_name = v.sport_name;

-- 3. Staff (2) and Coach (2 per sport), created by the Manager through the procedure
EXEC dbo.sp_create_staff_coach @mgr, N'Staff', N'Lê Thị Thu',      'staff1@sportcenter.local', @hash, '0901000001';
EXEC dbo.sp_create_staff_coach @mgr, N'Staff', N'Phạm Minh Hằng',  'staff2@sportcenter.local', @hash, '0901000002';

DECLARE @cl INT = (SELECT sport_id FROM sport WHERE sport_name = N'Cầu lông');
DECLARE @br INT = (SELECT sport_id FROM sport WHERE sport_name = N'Bóng rổ');
DECLARE @bd INT = (SELECT sport_id FROM sport WHERE sport_name = N'Bóng đá');
DECLARE @bl INT = (SELECT sport_id FROM sport WHERE sport_name = N'Bơi lội');
DECLARE @bc INT = (SELECT sport_id FROM sport WHERE sport_name = N'Bóng chuyền');

EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'Trần Quốc Bảo',   'coach.cl1@sportcenter.local', @hash, '0902000001', @cl;
EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'Nguyễn Hữu Dũng', 'coach.cl2@sportcenter.local', @hash, '0902000002', @cl;
EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'Đặng Anh Tuấn',   'coach.br1@sportcenter.local', @hash, '0902000003', @br;
EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'Võ Thành Long',   'coach.br2@sportcenter.local', @hash, '0902000004', @br;
EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'Hoàng Gia Huy',   'coach.bd1@sportcenter.local', @hash, '0902000005', @bd;
EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'Bùi Đức Mạnh',    'coach.bd2@sportcenter.local', @hash, '0902000006', @bd;
EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'Lý Minh Quân',    'coach.bl1@sportcenter.local', @hash, '0902000007', @bl;
EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'Ngô Thanh Sơn',   'coach.bl2@sportcenter.local', @hash, '0902000008', @bl;
EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'Đỗ Hải Yến',      'coach.bc1@sportcenter.local', @hash, '0902000009', @bc;
EXEC dbo.sp_create_staff_coach @mgr, N'Coach', N'Vũ Thu Trang',    'coach.bc2@sportcenter.local', @hash, '0902000010', @bc;

-- 4. Members (5) self-register, with birth date and gender
EXEC dbo.sp_register_member N'Nguyễn Văn An',  'member1@sportcenter.local', @hash, '2005-03-14', 'Male',   '0903000001';
EXEC dbo.sp_register_member N'Trần Thị Bình',  'member2@sportcenter.local', @hash, '2004-07-21', 'Female', '0903000002';
EXEC dbo.sp_register_member N'Lê Quang Cường', 'member3@sportcenter.local', @hash, '2003-11-02', 'Male',   '0903000003';
EXEC dbo.sp_register_member N'Phan Thị Diệu',  'member4@sportcenter.local', @hash, '2006-01-30', 'Female', '0903000004';
EXEC dbo.sp_register_member N'Hồ Minh Đức',    'member5@sportcenter.local', @hash, '2002-09-09', 'Other',  '0903000005';

-- 5. Classes (10) created through sp_create_class (sessions are generated by it).
-- Slots are written as 'day@start' with day 2=Monday..7=Saturday, comma separated.
-- Every start_date falls on the weekday of the first slot, so session 1 = start_date.
DECLARE @plan TABLE (
    seq INT IDENTITY(1,1), course_name NVARCHAR(150), class_name NVARCHAR(100),
    start_date DATE, max_capacity INT, coach_email VARCHAR(100), room_name NVARCHAR(100), slots VARCHAR(100));
INSERT INTO @plan (course_name, class_name, start_date, max_capacity, coach_email, room_name, slots) VALUES
(N'Cầu lông Beginner',     N'CL-Beginner-K01',     '2026-10-05', 4, 'coach.cl1@sportcenter.local', N'Sân cầu lông A',     '2@18:00,4@18:00'), -- Mon (full, in progress)
(N'Cầu lông Beginner',     N'CL-Beginner-K02',     '2026-11-02', 4, 'coach.cl2@sportcenter.local', N'Sân cầu lông B',     '2@07:00'),
(N'Cầu lông Advanced',     N'CL-Advanced-K01',     '2026-11-03', 4, 'coach.cl1@sportcenter.local', N'Sân cầu lông A',     '3@18:00,5@18:00'),
(N'Bóng rổ Beginner',      N'BR-Beginner-K01',     '2026-11-03', 3, 'coach.br1@sportcenter.local', N'Sân bóng rổ 1',      '3@15:00'),
(N'Bóng rổ Intermediate',  N'BR-Intermediate-K01', '2026-11-06', 5, 'coach.br2@sportcenter.local', N'Sân bóng rổ 2',      '6@18:00'),       -- has a reassigned session
(N'Bóng đá Beginner',      N'BD-Beginner-K01',     '2026-11-02', 5, 'coach.bd1@sportcenter.local', N'Sân mini 1',         '2@15:00'),
(N'Bóng đá Intermediate',  N'BD-Intermediate-K01', '2026-10-10', 5, 'coach.bd2@sportcenter.local', N'Sân mini 2',         '7@15:00'),       -- Sat (in progress)
(N'Bơi lội Beginner',      N'BL-Beginner-K01',     '2026-11-03', 3, 'coach.bl1@sportcenter.local', N'Bể bơi nhỏ',         '3@07:00'),       -- full, has a cancelled session + makeup
(N'Bơi lội Intermediate',  N'BL-Intermediate-K01', '2026-11-04', 4, 'coach.bl2@sportcenter.local', N'Bể bơi lớn',         '4@15:00'),
(N'Bóng chuyền Beginner',  N'BC-Beginner-K01',     '2026-11-04', 4, 'coach.bc1@sportcenter.local', N'Sân bóng chuyền 1',  '4@18:00');

DECLARE @slots dbo.IdList, @class_id INT;
DECLARE @seq INT = 1, @max_seq INT = (SELECT MAX(seq) FROM @plan);
DECLARE @course_id INT, @class_name NVARCHAR(100), @start DATE, @cap INT, @coach INT, @room INT, @slot_text VARCHAR(100);

WHILE @seq <= @max_seq
BEGIN
    SELECT @course_id  = (SELECT course_id FROM course WHERE course_name = p.course_name),
           @class_name = p.class_name, @start = p.start_date, @cap = p.max_capacity,
           @coach      = (SELECT user_id FROM [user] WHERE email = p.coach_email),
           @room       = (SELECT room_id FROM room WHERE room_name = p.room_name),
           @slot_text  = p.slots
    FROM @plan p WHERE p.seq = @seq;

    DELETE FROM @slots;
    INSERT INTO @slots (id)
    SELECT ts.time_slot_id
    FROM STRING_SPLIT(@slot_text, ',') x
    JOIN time_slot ts ON ts.day_of_week = CAST(LEFT(x.value, 1) AS TINYINT)
                     AND ts.start_time = CAST(SUBSTRING(x.value, 3, 5) AS TIME);

    EXEC dbo.sp_create_class @mgr, @course_id, @class_name, @start, @cap, @coach, @room, @slots, @class_id OUTPUT;
    SET @seq += 1;
END;

-- 6. Enrollments (full class CL-Beginner-K01 4/4 and BL-Beginner-K01 3/3; one Pending; one Dropped)
INSERT INTO class_members (class_id, member_id, registered_by, status)
SELECT c.class_id, u.user_id, st.user_id, v.status
FROM (VALUES
    (N'CL-Beginner-K01',     'member1@sportcenter.local', NULL,                       'Active'),
    (N'CL-Beginner-K01',     'member2@sportcenter.local', NULL,                       'Active'),
    (N'CL-Beginner-K01',     'member3@sportcenter.local', NULL,                       'Active'),
    (N'CL-Beginner-K01',     'member4@sportcenter.local', 'staff1@sportcenter.local', 'Active'),
    (N'CL-Beginner-K02',     'member5@sportcenter.local', NULL,                       'Active'),
    (N'CL-Beginner-K02',     'member1@sportcenter.local', NULL,                       'Pending'),
    (N'BR-Intermediate-K01', 'member2@sportcenter.local', NULL,                       'Active'),
    (N'BR-Intermediate-K01', 'member3@sportcenter.local', NULL,                       'Active'),
    (N'BD-Intermediate-K01', 'member4@sportcenter.local', NULL,                       'Active'),
    (N'BD-Intermediate-K01', 'member5@sportcenter.local', NULL,                       'Active'),
    (N'BL-Beginner-K01',     'member1@sportcenter.local', NULL,                       'Active'),
    (N'BL-Beginner-K01',     'member2@sportcenter.local', 'staff1@sportcenter.local', 'Active'),
    (N'BL-Beginner-K01',     'member5@sportcenter.local', NULL,                       'Active'),
    (N'BL-Intermediate-K01', 'member3@sportcenter.local', NULL,                       'Active'),
    (N'BL-Intermediate-K01', 'member4@sportcenter.local', NULL,                       'Dropped'),
    (N'BC-Beginner-K01',     'member5@sportcenter.local', 'staff2@sportcenter.local', 'Active')  -- member5 is free Wed 18:00
) AS v(class_name, member_email, registrar_email, status)
JOIN class c ON c.class_name = v.class_name
JOIN [user] u ON u.email = v.member_email
LEFT JOIN [user] st ON st.email = v.registrar_email;

-- 7. Payments: one Paid, one Pending (sample payOS-style values)
INSERT INTO payment (enrollment_id, order_code, amount, description, status, paid_at, created_by)
SELECT cm.enrollment_id, v.order_code, co.tuition_fee, v.description, v.status,
       CASE WHEN v.status = 'Paid' THEN '2026-10-01T09:30:00' END, NULL
FROM (VALUES
    (N'CL-Beginner-K01', 'member1@sportcenter.local', 100001, 'CL Beginner K01 - An', 'Paid'),
    (N'CL-Beginner-K02', 'member1@sportcenter.local', 100002, 'CL Beginner K02 - An', 'Pending')
) AS v(class_name, member_email, order_code, description, status)
JOIN class c ON c.class_name = v.class_name
JOIN course co ON co.course_id = c.course_id
JOIN [user] u ON u.email = v.member_email
JOIN class_members cm ON cm.class_id = c.class_id AND cm.member_id = u.user_id;

-- 8. Reassigned session: BR-Intermediate-K01 session 2 is taught by another basketball coach
DECLARE @s_reassign INT = (SELECT s.session_id FROM session s JOIN class c ON c.class_id = s.class_id
                           WHERE c.class_name = N'BR-Intermediate-K01' AND s.session_no = 2);
DECLARE @coach_sub INT = (SELECT user_id FROM [user] WHERE email = 'coach.br1@sportcenter.local');
EXEC dbo.sp_reassign_session @mgr, @s_reassign, @coach_sub, NULL;

-- 9. Cancelled session with automatic makeup: BL-Beginner-K01 session 2
DECLARE @s_cancel INT = (SELECT s.session_id FROM session s JOIN class c ON c.class_id = s.class_id
                         WHERE c.class_name = N'BL-Beginner-K01' AND s.session_no = 2);
DECLARE @makeup INT;
EXEC dbo.sp_cancel_session @mgr, @s_cancel, @makeup OUTPUT;
IF @makeup IS NULL THROW 60000, N'Seed: expected an automatic makeup session', 1;

-- 10. Sessions before 2026-10-20 are already taught: Completed, with attendance for Active members
UPDATE session SET status = 'Completed'
WHERE session_date < '2026-10-20' AND status = 'Scheduled';

UPDATE class SET status = 'Ongoing'
WHERE status = 'Enrolling'
  AND EXISTS (SELECT 1 FROM session s WHERE s.class_id = class.class_id AND s.status = 'Completed');

INSERT INTO attendance (session_id, member_id, status, check_in_time, remarks, marked_by)
SELECT s.session_id, cm.member_id, a.status,
       CASE a.status
            WHEN 'Present' THEN DATEADD(MINUTE, -5,  CAST(s.session_date AS DATETIME) + CAST(ts.start_time AS DATETIME))
            WHEN 'Late'    THEN DATEADD(MINUTE, 12,  CAST(s.session_date AS DATETIME) + CAST(ts.start_time AS DATETIME))
       END,
       CASE a.status WHEN 'Late' THEN N'Đến muộn 12 phút' WHEN 'Absent' THEN N'Xin nghỉ có báo trước' END,
       s.coach_id
FROM session s
JOIN time_slot ts ON ts.time_slot_id = s.time_slot_id
JOIN class_members cm ON cm.class_id = s.class_id AND cm.status = 'Active'
CROSS APPLY (SELECT CASE (s.session_no + cm.enrollment_id) % 6
                         WHEN 0 THEN 'Absent' WHEN 1 THEN 'Late' ELSE 'Present' END AS status) a
WHERE s.status = 'Completed';
GO
