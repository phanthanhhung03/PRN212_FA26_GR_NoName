using System;

namespace SportCenter.Services
{
    // Quy tắc chung cho việc sửa buổi học: chỉ sửa được buổi chưa bắt đầu.
    public static class SessionRules
    {
        // Buổi đã bắt đầu khi đã tới giờ bắt đầu của khung giờ trong ngày học.
        public static bool HasStarted(DateOnly sessionDate, TimeOnly startTime) =>
            sessionDate.ToDateTime(startTime) <= DateTime.Now;

        // Chỉ buổi đang "Scheduled" và chưa bắt đầu mới được đổi coach/phòng hoặc hủy.
        public static bool CanModify(string status, DateOnly sessionDate, TimeOnly startTime) =>
            status == "Scheduled" && !HasStarted(sessionDate, startTime);
    }
}
