//using FinanceAPI.Controllers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace FinanceAPI.Repositories
{
    public class DatabaseInteractions
    {
        #region Private Variables
        private readonly string connectionString = string.Empty;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseInteractions> _logger;
        #endregion

        public DatabaseInteractions(IConfiguration configuration, ILogger<DatabaseInteractions> logger)
        {
            _logger = logger;
            _configuration = configuration;
            connectionString = _configuration["ConnectionString"] ?? string.Empty;
        }
        public void UpdateFinalReportRequest(int finalReportID, int statusCode, string mergedReport)
        {
            string updateMergedFinalRepotQuery = String.Format("EXEC UPDATE_FINAL_REPORT @ReportId={0}, @StatusCode={1}, @Base64String='{2}'", finalReportID, statusCode, mergedReport);
            DataTable accountDetailTable = new();
            using (SqlConnection connection = new(connectionString))
            {
                using SqlCommand command = new(updateMergedFinalRepotQuery, connection);
                try
                {
                    connection.Open();

                    using SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        accountDetailTable.Load(reader);
                    }
                    else
                    {

                    }

                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                     String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                 );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while update the Final Report request.\n Error Details:\n{0}", ex.Message)
                               );
                }
                finally
                {
                    accountDetailTable?.Dispose();
                }
            }
        }

        public void UpdateFinalReportRequest(int finalReportID, int statusCode)
        {
            string updateMergedFinalRepotQuery = String.Format("EXEC UPDATE_FINAL_REPORT @ReportId={0}, @StatusCode={1}", finalReportID, statusCode);
            DataTable accountDetailTable = new();
            using (SqlConnection connection = new(connectionString))
            {
                using SqlCommand command = new(updateMergedFinalRepotQuery, connection);
                try
                {
                    connection.Open();

                    using SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        accountDetailTable.Load(reader);
                    }
                    else
                    {

                    }

                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                     String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                 );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                  String.Format("An unexpected error occurred while update the Final Report request.\n Error Details:\n{0}", ex.Message)
                              );
                }
                finally
                {
                    accountDetailTable?.Dispose();
                }
            }
        }
    }
}
