using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
using SautinSoft;
using System;
using System.IO;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ConvertPDFToWord.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PDFToWordConverter : ControllerBase
    {
        [HttpPost("convert")]
        public async Task<IActionResult> ConvertPdfToWord([FromForm] IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                return BadRequest("No PDF file uploaded.");

            /*SautinSoft.PdfFocus.SetLicense("01/31/266O51E2xbXSSV2jQXDE+zZz5VeizcvawU49");*/

            // Save uploaded PDF temporarily
            string pdfPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            string docxPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".docx");

            try
            {
                // Save uploaded file
                using (var stream = new FileStream(pdfPath, FileMode.Create))
                    await pdfFile.CopyToAsync(stream);

                PdfFocus f = new PdfFocus();

                // Load PDF
                f.OpenPdf(pdfPath);

                if (f.PageCount > 0)
                {
                    // Conversion options
                    f.WordOptions.Format = PdfFocus.CWordOptions.eWordDocument.Docx;
                    // Optional: uncomment if supported by your version
                    f.WordOptions.DetectTables = true;
                    // f.WordOptions.PreserveTextLayout = true;

                    // Convert to Word
                    int result = f.ToWord(docxPath);

                    // Explicitly close to free the file lock
                    f.ClosePdf();
                    //Remove Watermarks
                    using (var wordDoc = WordprocessingDocument.Open(docxPath, true))
                    {
                        var body = wordDoc.MainDocumentPart.Document.Body;

                        foreach (var paragraph in body.Elements<Paragraph>())
                        {
                            // Merge all run texts in the paragraph
                            string fullText = string.Concat(paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));


                            if (fullText.Contains("SautinSoft"))
                            {
                                // Remove the text
                                fullText = fullText.Replace("SautinSoft", string.Empty);

                                // Remove all existing runs
                                paragraph.RemoveAllChildren<Run>();

                                // Add a single new run with updated text
                                var run = new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(fullText));

                                paragraph.Append(run);
                            }
                        }

                        wordDoc.MainDocumentPart.Document.Save();
                    }

                    if (result == 0)
                    {
                        var fileBytes = await System.IO.File.ReadAllBytesAsync(docxPath);
                        return File(fileBytes,
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                            "Converted.docx");
                    }
                    else
                    {
                        return StatusCode(500, "Failed to convert PDF.");
                    }
                }

                return BadRequest("Invalid or empty PDF file.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
            finally
            {
                // Wait a bit to ensure OS releases file locks
                await Task.Delay(300);

                try
                {
                    if (System.IO.File.Exists(pdfPath))
                        System.IO.File.Delete(pdfPath);

                    if (System.IO.File.Exists(docxPath))
                        System.IO.File.Delete(docxPath);
                }
                catch
                {
                    // Ignore file lock cleanup errors
                }
            }
        }
      
    }
}
