using FinanceAPI.Controllers;
using Microsoft.Extensions.Configuration;
using System;
using Newtonsoft.Json;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace FinanceAPI.Services
{
    public class Services(IConfiguration configuration, ILogger<Services> logger)
    {
        #region Private Variables
        private readonly ILogger<Services> _logger = logger;
        private readonly IConfiguration _configuration = configuration;
        private static readonly HttpClient client = new();
        #endregion
        public async Task<string> ProcessReportAsync(int accountNumber, string[] ReportsIds)
        {
            try
            {
                List<byte[]> reportArray = new();
                foreach (string ReportId in ReportsIds)
                {
                    string API = _configuration["ApiSettings:ReportAPIPrefix"] + GetAPIUrl(ReportId);
                    // Add Account Number to Header
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("ACCOUNT", accountNumber.ToString());

                    HttpResponseMessage response = await client.GetAsync(API);

                    response.EnsureSuccessStatusCode(); // Throws if not successful

                    string responseData = await response.Content.ReadAsStringAsync();
                    responseData = responseData.Replace("data:application/pdf;base64,", "");
                    ReportResponse report = JsonConvert.DeserializeObject<ReportResponse>(responseData) ?? new ReportResponse { Report = "" };
                    reportArray.Add(Convert.FromBase64String(report.Report));
                }
                byte[] mergedReportArray = GetMergedReport(reportArray);
                string mergedReport = Convert.ToBase64String(mergedReportArray);
                return mergedReport;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                                    string.Format("An unexpected error occurred in processing the Report.\n Error Details:\n{0}", ex.Message)
                                );
                throw;
            }
        }
        public string GetAPIUrl(string ReportId)
        {
            return ReportId switch
            {
                "1" => "/GetPortfolioPerformanceReport",
                " 2" => "/GetAssetAllocationReport",
                _ => "",
            };
        }

        public byte[] GetMergedReport(List<byte[]> reportArray)
        {
            using (MemoryStream outputStream = new MemoryStream())
            {
                Document document = new Document();
                PdfCopy copy = new PdfCopy(document, outputStream);
                document.Open();

                foreach (byte[] report in reportArray)
                {
                    AddPdfToDocument(copy, report);
                }

                document.Close();
                return outputStream.ToArray();
            }
        }

        private void AddPdfToDocument(PdfCopy copy, byte[] pdfBytes)
        {
            using (MemoryStream inputStream = new MemoryStream(pdfBytes))
            {
                PdfReader reader = new PdfReader(inputStream);
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    copy.AddPage(copy.GetImportedPage(reader, i));
                }
                reader.Close();
            }
        }
    }

    public class ReportResponse
    {
        public required string Report;
    }
}
