using System;

namespace SportCenter.Services
{
    // Quy tắc chung cho việc sửa buổi học: chỉ sửa được buổi chưa bắt đầu.
    public static class SessionRules
    {
        // Buổi đã bắt đầu khi đã tới giờ bắt đầu của khung giờ trong ngày học.
        public static bool HasStarted(DateOnly sessionDate, TimeOnly startTime) =>
            sessionDate.ToDateTime(startTime) <= DateTime.Now;

        // Thứ trong tuần theo cách database lưu (time_slot.day_of_week): Thứ 2 = 2 ... Thứ 7 = 7, Chủ nhật = 8.
        public static byte DbDayOfWeek(DateOnly date) => (byte)(((int)date.DayOfWeek + 6) % 7 + 2);

        // Chỉ buổi đang "Scheduled" và chưa bắt đầu mới được đổi coach/phòng hoặc hủy.
        public static bool CanModify(string status, DateOnly sessionDate, TimeOnly startTime) =>
            status == "Scheduled" && !HasStarted(sessionDate, startTime);
    }
}
