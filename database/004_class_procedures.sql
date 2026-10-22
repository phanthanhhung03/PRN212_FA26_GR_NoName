USE SportCenterDB;
GO

-- Manager creates a class: writes class, class_time_slot and all planned sessions
-- in one transaction. Any failed check throws before the first INSERT; XACT_ABORT
-- rolls everything back on any error (including a unique-index race on session).
CREATE PROCEDURE dbo.sp_create_class
    @actor_id     INT,
    @course_id    INT,
    @class_name   NVARCHAR(100),
    @start_date   DATE,
    @max_capacity INT,
    @coach_id     INT,
    @room_id      INT,
    @slots        dbo.IdList READONLY,
    @class_id     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @class_id = NULL;

    BEGIN TRAN;

    IF NOT EXISTS (
        SELECT 1
        FROM [user] u
        JOIN [role] r ON r.role_id = u.role_id
        WHERE u.user_id = @actor_id AND r.role_name = N'Manager' AND u.status = 'Active')
        THROW 50001, N'Chỉ Manager đang hoạt động mới được tạo lớp.', 1;

    DECLARE @sport_id INT, @total INT;
    SELECT @sport_id = sport_id, @total = total_sessions
    FROM course
    WHERE course_id = @course_id AND status = 'Active';
    IF @sport_id IS NULL
        THROW 50002, N'Khóa học không tồn tại hoặc đã ngừng.', 1;

    IF NOT EXISTS (SELECT 1 FROM @slots)
       OR EXISTS (SELECT 1 FROM @slots s WHERE NOT EXISTS (SELECT 1 FROM time_slot ts WHERE ts.time_slot_id = s.id))
        THROW 50008, N'Danh sách khung giờ rỗng hoặc không hợp lệ.', 1;

    IF NOT EXISTS (
        SELECT 1
        FROM [user] u
        JOIN [role] r ON r.role_id = u.role_id
        WHERE u.user_id = @coach_id AND r.role_name = N'Coach'
          AND u.status = 'Active' AND u.sport_id = @sport_id)
        THROW 50003, N'Coach không hợp lệ cho môn của khóa học.', 1;

    DECLARE @room_capacity INT;
    SELECT @room_capacity = capacity
    FROM room
    WHERE room_id = @room_id AND status = 'Active' AND sport_id = @sport_id;
    IF @room_capacity IS NULL
        THROW 50004, N'Phòng không hợp lệ cho môn của khóa học.', 1;

    IF @max_capacity > @room_capacity
        THROW 50005, N'Sĩ số tối đa lớn hơn sức chứa phòng.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.fn_planned_dates(@start_date, @slots, @total) p
        JOIN session s ON s.session_date = p.session_date
                      AND s.time_slot_id = p.time_slot_id
                      AND s.coach_id = @coach_id
                      AND s.status <> 'Cancelled')
        THROW 50006, N'Coach đã bận ở một buổi dự kiến.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.fn_planned_dates(@start_date, @slots, @total) p
        JOIN session s ON s.session_date = p.session_date
                      AND s.time_slot_id = p.time_slot_id
                      AND s.room_id = @room_id
                      AND s.status <> 'Cancelled')
        THROW 50007, N'Phòng đã bận ở một buổi dự kiến.', 1;

    INSERT INTO class (course_id, coach_id, room_id, class_name, start_date, max_capacity, status)
    VALUES (@course_id, @coach_id, @room_id, @class_name, @start_date, @max_capacity, 'Enrolling');
    SET @class_id = SCOPE_IDENTITY();

    INSERT INTO class_time_slot (class_id, time_slot_id)
    SELECT @class_id, id FROM @slots;

    INSERT INTO session (class_id, session_no, session_date, time_slot_id, coach_id, room_id, status)
    SELECT @class_id, p.session_no, p.session_date, p.time_slot_id, @coach_id, @room_id, 'Scheduled'
    FROM dbo.fn_planned_dates(@start_date, @slots, @total) p;

    UPDATE class
    SET end_date = (SELECT MAX(session_date) FROM session WHERE class_id = @class_id)
    WHERE class_id = @class_id;

    COMMIT;
END;
GO
