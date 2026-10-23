-- Rebuilds SportCenterDB from scratch (drops the old one) and loads the seed.
-- Seed contains Vietnamese text: run with UTF-8 code page (-f 65001):
--   sqlcmd -S . -E -I -b -f 65001 -i build.sql
:r 000_create_database.sql
:r 001_schema.sql

:r 002_functions.sql

:r 003_account_procedures.sql

:r 004_class_procedures.sql

:r 005_session_procedures.sql

:r 006_security.sql

:r 007_seed_data.sql

:r 008_busy_slots.sql
