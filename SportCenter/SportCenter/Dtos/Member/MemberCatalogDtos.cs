using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportCenter.Dtos
{
    // Một khóa học đang mở
    public record MemberCourseDto(
         int CourseId, string CourseName, string? Level, int TotalSessions, decimal TuitionFee);

    // Một môn đang hoạt động kèm các khóa đang hoạt động của môn đó.
    public record MemberSportDto(
        int SportId, string SportName, string? Description, List<MemberCourseDto> Courses);
}
