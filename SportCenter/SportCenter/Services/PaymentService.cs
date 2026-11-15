using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportCenter.Dtos;
using SportCenter.Entities;
using System.Data;

namespace SportCenter.Services;

/// <summary>UC06: tạo khoản Pending có QR payload và mô phỏng kết quả thanh toán.</summary>
public sealed class PaymentService
{
    public async Task<PaymentCheckoutDto> CreatePendingPaymentAsync(int enrollmentId)
    {
        var role = CurrentUser.Value?.Role.RoleName;
        if (role is not ("Member" or "Staff"))
            throw new UnauthorizedAccessException("Không có quyền tạo thanh toán.");

        using var db = new SportCenterDbContext();
        var conn = (SqlConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_create_payment";
        cmd.Parameters.Add("@enrollment_id", SqlDbType.Int).Value = enrollmentId;
        cmd.Parameters.Add("@created_by", SqlDbType.Int).Value = CurrentUser.Value!.UserId;
        var output = new SqlParameter("@payment_id", SqlDbType.Int) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(output);

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(ex.Number switch
            {
                51007 => "Không tìm thấy đăng ký hợp lệ để tạo thanh toán.",
                51008 => "Bạn không có quyền tạo thanh toán cho đăng ký này.",
                _ => "Không tạo được khoản thanh toán. Vui lòng thử lại."
            }, ex);
        }

        int paymentId = (int)output.Value;
        return await db.Payments.AsNoTracking()
            .Where(p => p.PaymentId == paymentId)
            .Select(p => new PaymentCheckoutDto(p.PaymentId, p.EnrollmentId, p.OrderCode,
                p.Amount, p.Description, p.QrCode, p.CheckoutUrl, p.Status, p.CreatedAt))
            .FirstAsync();
    }

    public async Task ConfirmMockPaymentAsync(int paymentId, bool success)
    {
        if (CurrentUser.Value?.Role.RoleName is not ("Member" or "Staff"))
            throw new UnauthorizedAccessException("Không có quyền xác nhận thanh toán.");

        using var db = new SportCenterDbContext();
        var conn = (SqlConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.sp_mock_confirm_payment";
        cmd.Parameters.Add("@payment_id", SqlDbType.Int).Value = paymentId;
        cmd.Parameters.Add("@actor_id", SqlDbType.Int).Value = CurrentUser.Value!.UserId;
        cmd.Parameters.Add("@is_success", SqlDbType.Bit).Value = success;

        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(ex.Number switch
            {
                51009 => "Không tìm thấy giao dịch.",
                51010 => "Bạn không có quyền xác nhận giao dịch này.",
                51011 => "Giao dịch không còn ở trạng thái chờ thanh toán.",
                _ => "Không cập nhật được trạng thái thanh toán. Vui lòng thử lại."
            }, ex);
        }
    }
}
