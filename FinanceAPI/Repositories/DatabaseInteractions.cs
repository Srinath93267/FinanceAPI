//using FinanceAPI.Controllers;
using FinanceAPI.Controllers;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Data;
using System.Runtime;

namespace FinanceAPI.Repositories
{
    public class DatabaseInteractions
    {
        #region Private Variables
        private readonly string connectionString = string.Empty;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseInteractions> _logger;
        private readonly ApiSettings _settings;
        #endregion
        public DatabaseInteractions(IConfiguration configuration, ILogger<DatabaseInteractions> logger, IOptions<ApiSettings> options)
        {
            _logger = logger;
            _configuration = configuration;
            _settings = options.Value;
            connectionString = $"{_settings.ConnectionString}" ?? string.Empty;
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
                    command?.Dispose();
                    connection?.Dispose();
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
                    command?.Dispose();
                    connection?.Dispose();
                    accountDetailTable?.Dispose();
                }
            }
        }
        public async Task DeleteReportFromAPreset(int presetID, PresetInfo removedSelectReport)
        {
            string deleteReportFromAPresetQuery = String.Format("EXEC DELETE_A_REPORT_FROM_A_PRESET @PresetId={0}, @ReportId={1}", presetID, removedSelectReport.Reports.Id);
            using SqlConnection connection = new(connectionString);
            using SqlCommand command = new(deleteReportFromAPresetQuery, connection);
            try
            {
                connection.Open();

                await command.ExecuteReaderAsync();

                _logger.LogInformation(
                                    String.Format("The {0} has been successfully removed from the Preset ID: {0}", removedSelectReport.Reports.Name, presetID));
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred.\n Error Details:\n{0}", ex.Message)
                                );
                throw;
            }
            finally
            {
                command?.Dispose();
                connection?.Dispose();
            }
        }
        public async Task AddReportToAPreset(int presetID, PresetInfo newSelectedReport)
        {
            string deleteReportFromAPresetQuery = String.Format("EXEC ADD_REPORT_TO_PRESET @PresetId={0}, @ReportId={1}", presetID, newSelectedReport.Reports.Id);
            using SqlConnection connection = new(connectionString);
            using SqlCommand command = new(deleteReportFromAPresetQuery, connection);
            try
            {
                connection.Open();

                await command.ExecuteReaderAsync();

                _logger.LogInformation(
                                    String.Format("The {0} has been successfully added to the Preset ID: {0}", newSelectedReport.Reports.Name, presetID));
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred.\n Error Details:\n{0}", ex.Message)
                                );
                throw;
            }
            finally
            {
                command?.Dispose();
                connection?.Dispose();
            }
        }
    }
}
