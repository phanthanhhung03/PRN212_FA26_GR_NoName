USE SportCenterDB;
GO

-- =====================================================
-- 002: Thêm bảng khung giờ (time_slot)
-- =====================================================

-- 1. TIME_SLOT: danh mục khung giờ dùng chung toàn trung tâm
CREATE TABLE time_slot (
    time_slot_id INT PRIMARY KEY IDENTITY(1,1),
    slot_name    NVARCHAR(50) NOT NULL,            -- VD: N'Tối T2 18:00-19:30'
    day_of_week  TINYINT NOT NULL,                 -- 2=Thứ 2 ... 7=Thứ 7, 8=Chủ nhật
    start_time   TIME NOT NULL,
    end_time     TIME NOT NULL,
    CONSTRAINT CK_TimeSlot_Day   CHECK (day_of_week BETWEEN 2 AND 8),
    CONSTRAINT CK_TimeSlot_Time  CHECK (end_time > start_time),
    CONSTRAINT CK_TimeSlot_Hours CHECK (start_time >= '07:00' AND end_time <= '20:00'),
    CONSTRAINT UQ_TimeSlot UNIQUE (day_of_week, start_time, end_time)
);
GO

-- 2. CLASS_TIME_SLOT: một lớp có thể học nhiều khung trong tuần
CREATE TABLE class_time_slot (
    class_id     INT NOT NULL,
    time_slot_id INT NOT NULL,
    CONSTRAINT PK_Class_TimeSlot PRIMARY KEY (class_id, time_slot_id),
    CONSTRAINT FK_ClassTS_Class    FOREIGN KEY (class_id)     REFERENCES class(class_id),
    CONSTRAINT FK_ClassTS_TimeSlot FOREIGN KEY (time_slot_id) REFERENCES time_slot(time_slot_id)
);
GO

-- 3. SCHEDULE: thêm liên kết đến khung mẫu (nullable)
ALTER TABLE schedule ADD time_slot_id INT NULL;
GO

ALTER TABLE schedule ADD CONSTRAINT FK_Schedule_TimeSlot
    FOREIGN KEY (time_slot_id) REFERENCES time_slot(time_slot_id);
GO

-- 4. SCHEMA_MIGRATIONS: ghi nhận file đã chạy (tạo nếu chưa có)
IF OBJECT_ID('schema_migrations', 'U') IS NULL
BEGIN
    CREATE TABLE schema_migrations (
        file_name  VARCHAR(255) PRIMARY KEY,
        applied_at DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

INSERT INTO schema_migrations (file_name) VALUES ('002_add_time_slot.sql');
GO
