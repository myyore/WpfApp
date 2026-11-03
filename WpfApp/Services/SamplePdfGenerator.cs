using System.IO;
using System.Text;

namespace WpfApp.Services;

public static class SamplePdfGenerator
{
    public static void Create(string filePath, string title, string[] lines)
    {
        var sb = new StringBuilder();
        var offsets = new List<long>();
        sb.Append("%PDF-1.4\n");

        var content = new StringBuilder();
        content.Append("0.8 w\n");
        content.Append("40 40 515 762 re S\n");
        content.Append("40 662 m 555 662 l S\n");
        content.Append("40 200 m 555 200 l S\n");
        content.Append("40 120 m 555 120 l S\n");
        content.Append("BT\n/F1 26 Tf\n72 780 Td\n(").Append(Escape(title)).Append(") Tj\nET\n");
        content.Append("BT\n/F1 14 Tf\n72 700 Td\n18 TL\n");
        foreach (var line in lines)
            content.Append('(').Append(Escape(line)).Append(") Tj T*\n");
        content.Append("ET\n");

        string contentStr = content.ToString();
        int contentLen = Encoding.ASCII.GetByteCount(contentStr);

        offsets.Add(sb.Length);
        sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        offsets.Add(sb.Length);
        sb.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        offsets.Add(sb.Length);
        sb.Append("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] ")
          .Append("/Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>\nendobj\n");

        offsets.Add(sb.Length);
        sb.Append("4 0 obj\n<< /Length ").Append(contentLen).Append(" >>\nstream\n")
          .Append(contentStr).Append("\nendstream\nendobj\n");

        offsets.Add(sb.Length);
        sb.Append("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

        long xrefPos = sb.Length;
        sb.Append("xref\n0 6\n");
        sb.Append("0000000000 65535 f \n");
        foreach (var off in offsets)
            sb.Append(off.ToString("D10")).Append(" 00000 n \n");

        sb.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n")
          .Append(xrefPos).Append("\n%%EOF\n");

        File.WriteAllText(filePath, sb.ToString(), Encoding.ASCII);
    }

    private static string Escape(string s)
        => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}