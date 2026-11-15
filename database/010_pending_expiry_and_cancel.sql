USE SportCenterDB;
GO

/*
 Bổ sung cho 009 (chạy lại được):
 - sp_expire_pending_enrollments: đăng ký Pending quá @minutes phút chưa thanh toán
   thì payment = Expired, class_members = Dropped (trả chỗ).
 - sp_cancel_enrollment (UC06): học viên hủy đăng ký của mình trước ngày khai giảng.
*/

CREATE OR ALTER PROCEDURE dbo.sp_expire_pending_enrollments
    @minutes INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @cutoff DATETIME = DATEADD(MINUTE, -@minutes, GETDATE());

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.payment
        SET status = 'Expired'
        WHERE status = 'Pending' AND created_at < @cutoff;

        UPDATE dbo.class_members
        SET status = 'Dropped'
        WHERE status = 'Pending' AND enrolled_at < @cutoff;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_cancel_enrollment
    @class_id INT,
    @member_id INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @enrollment_id INT;
        SELECT @enrollment_id = enrollment_id
        FROM dbo.class_members WITH (UPDLOCK, HOLDLOCK)
        WHERE class_id = @class_id AND member_id = @member_id
          AND status IN ('Pending', 'Active');

        IF @enrollment_id IS NULL
            THROW 51012, N'Bạn không có đăng ký nào đang hiệu lực ở lớp này.', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.[class]
            WHERE class_id = @class_id AND status = 'Enrolling'
              AND start_date > CAST(GETDATE() AS DATE))
            THROW 51013, N'Lớp đã khai giảng, không thể hủy đăng ký.', 1;

        UPDATE dbo.class_members
        SET status = 'Dropped'
        WHERE enrollment_id = @enrollment_id;

        -- Khoản chưa trả thì hủy. Khoản đã Paid giữ nguyên: hoàn tiền xử lý ngoài hệ thống.
        UPDATE dbo.payment
        SET status = 'Cancelled'
        WHERE enrollment_id = @enrollment_id AND status = 'Pending';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
