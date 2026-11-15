namespace SportCenter.Dtos;

public sealed record PaymentCheckoutDto(
    int PaymentId,
    int EnrollmentId,
    long OrderCode,
    decimal Amount,
    string Description,
    string? QrCode,
    string? CheckoutUrl,
    string Status,
    DateTime CreatedAt);

public sealed record MemberLookupDto(int UserId, string FullName, string Email, string? Phone);

public sealed record EnrollmentResultDto(int EnrollmentId, int ClassId, string ClassName, string MemberName, string Status);
