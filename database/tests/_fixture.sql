-- Shared fixture: included with :r right after BEGIN TRAN. Direct inserts only.
DECLARE @r_mgr INT = (SELECT role_id FROM [role] WHERE role_name = N'Manager');
DECLARE @r_coach INT = (SELECT role_id FROM [role] WHERE role_name = N'Coach');
DECLARE @r_member INT = (SELECT role_id FROM [role] WHERE role_name = N'Member');

DECLARE @mgr INT, @sport INT, @other_sport INT, @course INT;
DECLARE @coachA INT, @coachB INT, @coachX INT;
DECLARE @roomS INT, @roomL INT, @roomX INT;
DECLARE @slotMon INT, @slotWed INT, @slotMonPm INT, @member INT;

INSERT INTO [user] (role_id, full_name, email, password_hash)
VALUES (@r_mgr, N'fx_manager', 'fx_manager@test.local', 'x');
SET @mgr = SCOPE_IDENTITY();

INSERT INTO sport (sport_name) VALUES (N'fx_sport');
SET @sport = SCOPE_IDENTITY();
INSERT INTO sport (sport_name) VALUES (N'fx_other_sport');
SET @other_sport = SCOPE_IDENTITY();

INSERT INTO course (sport_id, course_name, total_sessions, tuition_fee)
VALUES (@sport, N'fx_course', 4, 1000000);
SET @course = SCOPE_IDENTITY();

INSERT INTO [user] (role_id, full_name, email, password_hash, sport_id, created_by)
VALUES (@r_coach, N'fx_coachA', 'fx_coachA@test.local', 'x', @sport, @mgr);
SET @coachA = SCOPE_IDENTITY();
INSERT INTO [user] (role_id, full_name, email, password_hash, sport_id, created_by)
VALUES (@r_coach, N'fx_coachB', 'fx_coachB@test.local', 'x', @sport, @mgr);
SET @coachB = SCOPE_IDENTITY();
INSERT INTO [user] (role_id, full_name, email, password_hash, sport_id, created_by)
VALUES (@r_coach, N'fx_coachX', 'fx_coachX@test.local', 'x', @other_sport, @mgr);
SET @coachX = SCOPE_IDENTITY();

INSERT INTO room (sport_id, room_name, capacity) VALUES (@sport, N'fx_roomS', 10);
SET @roomS = SCOPE_IDENTITY();
INSERT INTO room (sport_id, room_name, capacity) VALUES (@sport, N'fx_roomL', 30);
SET @roomL = SCOPE_IDENTITY();
INSERT INTO room (sport_id, room_name, capacity) VALUES (@other_sport, N'fx_roomX', 30);
SET @roomX = SCOPE_IDENTITY();

SET @slotMon   = (SELECT time_slot_id FROM time_slot WHERE day_of_week = 2 AND start_time = '07:00');
SET @slotWed   = (SELECT time_slot_id FROM time_slot WHERE day_of_week = 4 AND start_time = '07:00');
SET @slotMonPm = (SELECT time_slot_id FROM time_slot WHERE day_of_week = 2 AND start_time = '15:00');

INSERT INTO [user] (role_id, full_name, email, password_hash)
VALUES (@r_member, N'fx_member', 'fx_member@test.local', 'x');
SET @member = SCOPE_IDENTITY();
