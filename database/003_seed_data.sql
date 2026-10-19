USE SportCenterDB;
GO
SET NOCOUNT ON;

-- =====================================================
-- 003: Dữ liệu mẫu để test
-- Chạy sau 001 và 002. Chạy bằng tài khoản quản trị (sa/dbo),
-- không chạy bằng app_user.
-- =====================================================

-- Mật khẩu chung cho tất cả tài khoản mẫu: 'Password123!'
-- Thay bằng hash BCrypt thật (sinh bằng BCrypt.Net-Next trong app) trước khi chạy.
DECLARE @hash VARCHAR(255) = '<hash_bcrypt_cua_Password123!>';
DECLARE @mgr  INT;

-- 1. Manager: cập nhật hash thật cho tài khoản đã tạo ở 001
UPDATE [user] SET password_hash = @hash WHERE email = 'manager@sportcenter.local';
SELECT @mgr = user_id FROM [user] WHERE email = 'manager@sportcenter.local';

-- 2. Staff (2 người), tạo qua procedure với Manager làm người tạo
EXEC sp_create_staff_coach @mgr, N'Staff', N'Lê Thị Thu',  'staff1@sportcenter.local', @hash, '0901000001';
EXEC sp_create_staff_coach @mgr, N'Staff', N'Phạm Minh Hằng', 'staff2@sportcenter.local', @hash, '0901000002';

-- 3. Coach (10 người, 2 cho mỗi môn)
EXEC sp_create_staff_coach @mgr, N'Coach', N'Trần Quốc Bảo',   'coach.cl1@sportcenter.local', @hash, '0902000001';
EXEC sp_create_staff_coach @mgr, N'Coach', N'Nguyễn Hữu Dũng', 'coach.cl2@sportcenter.local', @hash, '0902000002';
EXEC sp_create_staff_coach @mgr, N'Coach', N'Đặng Anh Tuấn',   'coach.br1@sportcenter.local', @hash, '0902000003';
EXEC sp_create_staff_coach @mgr, N'Coach', N'Võ Thành Long',   'coach.br2@sportcenter.local', @hash, '0902000004';
EXEC sp_create_staff_coach @mgr, N'Coach', N'Hoàng Gia Huy',   'coach.bd1@sportcenter.local', @hash, '0902000005';
EXEC sp_create_staff_coach @mgr, N'Coach', N'Bùi Đức Mạnh',    'coach.bd2@sportcenter.local', @hash, '0902000006';
EXEC sp_create_staff_coach @mgr, N'Coach', N'Lý Minh Quân',    'coach.bl1@sportcenter.local', @hash, '0902000007';
EXEC sp_create_staff_coach @mgr, N'Coach', N'Ngô Thanh Sơn',   'coach.bl2@sportcenter.local', @hash, '0902000008';
EXEC sp_create_staff_coach @mgr, N'Coach', N'Đỗ Hải Yến',      'coach.bc1@sportcenter.local', @hash, '0902000009';
EXEC sp_create_staff_coach @mgr, N'Coach', N'Vũ Thu Trang',    'coach.bc2@sportcenter.local', @hash, '0902000010';

-- 4. Member (5 người) tự đăng ký
EXEC sp_register_member N'Nguyễn Văn An',  'member1@sportcenter.local', @hash, '0903000001';
EXEC sp_register_member N'Trần Thị Bình',  'member2@sportcenter.local', @hash, '0903000002';
EXEC sp_register_member N'Lê Quang Cường', 'member3@sportcenter.local', @hash, '0903000003';
EXEC sp_register_member N'Phan Thị Diệu',  'member4@sportcenter.local', @hash, '0903000004';
EXEC sp_register_member N'Hồ Minh Đức',    'member5@sportcenter.local', @hash, '0903000005';

-- 5. Môn thể thao (5)
INSERT INTO sport (sport_name, description) VALUES
(N'Cầu lông',   N'Bộ môn cầu lông trong nhà'),
(N'Bóng rổ',    N'Bộ môn bóng rổ'),
(N'Bóng đá',    N'Bộ môn bóng đá sân mini'),
(N'Bơi lội',    N'Bộ môn bơi lội, bể bơi trong nhà'),
(N'Bóng chuyền', N'Bộ môn bóng chuyền');

-- 6. Khóa học (2 khóa mỗi môn)
INSERT INTO course (sport_id, course_name, level, total_sessions, tuition_fee, description) VALUES
((SELECT sport_id FROM sport WHERE sport_name = N'Cầu lông'), N'Cầu lông Beginner',     N'Beginner',     24, 1500000, N'Nhập môn cầu lông'),
((SELECT sport_id FROM sport WHERE sport_name = N'Cầu lông'), N'Cầu lông Advanced',     N'Advanced',     24, 2200000, N'Nâng cao kỹ thuật'),
((SELECT sport_id FROM sport WHERE sport_name = N'Bóng rổ'),  N'Bóng rổ Beginner',      N'Beginner',     24, 1800000, N'Nhập môn bóng rổ'),
((SELECT sport_id FROM sport WHERE sport_name = N'Bóng rổ'),  N'Bóng rổ Intermediate',  N'Intermediate', 24, 2000000, N'Trung cấp bóng rổ'),
((SELECT sport_id FROM sport WHERE sport_name = N'Bóng đá'),  N'Bóng đá Beginner',      N'Beginner',     20, 1700000, N'Nhập môn bóng đá'),
((SELECT sport_id FROM sport WHERE sport_name = N'Bóng đá'),  N'Bóng đá Intermediate',  N'Intermediate', 20, 2100000, N'Trung cấp bóng đá'),
((SELECT sport_id FROM sport WHERE sport_name = N'Bơi lội'),  N'Bơi lội Beginner',      N'Beginner',     16, 1200000, N'Học bơi cơ bản'),
((SELECT sport_id FROM sport WHERE sport_name = N'Bơi lội'),  N'Bơi lội Intermediate',  N'Intermediate', 16, 1400000, N'Bơi kỹ thuật'),
((SELECT sport_id FROM sport WHERE sport_name = N'Bóng chuyền'), N'Bóng chuyền Beginner',     N'Beginner',     20, 1300000, N'Nhập môn bóng chuyền'),
((SELECT sport_id FROM sport WHERE sport_name = N'Bóng chuyền'), N'Bóng chuyền Intermediate', N'Intermediate', 20, 1600000, N'Trung cấp bóng chuyền');

-- 7. Khung giờ (time_slot) trong khoảng 07:00-20:00
INSERT INTO time_slot (slot_name, day_of_week, start_time, end_time) VALUES
(N'Sáng T2',  2, '07:00', '08:30'),
(N'Tối T2',   2, '18:00', '19:30'),
(N'Tối T4',   4, '18:00', '19:30'),
(N'Chiều T7', 7, '15:00', '16:30'),
(N'Sáng CN',  8, '08:00', '09:30'),
(N'Tối T6',   6, '18:00', '19:30');

-- 8. Lớp học (một vài đầy, một vài còn chỗ, có lớp hủy và đang diễn ra)
INSERT INTO class (course_id, coach_id, class_name, start_date, end_date, max_capacity, status) VALUES
-- Cầu lông
((SELECT course_id FROM course WHERE course_name = N'Cầu lông Beginner'), (SELECT user_id FROM [user] WHERE email='coach.cl1@sportcenter.local'), N'CL-Beginner-K01',  '2026-11-02', '2027-01-31', 4, 'Enrolling'),
((SELECT course_id FROM course WHERE course_name = N'Cầu lông Beginner'), (SELECT user_id FROM [user] WHERE email='coach.cl2@sportcenter.local'), N'CL-Beginner-K02',  '2026-11-02', '2027-01-31', 4, 'Enrolling'),
((SELECT course_id FROM course WHERE course_name = N'Cầu lông Advanced'), (SELECT user_id FROM [user] WHERE email='coach.cl1@sportcenter.local'), N'CL-Advanced-K01', '2026-11-05', '2027-02-05', 4, 'Cancelled'),
-- Bóng rổ
((SELECT course_id FROM course WHERE course_name = N'Bóng rổ Beginner'),     (SELECT user_id FROM [user] WHERE email='coach.br1@sportcenter.local'), N'BR-Beginner-K01',     '2026-11-03', '2027-02-02', 3, 'Enrolling'),
((SELECT course_id FROM course WHERE course_name = N'Bóng rổ Intermediate'), (SELECT user_id FROM [user] WHERE email='coach.br2@sportcenter.local'), N'BR-Intermediate-K01', '2026-11-04', '2027-02-03', 5, 'Enrolling'),
-- Bóng đá
((SELECT course_id FROM course WHERE course_name = N'Bóng đá Beginner'),     (SELECT user_id FROM [user] WHERE email='coach.bd1@sportcenter.local'), N'BD-Beginner-K01',     '2026-11-02', '2027-01-25', 5, 'Enrolling'),
((SELECT course_id FROM course WHERE course_name = N'Bóng đá Intermediate'), (SELECT user_id FROM [user] WHERE email='coach.bd2@sportcenter.local'), N'BD-Intermediate-K01', '2026-10-05', '2026-12-20', 5, 'Ongoing'),
-- Bơi lội
((SELECT course_id FROM course WHERE course_name = N'Bơi lội Beginner'),     (SELECT user_id FROM [user] WHERE email='coach.bl1@sportcenter.local'), N'BL-Beginner-K01',     '2026-11-03', '2027-02-02', 3, 'Enrolling'),
((SELECT course_id FROM course WHERE course_name = N'Bơi lội Intermediate'), (SELECT user_id FROM [user] WHERE email='coach.bl2@sportcenter.local'), N'BL-Intermediate-K01', '2026-11-04', '2027-02-03', 4, 'Enrolling'),
-- Bóng chuyền
((SELECT course_id FROM course WHERE course_name = N'Bóng chuyền Beginner'), (SELECT user_id FROM [user] WHERE email='coach.bc1@sportcenter.local'), N'BC-Beginner-K01', '2026-11-03', '2027-02-02', 4, 'Enrolling');

-- 9. Khung giờ của lớp (class_time_slot)
INSERT INTO class_time_slot (class_id, time_slot_id) VALUES
((SELECT class_id FROM class WHERE class_name = N'CL-Beginner-K01'),  (SELECT time_slot_id FROM time_slot WHERE slot_name = N'Tối T2')),
((SELECT class_id FROM class WHERE class_name = N'CL-Beginner-K01'),  (SELECT time_slot_id FROM time_slot WHERE slot_name = N'Tối T4')),
((SELECT class_id FROM class WHERE class_name = N'CL-Beginner-K02'),  (SELECT time_slot_id FROM time_slot WHERE slot_name = N'Sáng T2')),
((SELECT class_id FROM class WHERE class_name = N'BR-Intermediate-K01'), (SELECT time_slot_id FROM time_slot WHERE slot_name = N'Tối T6')),
((SELECT class_id FROM class WHERE class_name = N'BD-Intermediate-K01'), (SELECT time_slot_id FROM time_slot WHERE slot_name = N'Chiều T7')),
((SELECT class_id FROM class WHERE class_name = N'BL-Beginner-K01'),    (SELECT time_slot_id FROM time_slot WHERE slot_name = N'Sáng CN')),
((SELECT class_id FROM class WHERE class_name = N'BC-Beginner-K01'),    (SELECT time_slot_id FROM time_slot WHERE slot_name = N'Tối T4'));

-- 10. Ghi danh (class_members)
-- CL-Beginner-K01: đầy (4/4 Active)
INSERT INTO class_members (class_id, member_id, registered_by, status) VALUES
((SELECT class_id FROM class WHERE class_name=N'CL-Beginner-K01'), (SELECT user_id FROM [user] WHERE email='member1@sportcenter.local'), NULL, 'Active'),
((SELECT class_id FROM class WHERE class_name=N'CL-Beginner-K01'), (SELECT user_id FROM [user] WHERE email='member2@sportcenter.local'), NULL, 'Active'),
((SELECT class_id FROM class WHERE class_name=N'CL-Beginner-K01'), (SELECT user_id FROM [user] WHERE email='member3@sportcenter.local'), NULL, 'Active'),
((SELECT class_id FROM class WHERE class_name=N'CL-Beginner-K01'), (SELECT user_id FROM [user] WHERE email='member4@sportcenter.local'), (SELECT user_id FROM [user] WHERE email='staff1@sportcenter.local'), 'Active'),
-- CL-Beginner-K02: còn chỗ, có một ghi danh Pending
((SELECT class_id FROM class WHERE class_name=N'CL-Beginner-K02'), (SELECT user_id FROM [user] WHERE email='member5@sportcenter.local'), NULL, 'Active'),
((SELECT class_id FROM class WHERE class_name=N'CL-Beginner-K02'), (SELECT user_id FROM [user] WHERE email='member1@sportcenter.local'), NULL, 'Pending'),
-- BR-Intermediate-K01: 2/5 Active
((SELECT class_id FROM class WHERE class_name=N'BR-Intermediate-K01'), (SELECT user_id FROM [user] WHERE email='member2@sportcenter.local'), NULL, 'Active'),
((SELECT class_id FROM class WHERE class_name=N'BR-Intermediate-K01'), (SELECT user_id FROM [user] WHERE email='member3@sportcenter.local'), NULL, 'Active'),
-- BD-Intermediate-K01 (Ongoing): 2/5 Active
((SELECT class_id FROM class WHERE class_name=N'BD-Intermediate-K01'), (SELECT user_id FROM [user] WHERE email='member4@sportcenter.local'), NULL, 'Active'),
((SELECT class_id FROM class WHERE class_name=N'BD-Intermediate-K01'), (SELECT user_id FROM [user] WHERE email='member5@sportcenter.local'), NULL, 'Active'),
-- BL-Beginner-K01: đầy (3/3 Active), một người do Staff ghi danh hộ
((SELECT class_id FROM class WHERE class_name=N'BL-Beginner-K01'), (SELECT user_id FROM [user] WHERE email='member1@sportcenter.local'), NULL, 'Active'),
((SELECT class_id FROM class WHERE class_name=N'BL-Beginner-K01'), (SELECT user_id FROM [user] WHERE email='member2@sportcenter.local'), (SELECT user_id FROM [user] WHERE email='staff1@sportcenter.local'), 'Active'),
((SELECT class_id FROM class WHERE class_name=N'BL-Beginner-K01'), (SELECT user_id FROM [user] WHERE email='member5@sportcenter.local'), NULL, 'Active'),
-- BL-Intermediate-K01: 1 Active, 1 Dropped
((SELECT class_id FROM class WHERE class_name=N'BL-Intermediate-K01'), (SELECT user_id FROM [user] WHERE email='member3@sportcenter.local'), NULL, 'Active'),
((SELECT class_id FROM class WHERE class_name=N'BL-Intermediate-K01'), (SELECT user_id FROM [user] WHERE email='member4@sportcenter.local'), NULL, 'Dropped'),
-- BC-Beginner-K01: 1 Active, do Staff ghi danh hộ
((SELECT class_id FROM class WHERE class_name=N'BC-Beginner-K01'), (SELECT user_id FROM [user] WHERE email='member2@sportcenter.local'), (SELECT user_id FROM [user] WHERE email='staff2@sportcenter.local'), 'Active');
-- CL-Advanced-K01 (Cancelled), BR-Beginner-K01, BD-Beginner-K01: không có học viên

-- 11. Thanh toán mẫu (payment)
INSERT INTO payment (enrollment_id, order_code, amount, description, status, paid_at, created_by) VALUES
((SELECT cm.enrollment_id FROM class_members cm JOIN class c ON c.class_id = cm.class_id JOIN [user] u ON u.user_id = cm.member_id
  WHERE c.class_name = N'CL-Beginner-K01' AND u.email = 'member1@sportcenter.local'),
 100001, 1500000, N'CL Beginner K01 - An', 'Paid', GETDATE(), NULL),
((SELECT cm.enrollment_id FROM class_members cm JOIN class c ON c.class_id = cm.class_id JOIN [user] u ON u.user_id = cm.member_id
  WHERE c.class_name = N'CL-Beginner-K02' AND u.email = 'member1@sportcenter.local'),
 100002, 1500000, N'CL Beginner K02 - An', 'Pending', NULL, NULL);

-- 12. Buổi học (schedule) cho 2 lớp đang hoạt động để test điểm danh
INSERT INTO schedule (class_id, session_no, session_date, start_time, end_time, room_location, session_note, status, time_slot_id) VALUES
((SELECT class_id FROM class WHERE class_name=N'CL-Beginner-K01'), 1, '2026-11-02', '18:00', '19:30', N'Sân 1', NULL, 'Scheduled', (SELECT time_slot_id FROM time_slot WHERE slot_name=N'Tối T2')),
((SELECT class_id FROM class WHERE class_name=N'CL-Beginner-K01'), 2, '2026-11-04', '18:00', '19:30', N'Sân 1', NULL, 'Scheduled', (SELECT time_slot_id FROM time_slot WHERE slot_name=N'Tối T4')),
((SELECT class_id FROM class WHERE class_name=N'BD-Intermediate-K01'), 1, '2026-10-10', '15:00', '16:30', N'Sân cỏ 2', N'Buổi đầu, làm quen đội hình', 'Completed', (SELECT time_slot_id FROM time_slot WHERE slot_name=N'Chiều T7')),
((SELECT class_id FROM class WHERE class_name=N'BD-Intermediate-K01'), 2, '2026-10-17', '15:00', '16:30', N'Sân cỏ 2', N'Luyện kỹ thuật chuyền bóng', 'Completed', (SELECT time_slot_id FROM time_slot WHERE slot_name=N'Chiều T7'));

-- 13. Điểm danh mẫu cho 2 buổi đã hoàn thành của BD-Intermediate-K01
INSERT INTO attendance (schedule_id, member_id, status, check_in_time, remarks, marked_by) VALUES
((SELECT s.schedule_id FROM schedule s JOIN class c ON c.class_id = s.class_id WHERE c.class_name=N'BD-Intermediate-K01' AND s.session_no=1),
 (SELECT user_id FROM [user] WHERE email='member4@sportcenter.local'), 'Present', '2026-10-10 14:55', NULL,
 (SELECT user_id FROM [user] WHERE email='coach.bd2@sportcenter.local')),
((SELECT s.schedule_id FROM schedule s JOIN class c ON c.class_id = s.class_id WHERE c.class_name=N'BD-Intermediate-K01' AND s.session_no=1),
 (SELECT user_id FROM [user] WHERE email='member5@sportcenter.local'), 'Late', '2026-10-10 15:12', N'Đến muộn 12 phút',
 (SELECT user_id FROM [user] WHERE email='coach.bd2@sportcenter.local')),
((SELECT s.schedule_id FROM schedule s JOIN class c ON c.class_id = s.class_id WHERE c.class_name=N'BD-Intermediate-K01' AND s.session_no=2),
 (SELECT user_id FROM [user] WHERE email='member4@sportcenter.local'), 'Absent', NULL, N'Xin nghỉ có báo trước',
 (SELECT user_id FROM [user] WHERE email='coach.bd2@sportcenter.local')),
((SELECT s.schedule_id FROM schedule s JOIN class c ON c.class_id = s.class_id WHERE c.class_name=N'BD-Intermediate-K01' AND s.session_no=2),
 (SELECT user_id FROM [user] WHERE email='member5@sportcenter.local'), 'Present', '2026-10-17 14:58', NULL,
 (SELECT user_id FROM [user] WHERE email='coach.bd2@sportcenter.local'));

-- 14. Ghi nhận migration
INSERT INTO schema_migrations (file_name) VALUES ('003_seed_data.sql');
GO
