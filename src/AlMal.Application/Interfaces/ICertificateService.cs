namespace AlMal.Application.Interfaces;

public class CertificateDetail
{
    public int Id { get; set; }
    public string CourseTitleAr { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public DateTime CompletionDate { get; set; }
    public string CertificateNumber { get; set; } = null!;
    public string? PdfUrl { get; set; }
}

public class CertificateVerifyResult
{
    public bool Valid { get; set; }
    public DateTime? IssueDate { get; set; }
    public string? CourseTitle { get; set; }
    public string? UserName { get; set; }
}

public interface ICertificateService
{
    Task<CertificateDetail?> GetCertificateAsync(int certificateId, string userId, CancellationToken ct = default);
    Task<List<CertificateDetail>> GetUserCertificatesAsync(string userId, CancellationToken ct = default);
    Task<CertificateVerifyResult> VerifyCertificateAsync(string certificateNumber, CancellationToken ct = default);
    Task<byte[]> GenerateCertificatePdfAsync(int certificateId, CancellationToken ct = default);
}
