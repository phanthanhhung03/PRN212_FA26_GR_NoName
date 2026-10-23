USE SportCenterDB;
GO

-- =====================================================
-- 008: Which weekly time slots are busy for a given room and coach?
-- Used by the "Open class" form to grey out slots that cannot be chosen.
--
-- For every time_slot, the procedure plans the class as if that slot were added to the
-- slots already selected (@selected), using the real calendar (fn_planned_dates, which
-- depends on @start_date and @total). Only the dates of the slot being tested are checked
-- against existing non-Cancelled sessions of the room and of the coach, so a slot the
-- Manager has already selected is evaluated as part of the current selection.
--
-- Result: one row per busy slot: time_slot_id, reason ('Room' | 'Coach' | 'Both').
-- Free slots are not returned. Read-only: no writes, no transaction.
-- =====================================================
CREATE PROCEDURE dbo.sp_busy_slots
    @room_id    INT,
    @coach_id   INT,
    @start_date DATE,
    @selected   dbo.IdList READONLY,
    @total      INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @result TABLE (time_slot_id INT PRIMARY KEY, reason VARCHAR(10) NOT NULL);
    DECLARE @candidate dbo.IdList;
    DECLARE @slot INT, @room_busy BIT, @coach_busy BIT;

    DECLARE slot_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT time_slot_id FROM dbo.time_slot ORDER BY time_slot_id;
    OPEN slot_cursor;
    FETCH NEXT FROM slot_cursor INTO @slot;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        DELETE FROM @candidate;
        INSERT INTO @candidate (id) SELECT id FROM @selected;
        IF NOT EXISTS (SELECT 1 FROM @candidate WHERE id = @slot)
            INSERT INTO @candidate (id) VALUES (@slot);

        SET @room_busy = CASE WHEN EXISTS (
            SELECT 1
            FROM dbo.fn_planned_dates(@start_date, @candidate, @total) p
            JOIN dbo.session s
              ON s.session_date = p.session_date
             AND s.time_slot_id = p.time_slot_id
             AND s.status <> 'Cancelled'
             AND s.room_id = @room_id
            WHERE p.time_slot_id = @slot) THEN 1 ELSE 0 END;

        SET @coach_busy = CASE WHEN EXISTS (
            SELECT 1
            FROM dbo.fn_planned_dates(@start_date, @candidate, @total) p
            JOIN dbo.session s
              ON s.session_date = p.session_date
             AND s.time_slot_id = p.time_slot_id
             AND s.status <> 'Cancelled'
             AND s.coach_id = @coach_id
            WHERE p.time_slot_id = @slot) THEN 1 ELSE 0 END;

        IF @room_busy = 1 OR @coach_busy = 1
            INSERT INTO @result (time_slot_id, reason)
            VALUES (@slot, CASE WHEN @room_busy = 1 AND @coach_busy = 1 THEN 'Both'
                                WHEN @room_busy = 1 THEN 'Room'
                                ELSE 'Coach' END);

        FETCH NEXT FROM slot_cursor INTO @slot;
    END

    CLOSE slot_cursor;
    DEALLOCATE slot_cursor;

    SELECT time_slot_id, reason FROM @result ORDER BY time_slot_id;
END;
GO

GRANT EXECUTE ON dbo.sp_busy_slots TO app_user;
GO
