USE SportCenterDB;
GO

-- Internal helper (not granted to app_user): validates a coach and/or room for one
-- session at (@session_date, @time_slot_id). A NULL @coach_id / @room_id skips that part.
-- @exclude_session_id is ignored in the busy checks (the session being changed).
-- Order: 50003 coach invalid, 50004 room invalid, 50005 capacity, 50006 coach busy, 50007 room busy.
CREATE PROCEDURE dbo.usp_check_assignment
    @sport_id           INT,
    @max_capacity       INT,
    @session_date       DATE,
    @time_slot_id       INT,
    @coach_id           INT,
    @room_id            INT,
    @exclude_session_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @coach_id IS NOT NULL AND NOT EXISTS (
        SELECT 1
        FROM [user] u
        JOIN [role] r ON r.role_id = u.role_id
        WHERE u.user_id = @coach_id AND r.role_name = N'Coach'
          AND u.status = 'Active' AND u.sport_id = @sport_id)
        THROW 50003, N'Coach không hợp lệ cho môn của lớp.', 1;

    IF @room_id IS NOT NULL
    BEGIN
        DECLARE @room_capacity INT;
        SELECT @room_capacity = capacity
        FROM room
        WHERE room_id = @room_id AND status = 'Active' AND sport_id = @sport_id;
        IF @room_capacity IS NULL
            THROW 50004, N'Phòng không hợp lệ cho môn của lớp.', 1;
        IF @max_capacity > @room_capacity
            THROW 50005, N'Sĩ số tối đa lớn hơn sức chứa phòng.', 1;
    END;

    IF @coach_id IS NOT NULL AND EXISTS (
        SELECT 1 FROM session
        WHERE coach_id = @coach_id AND session_date = @session_date
          AND time_slot_id = @time_slot_id AND status <> 'Cancelled'
          AND session_id <> ISNULL(@exclude_session_id, -1))
        THROW 50006, N'Coach đã bận ở ngày và khung giờ này.', 1;

    IF @room_id IS NOT NULL AND EXISTS (
        SELECT 1 FROM session
        WHERE room_id = @room_id AND session_date = @session_date
          AND time_slot_id = @time_slot_id AND status <> 'Cancelled'
          AND session_id <> ISNULL(@exclude_session_id, -1))
        THROW 50007, N'Phòng đã bận ở ngày và khung giờ này.', 1;
END;
GO

-- Manager temporarily changes the coach and/or room of one Scheduled session.
-- NULL parameter = keep current value. The class's own coach/room are not touched.
CREATE PROCEDURE dbo.sp_reassign_session
    @actor_id   INT,
    @session_id INT,
    @coach_id   INT = NULL,
    @room_id    INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    IF NOT EXISTS (
        SELECT 1
        FROM [user] u
        JOIN [role] r ON r.role_id = u.role_id
        WHERE u.user_id = @actor_id AND r.role_name = N'Manager' AND u.status = 'Active')
        THROW 50001, N'Chỉ Manager đang hoạt động mới được đổi coach/phòng.', 1;

    DECLARE @sport_id INT, @max_capacity INT, @session_date DATE, @time_slot_id INT;
    SELECT @sport_id = co.sport_id, @max_capacity = c.max_capacity,
           @session_date = s.session_date, @time_slot_id = s.time_slot_id
    FROM session s WITH (UPDLOCK, HOLDLOCK)
    JOIN class c   ON c.class_id = s.class_id
    JOIN course co ON co.course_id = c.course_id
    WHERE s.session_id = @session_id AND s.status = 'Scheduled';
    IF @sport_id IS NULL
        THROW 50009, N'Buổi học không tồn tại hoặc không ở trạng thái Scheduled.', 1;

    EXEC dbo.usp_check_assignment @sport_id, @max_capacity, @session_date, @time_slot_id,
                                  @coach_id, @room_id, @session_id;

    UPDATE session
    SET coach_id = ISNULL(@coach_id, coach_id),
        room_id  = ISNULL(@room_id, room_id)
    WHERE session_id = @session_id;

    COMMIT;
END;
GO

-- Manager cancels one Scheduled session, then tries an automatic makeup session:
-- earliest date after the class's latest session date (any status) matching one of the
-- class's slots (earlier slot first on the same day), with the class's coach and room.
-- If that coach or room is busy, @makeup_session_id = NULL and the cancel still stands.
CREATE PROCEDURE dbo.sp_cancel_session
    @actor_id          INT,
    @session_id        INT,
    @makeup_session_id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @makeup_session_id = NULL;

    BEGIN TRAN;

    IF NOT EXISTS (
        SELECT 1
        FROM [user] u
        JOIN [role] r ON r.role_id = u.role_id
        WHERE u.user_id = @actor_id AND r.role_name = N'Manager' AND u.status = 'Active')
        THROW 50001, N'Chỉ Manager đang hoạt động mới được hủy buổi.', 1;

    DECLARE @class_id INT;
    SELECT @class_id = class_id
    FROM session WITH (UPDLOCK, HOLDLOCK)
    WHERE session_id = @session_id AND status = 'Scheduled';
    IF @class_id IS NULL
        THROW 50009, N'Buổi học không tồn tại hoặc không ở trạng thái Scheduled.', 1;

    UPDATE session SET status = 'Cancelled' WHERE session_id = @session_id;

    DECLARE @coach_id INT, @room_id INT, @total INT, @valid INT, @last_date DATE;
    SELECT @coach_id = c.coach_id, @room_id = c.room_id, @total = co.total_sessions
    FROM class c
    JOIN course co ON co.course_id = c.course_id
    WHERE c.class_id = @class_id;

    SELECT @valid = COUNT(*) FROM session WHERE class_id = @class_id AND status <> 'Cancelled';
    SELECT @last_date = MAX(session_date) FROM session WHERE class_id = @class_id;

    IF @coach_id IS NOT NULL AND @valid < @total
    BEGIN
        DECLARE @mk_date DATE, @mk_slot INT;
        SELECT TOP (1) @mk_date = d.dt, @mk_slot = ts.time_slot_id
        FROM (VALUES (1),(2),(3),(4),(5),(6),(7)) AS n(k)
        CROSS APPLY (SELECT DATEADD(DAY, n.k, @last_date) AS dt) d
        JOIN class_time_slot cts ON cts.class_id = @class_id
        JOIN time_slot ts        ON ts.time_slot_id = cts.time_slot_id
                                AND ts.day_of_week = (DATEDIFF(DAY, '19000101', d.dt) % 7) + 2
        ORDER BY d.dt, ts.start_time;

        IF @mk_date IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM session
               WHERE session_date = @mk_date AND time_slot_id = @mk_slot
                 AND status <> 'Cancelled'
                 AND (coach_id = @coach_id OR room_id = @room_id))
        BEGIN
            INSERT INTO session (class_id, session_no, session_date, time_slot_id, coach_id, room_id, status)
            SELECT @class_id, ISNULL(MAX(session_no), 0) + 1, @mk_date, @mk_slot, @coach_id, @room_id, 'Scheduled'
            FROM session
            WHERE class_id = @class_id;
            SET @makeup_session_id = SCOPE_IDENTITY();
        END;
    END;

    UPDATE class
    SET end_date = (SELECT MAX(session_date) FROM session
                    WHERE class_id = @class_id AND status <> 'Cancelled')
    WHERE class_id = @class_id;

    COMMIT;
END;
GO

-- Manager adds a makeup session by hand (date, slot, coach, room) for a class that has
-- fewer valid sessions than course.total_sessions.
-- Check order: 50001 actor, 50011 class missing/Cancelled/already full, 50010 slot missing
-- or weekday mismatch, then 50003/50004/50005/50006/50007 via usp_check_assignment.
CREATE PROCEDURE dbo.sp_add_makeup_session
    @actor_id     INT,
    @class_id     INT,
    @session_date DATE,
    @time_slot_id INT,
    @coach_id     INT,
    @room_id      INT,
    @session_id   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @session_id = NULL;

    BEGIN TRAN;

    IF NOT EXISTS (
        SELECT 1
        FROM [user] u
        JOIN [role] r ON r.role_id = u.role_id
        WHERE u.user_id = @actor_id AND r.role_name = N'Manager' AND u.status = 'Active')
        THROW 50001, N'Chỉ Manager đang hoạt động mới được thêm buổi bù.', 1;

    DECLARE @sport_id INT, @max_capacity INT, @total INT, @valid INT;
    SELECT @sport_id = co.sport_id, @max_capacity = c.max_capacity, @total = co.total_sessions
    FROM class c WITH (UPDLOCK, HOLDLOCK)
    JOIN course co ON co.course_id = c.course_id
    WHERE c.class_id = @class_id AND c.status <> 'Cancelled';

    SELECT @valid = COUNT(*) FROM session WHERE class_id = @class_id AND status <> 'Cancelled';
    IF @sport_id IS NULL OR @valid >= @total
        THROW 50011, N'Lớp không tồn tại, đã hủy hoặc đã đủ số buổi.', 1;

    IF NOT EXISTS (
        SELECT 1 FROM time_slot
        WHERE time_slot_id = @time_slot_id
          AND day_of_week = (DATEDIFF(DAY, '19000101', @session_date) % 7) + 2)
        THROW 50010, N'Ngày buổi bù không khớp thứ của khung giờ.', 1;

    IF @coach_id IS NULL
        THROW 50003, N'Phải chọn coach cho buổi bù.', 1;
    IF @room_id IS NULL
        THROW 50004, N'Phải chọn phòng cho buổi bù.', 1;

    EXEC dbo.usp_check_assignment @sport_id, @max_capacity, @session_date, @time_slot_id,
                                  @coach_id, @room_id, NULL;

    INSERT INTO session (class_id, session_no, session_date, time_slot_id, coach_id, room_id, status)
    SELECT @class_id, ISNULL(MAX(session_no), 0) + 1, @session_date, @time_slot_id, @coach_id, @room_id, 'Scheduled'
    FROM session
    WHERE class_id = @class_id;
    SET @session_id = SCOPE_IDENTITY();

    UPDATE class
    SET end_date = (SELECT MAX(session_date) FROM session
                    WHERE class_id = @class_id AND status <> 'Cancelled')
    WHERE class_id = @class_id;

    COMMIT;
END;
GO
