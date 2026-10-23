USE SportCenterDB;
SET NOCOUNT ON;
-- Read-only checks on the seeded database (no transaction, nothing to roll back).
-- Run with sqlcmd -f 65001 (UTF-8) so Vietnamese literals compare correctly.

DECLARE @n INT;

-- Manager
SET @n = (SELECT COUNT(*) FROM [user] u JOIN [role] r ON r.role_id = u.role_id WHERE r.role_name = N'Manager');
IF ISNULL(@n, 0) <> 1 THROW 60000, N'FAIL: exactly 1 Manager', 1;
IF NOT EXISTS (SELECT 1 FROM [user] WHERE email = 'manager@sportcenter.local')
    THROW 60000, N'FAIL: manager@sportcenter.local missing', 1;

-- Counts
IF ISNULL((SELECT COUNT(*) FROM sport), 0) <> 5 THROW 60000, N'FAIL: 5 sports', 1;
IF ISNULL((SELECT COUNT(*) FROM course), 0) <> 10 THROW 60000, N'FAIL: 10 courses', 1;
IF ISNULL((SELECT COUNT(*) FROM [user] u JOIN [role] r ON r.role_id = u.role_id WHERE r.role_name = N'Staff'), 0) < 2
    THROW 60000, N'FAIL: >= 2 Staff', 1;
IF ISNULL((SELECT COUNT(*) FROM [user] u JOIN [role] r ON r.role_id = u.role_id WHERE r.role_name = N'Member'), 0) < 5
    THROW 60000, N'FAIL: >= 5 Member', 1;
IF ISNULL((SELECT COUNT(*) FROM class), 0) < 8 THROW 60000, N'FAIL: >= 8 classes', 1;

-- Fake BCrypt-format hash
IF EXISTS (SELECT 1 FROM [user] WHERE password_hash NOT LIKE '$2a$11$%' OR LEN(password_hash) <> 60)
    THROW 60000, N'FAIL: every password_hash must be a 60-char $2a$11$ sample', 1;

-- Members have birth_date and gender
IF EXISTS (SELECT 1 FROM [user] u JOIN [role] r ON r.role_id = u.role_id
           WHERE r.role_name = N'Member' AND (u.birth_date IS NULL OR u.gender IS NULL))
    THROW 60000, N'FAIL: members need birth_date and gender', 1;

-- Vietnamese round trip
IF NOT EXISTS (SELECT 1 FROM [user] WHERE full_name = N'Trần Quốc Bảo')
    THROW 60000, N'FAIL: Vietnamese name did not round-trip (run sqlcmd with -f 65001)', 1;
IF NOT EXISTS (SELECT 1 FROM sport WHERE sport_name = N'Bóng đá')
    THROW 60000, N'FAIL: Vietnamese sport name did not round-trip', 1;

-- Each sport: >= 2 Coaches with sport_id, >= 2 rooms with different capacities
IF EXISTS (
    SELECT 1 FROM sport s
    WHERE (SELECT COUNT(*) FROM [user] u JOIN [role] r ON r.role_id = u.role_id
           WHERE r.role_name = N'Coach' AND u.sport_id = s.sport_id) < 2
       OR (SELECT COUNT(*) FROM room m WHERE m.sport_id = s.sport_id) < 2
       OR (SELECT COUNT(DISTINCT m.capacity) FROM room m WHERE m.sport_id = s.sport_id) < 2)
    THROW 60000, N'FAIL: each sport needs >= 2 coaches and >= 2 rooms with different capacity', 1;
IF EXISTS (SELECT 1 FROM [user] u JOIN [role] r ON r.role_id = u.role_id
           WHERE r.role_name = N'Coach' AND u.sport_id IS NULL)
    THROW 60000, N'FAIL: Coach without sport_id', 1;

-- Non-cancelled classes: valid sessions = total_sessions
IF EXISTS (
    SELECT 1 FROM class c JOIN course co ON co.course_id = c.course_id
    WHERE c.status <> 'Cancelled'
      AND (SELECT COUNT(*) FROM session s WHERE s.class_id = c.class_id AND s.status <> 'Cancelled') <> co.total_sessions)
    THROW 60000, N'FAIL: valid sessions <> total_sessions', 1;

-- No double booking (room / coach) among non-cancelled sessions
IF EXISTS (SELECT room_id, session_date, time_slot_id FROM session WHERE status <> 'Cancelled'
           GROUP BY room_id, session_date, time_slot_id HAVING COUNT(*) > 1)
    THROW 60000, N'FAIL: duplicate room booking', 1;
IF EXISTS (SELECT coach_id, session_date, time_slot_id FROM session WHERE status <> 'Cancelled'
           GROUP BY coach_id, session_date, time_slot_id HAVING COUNT(*) > 1)
    THROW 60000, N'FAIL: duplicate coach booking', 1;

-- Reassigned session (coach or room differs from class), not cancelled
IF NOT EXISTS (SELECT 1 FROM session s JOIN class c ON c.class_id = s.class_id
               WHERE s.status <> 'Cancelled' AND (s.coach_id <> c.coach_id OR s.room_id <> c.room_id))
    THROW 60000, N'FAIL: need a reassigned session', 1;

-- Cancelled session with a later-numbered makeup in the same class
IF NOT EXISTS (SELECT 1 FROM session x JOIN session m ON m.class_id = x.class_id
                    AND m.session_no > x.session_no AND m.status <> 'Cancelled'
               WHERE x.status = 'Cancelled')
    THROW 60000, N'FAIL: need a cancelled session with makeup', 1;

-- >= 2 Completed sessions with attendance
SET @n = (SELECT COUNT(*) FROM session s WHERE s.status = 'Completed'
          AND EXISTS (SELECT 1 FROM attendance a WHERE a.session_id = s.session_id));
IF ISNULL(@n, 0) < 2 THROW 60000, N'FAIL: >= 2 Completed sessions with attendance', 1;

-- Every session before 2026-10-20 is Completed and none after is
IF EXISTS (SELECT 1 FROM session WHERE session_date < '2026-10-20' AND status = 'Scheduled')
    THROW 60000, N'FAIL: past session still Scheduled', 1;
IF EXISTS (SELECT 1 FROM session WHERE session_date >= '2026-10-20' AND status = 'Completed')
    THROW 60000, N'FAIL: future session Completed', 1;

-- Attendance only for Active enrolled members of the session class
IF EXISTS (SELECT 1 FROM attendance a JOIN session s ON s.session_id = a.session_id
           WHERE NOT EXISTS (SELECT 1 FROM class_members cm
                             WHERE cm.class_id = s.class_id AND cm.member_id = a.member_id AND cm.status = 'Active'))
    THROW 60000, N'FAIL: attendance for non-Active member', 1;

-- Full class, Pending enrollment, capacity never exceeded
IF NOT EXISTS (SELECT 1 FROM class c
               WHERE (SELECT COUNT(*) FROM class_members cm WHERE cm.class_id = c.class_id AND cm.status = 'Active') = c.max_capacity)
    THROW 60000, N'FAIL: need a full class', 1;
IF EXISTS (SELECT 1 FROM class c
           WHERE (SELECT COUNT(*) FROM class_members cm WHERE cm.class_id = c.class_id AND cm.status = 'Active') > c.max_capacity)
    THROW 60000, N'FAIL: class over capacity', 1;
IF NOT EXISTS (SELECT 1 FROM class_members WHERE status = 'Pending')
    THROW 60000, N'FAIL: need a Pending enrollment', 1;

-- Payments
IF NOT EXISTS (SELECT 1 FROM payment WHERE status = 'Paid' AND paid_at IS NOT NULL)
    THROW 60000, N'FAIL: need a Paid payment', 1;
IF NOT EXISTS (SELECT 1 FROM payment WHERE status = 'Pending')
    THROW 60000, N'FAIL: need a Pending payment', 1;

-- Class room/coach match the course sport; max_capacity <= room capacity
IF EXISTS (SELECT 1 FROM class c
           JOIN course co ON co.course_id = c.course_id
           JOIN room m ON m.room_id = c.room_id
           JOIN [user] u ON u.user_id = c.coach_id
           WHERE m.sport_id <> co.sport_id OR u.sport_id <> co.sport_id OR c.max_capacity > m.capacity)
    THROW 60000, N'FAIL: class room/coach sport or capacity mismatch', 1;

-- Start dates within range and on the weekday of a class slot
IF EXISTS (SELECT 1 FROM class WHERE start_date < '2026-10-05' OR start_date > '2026-11-09')
    THROW 60000, N'FAIL: start_date out of range', 1;
IF EXISTS (SELECT 1 FROM class c
           WHERE NOT EXISTS (SELECT 1 FROM class_time_slot cts JOIN time_slot ts ON ts.time_slot_id = cts.time_slot_id
                             WHERE cts.class_id = c.class_id
                               AND ts.day_of_week = (DATEDIFF(DAY, '19000101', c.start_date) % 7) + 2))
    THROW 60000, N'FAIL: start_date not on a slot weekday', 1;

-- No member has two non-Dropped enrollments with non-Cancelled sessions on the same date + slot
IF EXISTS (
    SELECT 1
    FROM class_members cm1
    JOIN class_members cm2 ON cm2.member_id = cm1.member_id AND cm2.class_id > cm1.class_id
    JOIN session s1 ON s1.class_id = cm1.class_id AND s1.status <> 'Cancelled'
    JOIN session s2 ON s2.class_id = cm2.class_id AND s2.status <> 'Cancelled'
                   AND s2.session_date = s1.session_date AND s2.time_slot_id = s1.time_slot_id
    WHERE cm1.status <> 'Dropped' AND cm2.status <> 'Dropped')
    THROW 60000, N'FAIL: a member has overlapping sessions across enrollments', 1;

PRINT 'PASS t07 seed';
GO
