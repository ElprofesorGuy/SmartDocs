using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace SmartDocs.Api.Services;

/// <summary>Génère de petits fichiers PDF / DOCX / XLSX valides pour les documents de démonstration.</summary>
public static class PlaceholderFiles
{
    public static byte[] Create(string extension, string name, string version)
        => extension switch
        {
            "pdf" => Pdf(name, version),
            "docx" => Docx(name, version),
            _ => Xlsx(name, version)
        };

    private static string Line(string version) => $"Document de demonstration SmartDocs - version {version}";

    private static string Ascii(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c < 128 ? c : '?');
        }
        return sb.ToString();
    }

    private static byte[] Pdf(string name, string version)
    {
        static string Esc(string s) => s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var content = $"BT /F1 20 Tf 60 760 Td ({Esc(Ascii(name))}) Tj 0 -30 Td /F1 12 Tf ({Esc(Line(version))}) Tj ET";

        var sb = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        void Obj(string body)
        {
            offsets.Add(sb.Length);
            sb.Append(offsets.Count).Append(" 0 obj\n").Append(body).Append("\nendobj\n");
        }
        Obj("<< /Type /Catalog /Pages 2 0 R >>");
        Obj("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        Obj("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>");
        Obj($"<< /Length {content.Length} >>\nstream\n{content}\nendstream");
        Obj("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var xref = sb.Length;
        sb.Append("xref\n0 6\n0000000000 65535 f \n");
        foreach (var o in offsets) sb.Append(o.ToString("D10")).Append(" 00000 n \n");
        sb.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static byte[] Zip(params (string Path, string Content)[] entries)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in entries)
            {
                var entry = zip.CreateEntry(path);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
        }
        return ms.ToArray();
    }

    private const string Decl = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";
    private const string RelNs = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string DocRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static byte[] Docx(string name, string version)
    {
        var body = $"<w:p><w:r><w:t>{SecurityElement.Escape(name)}</w:t></w:r></w:p>" +
                   $"<w:p><w:r><w:t>{SecurityElement.Escape(Line(version))}</w:t></w:r></w:p>";
        return Zip(
            ("[Content_Types].xml", Decl + "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>"),
            ("_rels/.rels", Decl + $"<Relationships xmlns=\"{RelNs}\"><Relationship Id=\"rId1\" Type=\"{DocRel}/officeDocument\" Target=\"word/document.xml\"/></Relationships>"),
            ("word/document.xml", Decl + "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" + body + "</w:body></w:document>"));
    }

    private static byte[] Xlsx(string name, string version)
    {
        string Cell(string r, string text) => $"<c r=\"{r}\" t=\"inlineStr\"><is><t>{SecurityElement.Escape(text)}</t></is></c>";
        var sheet = Decl + "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
                    $"<row r=\"1\">{Cell("A1", name)}</row><row r=\"2\">{Cell("A2", Line(version))}</row></sheetData></worksheet>";
        return Zip(
            ("[Content_Types].xml", Decl + "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>"),
            ("_rels/.rels", Decl + $"<Relationships xmlns=\"{RelNs}\"><Relationship Id=\"rId1\" Type=\"{DocRel}/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>"),
            ("xl/workbook.xml", Decl + $"<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"{DocRel}\"><sheets><sheet name=\"Feuille1\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>"),
            ("xl/_rels/workbook.xml.rels", Decl + $"<Relationships xmlns=\"{RelNs}\"><Relationship Id=\"rId1\" Type=\"{DocRel}/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>"),
            ("xl/worksheets/sheet1.xml", sheet));
    }
}
