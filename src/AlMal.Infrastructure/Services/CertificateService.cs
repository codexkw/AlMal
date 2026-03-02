using AlMal.Application.Interfaces;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AlMal.Infrastructure.Services;

public class CertificateService : ICertificateService
{
    private readonly AlMalDbContext _context;

    public CertificateService(AlMalDbContext context)
    {
        _context = context;
    }

    public async Task<CertificateDetail?> GetCertificateAsync(int certificateId, string userId, CancellationToken ct = default)
    {
        return await _context.Certificates
            .AsNoTracking()
            .Where(c => c.Id == certificateId && c.UserId == userId)
            .Select(c => new CertificateDetail
            {
                Id = c.Id,
                CourseTitleAr = c.Course.TitleAr,
                UserName = c.User.DisplayName,
                CompletionDate = c.IssuedAt,
                CertificateNumber = c.CertificateNumber,
                PdfUrl = c.PdfUrl
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<CertificateDetail>> GetUserCertificatesAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Certificates
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new CertificateDetail
            {
                Id = c.Id,
                CourseTitleAr = c.Course.TitleAr,
                UserName = c.User.DisplayName,
                CompletionDate = c.IssuedAt,
                CertificateNumber = c.CertificateNumber,
                PdfUrl = c.PdfUrl
            })
            .ToListAsync(ct);
    }

    public async Task<CertificateVerifyResult> VerifyCertificateAsync(string certificateNumber, CancellationToken ct = default)
    {
        var cert = await _context.Certificates
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber, ct);

        if (cert == null)
            return new CertificateVerifyResult { Valid = false };

        return new CertificateVerifyResult
        {
            Valid = true,
            IssueDate = cert.IssuedAt,
            CourseTitle = cert.Course.TitleAr,
            UserName = cert.User.DisplayName
        };
    }

    public async Task<byte[]> GenerateCertificatePdfAsync(int certificateId, CancellationToken ct = default)
    {
        var cert = await _context.Certificates
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == certificateId, ct);

        if (cert == null)
            return [];

        var pdfBytes = GeneratePdf(cert.User.DisplayName, cert.Course.TitleAr, cert.IssuedAt, cert.CertificateNumber);
        return pdfBytes;
    }

    private static byte[] GeneratePdf(string userName, string courseTitle, DateTime issuedAt, string certNumber)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(50);
                page.MarginVertical(40);
                page.DefaultTextStyle(x => x.FontSize(14));

                page.Content().Border(3).BorderColor(Colors.Purple.Darken3).Padding(30).Column(col =>
                {
                    col.Spacing(15);

                    // Header
                    col.Item().AlignCenter().Text("قناة المال")
                        .FontSize(36).Bold().FontColor(Colors.Purple.Darken3);

                    col.Item().AlignCenter().Text("Al-Mal Channel")
                        .FontSize(16).FontColor(Colors.Grey.Darken1);

                    col.Item().PaddingVertical(5).LineHorizontal(2).LineColor(Colors.Orange.Darken1);

                    // Title
                    col.Item().AlignCenter().Text("شهادة إتمام")
                        .FontSize(28).Bold().FontColor(Colors.Purple.Darken3);

                    col.Item().AlignCenter().Text("Certificate of Completion")
                        .FontSize(14).FontColor(Colors.Grey.Darken1);

                    col.Item().PaddingVertical(10);

                    // User name
                    col.Item().AlignCenter().Text("يُشهد بأن")
                        .FontSize(14).FontColor(Colors.Grey.Darken2);

                    col.Item().AlignCenter().Text(userName)
                        .FontSize(26).Bold().FontColor(Colors.Black);

                    col.Item().PaddingVertical(5);

                    // Course
                    col.Item().AlignCenter().Text("قد أتم بنجاح دورة")
                        .FontSize(14).FontColor(Colors.Grey.Darken2);

                    col.Item().AlignCenter().Text(courseTitle)
                        .FontSize(22).Bold().FontColor(Colors.Orange.Darken1);

                    col.Item().PaddingVertical(10);

                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Footer info
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"تاريخ الإصدار: {issuedAt:yyyy-MM-dd}")
                                .FontSize(11).FontColor(Colors.Grey.Darken2);
                        });

                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Text("هذا تحليل تعليمي وليس نصيحة استثمارية")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                        });

                        row.RelativeItem().AlignLeft().Column(c =>
                        {
                            c.Item().Text($"رقم الشهادة: {certNumber}")
                                .FontSize(11).FontColor(Colors.Grey.Darken2);
                        });
                    });
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }
}
