USE SportCenterDB;
GO

/*
 TV2 - UC05, UC06, UC10
 All write operations go through these procedures.
 SYS01: Pending + Active enrollments reserve seats; the class row is locked
        with UPDLOCK/HOLDLOCK before counting/inserting.
 SYS02: the unique constraint UQ_Class_Member is a second line of defense.
 Pending older than 5 minutes expires (sp_expire_pending_enrollments);
 a Member can cancel before the start date (sp_cancel_enrollment).
*/

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'PaymentOrderCodeSeq' AND schema_id = SCHEMA_ID(N'dbo'))
    EXEC(N'CREATE SEQUENCE dbo.PaymentOrderCodeSeq AS BIGINT START WITH 9000000001 INCREMENT BY 1;');
GO

CREATE OR ALTER PROCEDURE dbo.sp_enroll_member
    @class_id INT,
    @member_id INT,
    @registered_by INT = NULL,
    @enrollment_id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @enrollment_id = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.[user] u
            JOIN dbo.[role] r ON r.role_id = u.role_id
            WHERE u.user_id = @member_id AND r.role_name = N'Member' AND u.status = 'Active')
            THROW 51001, N'Tài khoản học viên không tồn tại hoặc không hoạt động.', 1;

        IF @registered_by IS NULL
        BEGIN
            -- UC05: self-enrollment. A Member can only enroll themselves.
            IF NOT EXISTS (SELECT 1 FROM dbo.[user] WHERE user_id = @member_id AND status = 'Active')
                THROW 51002, N'Tài khoản không hợp lệ để tự ghi danh.', 1;
        END
        ELSE IF NOT EXISTS (
            SELECT 1 FROM dbo.[user] u
            JOIN dbo.[role] r ON r.role_id = u.role_id
            WHERE u.user_id = @registered_by AND r.role_name = N'Staff' AND u.status = 'Active')
            THROW 51003, N'Chỉ Staff đang hoạt động mới được ghi danh hộ.', 1;

        DECLARE @max_capacity INT, @class_status VARCHAR(20);
        SELECT @max_capacity = max_capacity, @class_status = status
        FROM dbo.[class] WITH (UPDLOCK, HOLDLOCK)
        WHERE class_id = @class_id;

        IF @max_capacity IS NULL OR @class_status <> 'Enrolling'
            THROW 51004, N'Lớp không tồn tại hoặc không còn tuyển sinh.', 1;

        DECLARE @existing_enrollment_id INT, @existing_status VARCHAR(20);
        SELECT @existing_enrollment_id = enrollment_id, @existing_status = status
        FROM dbo.class_members WITH (UPDLOCK, HOLDLOCK)
        WHERE class_id = @class_id AND member_id = @member_id;

        IF @existing_enrollment_id IS NOT NULL AND @existing_status <> 'Dropped'
            THROW 51005, N'Học viên đã có đăng ký đối với lớp này.', 1;

        DECLARE @reserved INT;
        SELECT @reserved = COUNT(*)
        FROM dbo.class_members WITH (UPDLOCK, HOLDLOCK)
        WHERE class_id = @class_id AND status IN ('Pending', 'Active');

        IF @reserved >= @max_capacity
            THROW 51006, N'Lớp đã đủ sĩ số, không thể ghi danh thêm.', 1;

        IF @existing_enrollment_id IS NOT NULL
        BEGIN
            -- A failed/cancelled previous attempt may be retried without violating UQ_Class_Member.
            UPDATE dbo.class_members
            SET status = 'Pending', registered_by = @registered_by, enrolled_at = GETDATE()
            WHERE enrollment_id = @existing_enrollment_id;
            SET @enrollment_id = @existing_enrollment_id;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.class_members (class_id, member_id, registered_by, status)
            VALUES (@class_id, @member_id, @registered_by, 'Pending');
            SET @enrollment_id = CONVERT(INT, SCOPE_IDENTITY());
        END;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_create_payment
    @enrollment_id INT,
    @created_by INT = NULL,
    @payment_id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @payment_id = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @member_id INT, @registered_by INT, @amount DECIMAL(18,2), @enrollment_status VARCHAR(20);
        SELECT @member_id = cm.member_id,
               @registered_by = cm.registered_by,
               @amount = co.tuition_fee,
               @enrollment_status = cm.status
        FROM dbo.class_members cm WITH (UPDLOCK, HOLDLOCK)
        JOIN dbo.[class] c ON c.class_id = cm.class_id
        JOIN dbo.course co ON co.course_id = c.course_id
        WHERE cm.enrollment_id = @enrollment_id;

        IF @member_id IS NULL OR @enrollment_status <> 'Pending'
            THROW 51007, N'Không tìm thấy đăng ký hợp lệ để tạo thanh toán.', 1;

        IF @created_by IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.[user] u
            JOIN dbo.[role] r ON r.role_id = u.role_id
            WHERE u.user_id = @created_by AND u.status = 'Active'
              AND ((r.role_name = N'Member' AND u.user_id = @member_id)
                OR (r.role_name = N'Staff' AND @registered_by = u.user_id)))
            THROW 51008, N'Bạn không có quyền tạo thanh toán cho đăng ký này.', 1;

        SELECT TOP (1) @payment_id = payment_id
        FROM dbo.payment WITH (UPDLOCK, HOLDLOCK)
        WHERE enrollment_id = @enrollment_id AND status = 'Pending'
        ORDER BY created_at DESC;

        IF @payment_id IS NULL
        BEGIN
            DECLARE @order_code BIGINT = NEXT VALUE FOR dbo.PaymentOrderCodeSeq;
            DECLARE @description VARCHAR(25) = LEFT(CONCAT('SC-', @enrollment_id, '-', @member_id), 25);
            DECLARE @qr NVARCHAR(MAX) = CONCAT(
                N'SPORTCENTER|ORDER=', @order_code,
                N'|ENROLLMENT=', @enrollment_id,
                N'|AMOUNT=', CONVERT(VARCHAR(40), CONVERT(DECIMAL(18,0), @amount)));

            INSERT INTO dbo.payment (enrollment_id, order_code, amount, description,
                                     qr_code, checkout_url, status, created_by)
            VALUES (@enrollment_id, @order_code, @amount, @description,
                    @qr, CONCAT(N'mock://sportcenter/pay/', @order_code), 'Pending', @created_by);
            SET @payment_id = CONVERT(INT, SCOPE_IDENTITY());
        END;

        COMMIT TRANSACTION;

        SELECT p.payment_id AS PaymentId, p.enrollment_id AS EnrollmentId,
               p.order_code AS OrderCode, p.amount AS Amount,
               p.description AS Description, p.qr_code AS QrCode,
               p.checkout_url AS CheckoutUrl, p.status AS Status,
               p.created_at AS CreatedAt
        FROM dbo.payment p WHERE p.payment_id = @payment_id;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_mock_confirm_payment
    @payment_id INT,
    @actor_id INT,
    @is_success BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @member_id INT, @registered_by INT, @status VARCHAR(20), @enrollment_id INT;
        SELECT @member_id = cm.member_id, @registered_by = cm.registered_by,
               @status = p.status, @enrollment_id = p.enrollment_id
        FROM dbo.payment p WITH (UPDLOCK, HOLDLOCK)
        JOIN dbo.class_members cm WITH (UPDLOCK, HOLDLOCK) ON cm.enrollment_id = p.enrollment_id
        WHERE p.payment_id = @payment_id;

        IF @member_id IS NULL
            THROW 51009, N'Không tìm thấy giao dịch.', 1;

        IF NOT EXISTS (
            SELECT 1 FROM dbo.[user] u
            JOIN dbo.[role] r ON r.role_id = u.role_id
            WHERE u.user_id = @actor_id AND u.status = 'Active'
              AND ((r.role_name = N'Member' AND u.user_id = @member_id)
                OR (r.role_name = N'Staff' AND @registered_by = u.user_id)))
            THROW 51010, N'Bạn không có quyền xác nhận giao dịch này.', 1;

        IF @status <> 'Pending'
            THROW 51011, N'Giao dịch không còn ở trạng thái chờ thanh toán.', 1;

        IF @is_success = 1
        BEGIN
            UPDATE dbo.payment SET status = 'Paid', paid_at = GETDATE()
            WHERE payment_id = @payment_id;
            UPDATE dbo.class_members SET status = 'Active'
            WHERE enrollment_id = @enrollment_id AND status = 'Pending';
        END
        ELSE
        BEGIN
            UPDATE dbo.payment SET status = 'Cancelled'
            WHERE payment_id = @payment_id;
            UPDATE dbo.class_members SET status = 'Dropped'
            WHERE enrollment_id = @enrollment_id AND status = 'Pending';
            -- A later enrollment attempt can reactivate this same row as Pending.
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO


-- UC05/UC06: Pending quá hạn thì hết hạn; học viên hủy đăng ký trước ngày khai giảng.

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
