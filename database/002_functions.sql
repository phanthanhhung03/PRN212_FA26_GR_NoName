USE SportCenterDB;
GO

-- Planned (date, slot) pairs for a class starting on @start_date.
-- Weekday is computed independent of DATEFIRST: 1900-01-01 is a Monday = 2.
CREATE FUNCTION dbo.fn_planned_dates
(
    @start_date DATE,
    @slots      dbo.IdList READONLY,
    @total      INT
)
RETURNS TABLE
AS RETURN
(
    WITH digits AS (SELECT n FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) v(n)),
    nums AS (
        SELECT a.n + 10 * b.n + 100 * c.n AS n
        FROM digits a CROSS JOIN digits b CROSS JOIN digits c
    ),
    cfg AS (
        -- weeks needed = CEILING(total / slot count) + 1
        SELECT (CEILING(CAST(@total AS FLOAT) / NULLIF((SELECT COUNT(*) FROM @slots), 0)) + 1) * 7 AS days
    ),
    cal AS (
        SELECT DATEADD(DAY, nums.n, @start_date) AS d
        FROM nums CROSS JOIN cfg
        WHERE nums.n < cfg.days
    ),
    pairs AS (
        SELECT cal.d AS session_date, ts.time_slot_id, ts.start_time
        FROM cal
        JOIN time_slot ts ON ts.day_of_week = (DATEDIFF(DAY, '19000101', cal.d) % 7) + 2
        JOIN @slots s ON s.id = ts.time_slot_id
    ),
    numbered AS (
        SELECT ROW_NUMBER() OVER (ORDER BY session_date, start_time) AS session_no,
               session_date, time_slot_id
        FROM pairs
    )
    SELECT session_no, session_date, time_slot_id
    FROM numbered
    WHERE session_no <= @total
);
GO

CREATE FUNCTION dbo.fn_free_coaches
(
    @sport_id   INT,
    @start_date DATE,
    @slots      dbo.IdList READONLY,
    @total      INT
)
RETURNS TABLE
AS RETURN
(
    SELECT u.user_id, u.full_name
    FROM [user] u
    JOIN [role] r ON r.role_id = u.role_id AND r.role_name = N'Coach'
    WHERE u.status = 'Active'
      AND u.sport_id = @sport_id
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.fn_planned_dates(@start_date, @slots, @total) p
          JOIN session s ON s.session_date = p.session_date
                        AND s.time_slot_id = p.time_slot_id
                        AND s.coach_id = u.user_id
                        AND s.status <> 'Cancelled')
);
GO

CREATE FUNCTION dbo.fn_free_rooms
(
    @sport_id     INT,
    @min_capacity INT,
    @start_date   DATE,
    @slots        dbo.IdList READONLY,
    @total        INT
)
RETURNS TABLE
AS RETURN
(
    SELECT rm.room_id, rm.room_name, rm.capacity
    FROM room rm
    WHERE rm.status = 'Active'
      AND rm.sport_id = @sport_id
      AND rm.capacity >= @min_capacity
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.fn_planned_dates(@start_date, @slots, @total) p
          JOIN session s ON s.session_date = p.session_date
                        AND s.time_slot_id = p.time_slot_id
                        AND s.room_id = rm.room_id
                        AND s.status <> 'Cancelled')
);
GO
