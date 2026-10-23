using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportCenter.Dtos
{
    public record ClassListItemDto
        (
        int ClassId, string ClassName,
        string SportName, string CourseName,string? CoachName,
        string RoomName, DateOnly StartDate, DateOnly? EndDate,
        int MaxCapacity, int ActiveMembers, string Status
        );
}
