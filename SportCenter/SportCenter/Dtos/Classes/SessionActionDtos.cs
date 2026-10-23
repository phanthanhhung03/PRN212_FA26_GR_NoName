using System;
using System.Collections.Generic;

namespace SportCenter.Dtos
{
    // Dữ liệu cho hộp thoại "Đổi coach / phòng" của một buổi học.
    public record ReassignOptionsDto(
        int SessionId, int SessionNo, DateOnly SessionDate, byte DayOfWeek, TimeOnly StartTime, TimeOnly EndTime,
        int CurrentCoachId, int CurrentRoomId,
        List<CoachChoiceDto> Coaches, List<RoomChoiceDto> Rooms);

    // Kết quả hủy một buổi: nếu xếp được buổi bù tự động thì có thông tin buổi bù, ngược lại cả ba là null.
    public record CancelSessionResultDto(int? MakeupSessionNo, DateOnly? MakeupDate, byte? MakeupDayOfWeek);

    // Thông tin lớp cho hộp thoại "Xếp buổi bù": MissingSessions = số buổi còn thiếu so với khóa học.
    public record MakeupContextDto(
        int ClassId, string ClassName, DateOnly StartDate, int MissingSessions, int NextSessionNo,
        int? DefaultCoachId, int DefaultRoomId);

    // Coach và phòng có thể chọn cho một ngày và khung giờ cụ thể.
    public record MakeupChoicesDto(List<CoachChoiceDto> Coaches, List<RoomChoiceDto> Rooms);

    // Một coach để chọn: IsBusy = đã có buổi khác đúng ngày và khung giờ này.
    public record CoachChoiceDto(int UserId, string FullName, bool IsBusy, bool IsCurrent)
    {
        public bool IsSelectable => !IsBusy || IsCurrent;
        public string Note => IsCurrent ? "hiện tại" : IsBusy ? "đang bận" : "";
        public string NoteSuffix => Note == "" ? "" : " · " + Note;
    }

    // Một phòng để chọn: TooSmall = sức chứa nhỏ hơn sĩ số tối đa của lớp.
    public record RoomChoiceDto(int RoomId, string RoomName, int Capacity, bool IsBusy, bool TooSmall, bool IsCurrent)
    {
        public bool IsSelectable => IsCurrent || (!IsBusy && !TooSmall);
        public string Note => IsCurrent ? "hiện tại" : IsBusy ? "đang bận" : TooSmall ? "không đủ chỗ" : "";
        public string NoteSuffix => Note == "" ? "" : " · " + Note;
    }
}
