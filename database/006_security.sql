USE SportCenterDB;
GO

-- Application principal: no direct writes to core tables; writes go through procedures.
-- Replace WITHOUT LOGIN with the real application login when deploying.
CREATE USER app_user WITHOUT LOGIN;
GO

DENY INSERT, UPDATE, DELETE ON dbo.[user]           TO app_user;
DENY INSERT, UPDATE, DELETE ON dbo.payment          TO app_user;
DENY INSERT, UPDATE, DELETE ON dbo.class            TO app_user;
DENY INSERT, UPDATE, DELETE ON dbo.class_time_slot  TO app_user;
DENY INSERT, UPDATE, DELETE ON dbo.session          TO app_user;

-- Reads: schema-wide (covers tables, views and table-valued functions fn_*).
GRANT SELECT ON SCHEMA::dbo TO app_user;

-- EXECUTE per public procedure by name; internal helper usp_check_assignment stays unreachable.
GRANT EXECUTE ON dbo.sp_register_member     TO app_user;
GRANT EXECUTE ON dbo.sp_create_staff_coach  TO app_user;
GRANT EXECUTE ON dbo.sp_create_class        TO app_user;
GRANT EXECUTE ON dbo.sp_reassign_session    TO app_user;
GRANT EXECUTE ON dbo.sp_cancel_session      TO app_user;
GRANT EXECUTE ON dbo.sp_add_makeup_session  TO app_user;
GRANT EXECUTE ON TYPE::dbo.IdList           TO app_user;
DENY  EXECUTE ON dbo.usp_check_assignment   TO app_user;
GO
