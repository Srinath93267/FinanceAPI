﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FinanceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FinanceController : ControllerBase
    {
        #region Private Variables
        private readonly ILogger<FinanceController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string connectionString = string.Empty;
        private readonly string _secretApiKey = string.Empty;
        private List<FinalReport> finalReportsData = [];
        private readonly Services.Services _service;
        private readonly Repositories.DatabaseInteractions _databaseInteractor;
        private readonly ApiSettings _settings;
        #endregion
        public FinanceController(IConfiguration configuration, ILogger<FinanceController> logger, Services.Services service, Repositories.DatabaseInteractions databaseInteractor, IOptions<ApiSettings> options)
        {
            _logger = logger;
            _configuration = configuration;
            _settings = options.Value;
            _secretApiKey = $"{_settings.SecretKey}" ?? string.Empty;
            connectionString = $"{_settings.ConnectionString}" ?? string.Empty;
            _service = service;
            _databaseInteractor = databaseInteractor;
        }

        // GET: api/<FinanceController>
        [HttpGet]
        [Route("GetAllReportsList")]
        public ActionResult<List<ReportList>> GetAllReportsList([FromHeader(Name = "X-API-KEY")] string apiKey)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            List<ReportList> reportList = [];

            string query = "SELECT * FROM REPORT_LIST";
            DataTable reportListTable = new DataTable();
            using (SqlConnection connection = new(connectionString))
            {
                using (SqlCommand command = new(query, connection))
                {
                    try
                    {
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                reportListTable.Load(reader);
                            }
                            else
                            {
                                return NotFound();
                            }
                        }

                        foreach (DataRow row in reportListTable.Rows)
                        {
                            reportList.Add(new ReportList { Id = Convert.ToInt32(row[0]), Name = (string)row[1] });
                        }
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError(
                                         String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                     );
                        return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                                       String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                   );
                        return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                    }
                    finally
                    {
                        reportListTable?.Dispose();
                    }
                }
            }
            return Ok(reportList);
        }

        [HttpGet]
        [Route("GetAllPresetsList")]
        public ActionResult<List<Preset>> GetAllPresetsList([FromHeader(Name = "X-API-KEY")] string apiKey)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            List<Preset> presetList = [];
            string query = "EXEC GET_ALL_PRESET_DETAILS;";
            DataTable presetListsTable = new();
            using (SqlConnection connection = new(connectionString))
            {
                using SqlCommand command = new(query, connection);
                try
                {
                    connection.Open();

                    using SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        presetListsTable.Load(reader);
                        List<int> presetIds = presetListsTable.AsEnumerable().Select(row => Convert.ToInt32(row[0])).Distinct().ToList();
                        foreach (int id in presetIds)
                        {
                            var presetName = presetListsTable.AsEnumerable().Where(row => Convert.ToInt32(row[0]) == id).Select(row => (string)row[1]).Distinct().FirstOrDefault() ?? "";
                            DataTable reportsOfPresetTable = presetListsTable.AsEnumerable().Where(row => Convert.ToInt32(row[0]) == id).CopyToDataTable();
                            List<ReportList> presetReportList = [.. reportsOfPresetTable.AsEnumerable().Select(row => new ReportList { Id = Convert.ToInt32(row[2]), Name = (string)row[3] })];
                            presetList.Add(new Preset { Id = id, Name = presetName.ToString(), CreatedBy = "", Reports = [.. presetReportList] });
                            reportsOfPresetTable?.Dispose();
                        }
                    }
                    else
                    {
                        return NotFound();
                    }

                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                     String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                               );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                finally
                {
                    presetListsTable?.Dispose();
                }
            }

            return presetList;
        }

        [HttpGet]
        [Route("GetAPresetDetail/{presetId}")]
        public ActionResult<Preset> GetAPresetDetail([FromHeader(Name = "X-API-KEY")] string apiKey, int presetId)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            Preset preset;
            string query = String.Format("EXEC GET_A_PRESET_DETAIL @PresetId={0};", presetId);
            DataTable presetListsTable = new();
            using (SqlConnection connection = new(connectionString))
            {
                using SqlCommand command = new(query, connection);
                try
                {
                    connection.Open();

                    using SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        presetListsTable.Load(reader);

                        var presetName = presetListsTable.AsEnumerable().Where(row => Convert.ToInt32(row[0]) == presetId).Select(row => (string)row[1]).Distinct().FirstOrDefault() ?? "";
                        DataTable reportsOfPresetTable = presetListsTable.AsEnumerable().Where(row => Convert.ToInt32(row[0]) == presetId).CopyToDataTable();
                        List<ReportList> presetReportList = [.. reportsOfPresetTable.AsEnumerable().Select(row => new ReportList { Id = Convert.ToInt32(row[2]), Name = (string)row[3] })];
                        preset = new Preset { Id = presetId, Name = presetName.ToString(), CreatedBy = "", Reports = [.. presetReportList] };
                        reportsOfPresetTable?.Dispose();
                    }
                    else
                    {
                        return NotFound();
                    }

                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                     String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                               );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                finally
                {
                    presetListsTable?.Dispose();
                }
            }

            return preset;
        }

        [HttpGet]
        [Route("GetAnAccountDetail/{AccountNumber}")]
        public ActionResult<Account> GetAnAccountDetail([FromHeader(Name = "X-API-KEY")] string apiKey, Int64 AccountNumber)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            Account account;
            string query = String.Format("EXEC GET_AN_ACCOUNT_DETAIL @AccountNumber={0};", AccountNumber);
            DataTable accountDetailTable = new();
            using (SqlConnection connection = new(connectionString))
            {
                using SqlCommand command = new(query, connection);
                try
                {
                    connection.Open();

                    using SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        accountDetailTable.Load(reader);
                        account = accountDetailTable.AsEnumerable().Select(row => new Account { AccountNumber = Convert.ToInt64(row[0]), ClientName = (string)row[1] }).FirstOrDefault() ?? new Account { AccountNumber = 0, ClientName = "" };
                    }
                    else
                    {
                        return NotFound();
                    }

                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                     String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                               );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                finally
                {
                    accountDetailTable?.Dispose();
                }
            }

            return account;
        }

        [HttpGet]
        [Route("GetAccounts")]
        public ActionResult<List<Account>> GetAccounts([FromHeader(Name = "X-API-KEY")] string apiKey)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            List<Account> accountList;
            string query = "EXEC GET_ACCOUNTS";
            DataTable accountDetailTable = new();
            using (SqlConnection connection = new(connectionString))
            {
                using SqlCommand command = new(query, connection);
                try
                {
                    connection.Open();

                    using SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        accountDetailTable.Load(reader);
                        accountList = [.. accountDetailTable.AsEnumerable().Select(row => new Account { AccountNumber = Convert.ToInt64(row[0]), ClientName = (string)row[1] })];
                    }
                    else
                    {
                        return NotFound();
                    }

                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                     String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                               );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                finally
                {
                    accountDetailTable?.Dispose();
                }
            }

            return Ok(accountList);
        }

        [HttpGet]
        [Route("GetPortfolioPerformanceData")]
        public ActionResult<PortfolioPerformance> GetPortfolioPerformanceData([FromHeader(Name = "X-API-KEY")] string apiKey, [FromHeader(Name = "ACCOUNT")] Int64 AccountNumber)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            PortfolioPerformance portfolioPerformanceData;
            string query = String.Format("EXEC GET_PORTFOLIO_PERFORMANCE_DATA @AccountNumber={0};", AccountNumber);
            DataTable portfolioPerformanceDataTable = new();
            using (SqlConnection connection = new(connectionString))
            {
                using SqlCommand command = new(query, connection);
                try
                {
                    connection.Open();

                    using SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        portfolioPerformanceDataTable.Load(reader);
                        portfolioPerformanceData = portfolioPerformanceDataTable.AsEnumerable().Select(row =>
                        new PortfolioPerformance
                        {
                            PortfolioID = Convert.ToInt32(row[1]),
                            AccountNumber = Convert.ToInt32(row[2]),
                            ClientName = (string)row[0],
                            TotalPortfolioValue = (decimal)row[3],
                            InvestmentGrowthYTD = (decimal)row[4],
                            InvestmentGrowth1Y = (decimal)row[5],
                            InvestmentGrowth3Y = (decimal)row[6],
                            InvestmentGrowth5Y = (decimal)row[7],
                            InvestmentGrowth10Y = (decimal)row[8],
                            InvestmentGrowthSinceInception = (decimal)row[9],
                            AnnualizedReturnYTD = (decimal)row[10],
                            AnnualizedReturn1Y = (decimal)row[11],
                            AnnualizedReturn3Y = (decimal)row[12],
                            AnnualizedReturn5Y = (decimal)row[13],
                            AnnualizedReturn10Y = (decimal)row[14],
                            AnnualizedReturnSinceInception = (decimal)row[15],
                            RiskLevel = (string)row[16],
                            BenchmarkPerformance = (decimal)row[17]
                        }).FirstOrDefault() ??
                        new PortfolioPerformance
                        {
                            PortfolioID = 0,
                            AccountNumber = 0,
                            ClientName = "",
                            TotalPortfolioValue = 0,
                            InvestmentGrowthYTD = 0,
                            InvestmentGrowth1Y = 0,
                            InvestmentGrowth3Y = 0,
                            InvestmentGrowth5Y = 0,
                            InvestmentGrowth10Y = 0,
                            InvestmentGrowthSinceInception = 0,
                            AnnualizedReturnYTD = 0,
                            AnnualizedReturn1Y = 0,
                            AnnualizedReturn5Y = 0,
                            AnnualizedReturn10Y = 0,
                            AnnualizedReturnSinceInception = 0,
                            AnnualizedReturn3Y = 0,
                            RiskLevel = "",
                            BenchmarkPerformance = 0
                        };
                    }
                    else
                    {
                        return NotFound();
                    }

                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                     String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                               );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                finally
                {
                    portfolioPerformanceDataTable?.Dispose();
                }
            }

            return portfolioPerformanceData;
        }

        [HttpGet]
        [Route("GetAssetAllocationData")]
        public ActionResult<AssetAllocation> GetAssetAllocationData([FromHeader(Name = "X-API-KEY")] string apiKey, [FromHeader(Name = "ACCOUNT")] Int64 AccountNumber)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            AssetAllocation assetAllocationData;
            string query = String.Format("EXEC GET_ASSET_ALLOCATION_DATA @AccountNumber={0};", AccountNumber);
            DataTable assetAllocationDataTable = new();
            using (SqlConnection connection = new(connectionString))
            {
                using SqlCommand command = new(query, connection);
                try
                {
                    connection.Open();

                    using SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        assetAllocationDataTable.Load(reader);
                        assetAllocationData =
                        new AssetAllocation
                        {
                            AllocationID = assetAllocationDataTable.AsEnumerable().Select(row => Convert.ToInt32(row[1])).Distinct().FirstOrDefault(),
                            AccountNumber = assetAllocationDataTable.AsEnumerable().Select(row => Convert.ToInt32(row[2])).Distinct().FirstOrDefault(),
                            ClientName = assetAllocationDataTable.AsEnumerable().Select(row => (string)row[0]).FirstOrDefault() ?? "",
                            AssetClass = [.. assetAllocationDataTable.AsEnumerable().Select(row => (string)row[3])],
                            AllocationPercentage = [.. assetAllocationDataTable.AsEnumerable().Select(row => (decimal)row[4])],
                            MarketValue = [.. assetAllocationDataTable.AsEnumerable().Select(row => (decimal)row[5])],
                            ExpectedReturnPercentage = [.. assetAllocationDataTable.AsEnumerable().Select(row => (decimal)row[6])],
                            HistoricalPerformance3Y = [.. assetAllocationDataTable.AsEnumerable().Select(row => (decimal)row[7])],
                            HistoricalPerformance5Y = [.. assetAllocationDataTable.AsEnumerable().Select(row => (decimal)row[8])],
                            VolatilityRiskLevel = [.. assetAllocationDataTable.AsEnumerable().Select(row => (string)row[9])],
                            TargetAllocationPercentage = [.. assetAllocationDataTable.AsEnumerable().Select(row => (decimal)row[10])],
                            DeviationFromTarget = [.. assetAllocationDataTable.AsEnumerable().Select(row => (decimal)row[11])],
                            RebalancingRequired = [.. assetAllocationDataTable.AsEnumerable().Select(row => (bool)row[12])],
                            LiquidityLevel = [.. assetAllocationDataTable.AsEnumerable().Select(row => (string)row[13])],
                            MaturityDate = [.. assetAllocationDataTable.AsEnumerable().Select(row => row[14] != DBNull.Value ? (DateTime)row[14] : DateTime.MinValue)],
                            DividendYield = [.. assetAllocationDataTable.AsEnumerable().Select(row => (decimal)row[15])],
                            AdvisorNotes = [.. assetAllocationDataTable.AsEnumerable().Select(row => (string)row[16])],
                            CurrencyType = [.. assetAllocationDataTable.AsEnumerable().Select(row => (string)row[17])]
                        } ??
                        new AssetAllocation
                        {
                            AllocationID = 0,
                            AccountNumber = 0,
                            ClientName = "",
                            AssetClass = [],
                            AllocationPercentage = [],
                            MarketValue = [],
                            ExpectedReturnPercentage = [],
                            HistoricalPerformance3Y = [],
                            HistoricalPerformance5Y = [],
                            VolatilityRiskLevel = [],
                            TargetAllocationPercentage = [],
                            DeviationFromTarget = [],
                            RebalancingRequired = [false],
                            LiquidityLevel = [],
                            MaturityDate = [DateTime.MinValue],
                            DividendYield = [],
                            AdvisorNotes = [],
                            CurrencyType = [],
                        };
                    }
                    else
                    {
                        return NotFound();
                    }

                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                     String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                               );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                finally
                {
                    assetAllocationDataTable?.Dispose();
                }
            }

            return assetAllocationData;
        }

        [HttpGet]
        [Route("GetReadyReportsByAccount")]
        public ActionResult<List<FinalReport>> GetReadyReportsByAccount([FromHeader(Name = "X-API-KEY")] string apiKey, [FromHeader(Name = "ACCOUNT")] Int64 AccountNumber)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            List<FinalReport> ReadyReportsData = [];
            string query = String.Format("EXEC GET_FINAL_REPORT_REQUEST_BY_ACCNUM @Accountnumber={0};", AccountNumber);
            DataTable finalReportsDataTable = new();
            using (SqlConnection connection = new(connectionString))
            {
                using SqlCommand command = new(query, connection);
                try
                {
                    connection.Open();

                    using SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        finalReportsDataTable.Load(reader);
                        reader?.DisposeAsync();
                        finalReportsData = [.. finalReportsDataTable.AsEnumerable().Select(row => new FinalReport
                        {
                            FinalReportID = Convert.ToInt32(row[0]),
                            AccountNumber = Convert.ToInt32(row[1]),
                            ReportTitle = (string)row[2],
                            ReportPdf = row[3] != DBNull.Value ? (byte[])row[3] : Array.Empty<byte>(),
                            ReportDate = (DateTime)row[4],
                            PresetID = Convert.ToInt32(row[5]!=DBNull.Value?row[5]:0),
                            CreatedBy = (string)row[6],
                            StatusCd = Convert.ToInt32(row[7]),
                            ReportIDs = (string)row[8],
                            CreatedOn = (DateTime)row[9],
                            LastUpdatedOn = (DateTime)row[10],
                            Comments = row[11]!=DBNull.Value?(string)row[11]:"",
                            ClientName = (string)row[12],
                            PresetName = row[13]!=DBNull.Value?(string)row[13]:""
                        })];
                        ReadyReportsData = [.. finalReportsData.Where(row => row.StatusCd == 200)];
                    }
                    else
                    {
                        return NotFound();
                    }

                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                     String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                               );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                finally
                {
                    finalReportsDataTable?.Dispose();
                }
            }

            return ReadyReportsData;
        }

        [HttpGet]
        [Route("GetQueueReportsByAccount")]
        public ActionResult<List<FinalReport>> GetQueueReportsByAccount([FromHeader(Name = "X-API-KEY")] string apiKey, [FromHeader(Name = "ACCOUNT")] Int64 AccountNumber)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            List<FinalReport> QueueReportsData = [];
            string query = String.Format("EXEC GET_FINAL_REPORT_REQUEST_BY_ACCNUM @Accountnumber={0};", AccountNumber);
            DataTable finalReportsDataTable = new();
            using (SqlConnection connection = new(connectionString))
            {
                using SqlCommand command = new(query, connection);
                try
                {
                    connection.Open();

                    using SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        finalReportsDataTable.Load(reader);
                        finalReportsData = [.. finalReportsDataTable.AsEnumerable().Select(row => new FinalReport
                        {
                            FinalReportID = Convert.ToInt32(row[0]),
                            AccountNumber = Convert.ToInt32(row[1]),
                            ReportTitle = (string)row[2],
                            ReportPdf = row[3] != DBNull.Value ? (byte[])row[3] : Array.Empty<byte>(),
                            ReportDate = (DateTime)row[4],
                            PresetID = Convert.ToInt32(row[5]!=DBNull.Value?row[5]:0),
                            CreatedBy = (string)row[6],
                            StatusCd = Convert.ToInt32(row[7]),
                            ReportIDs = (string)row[8],
                            CreatedOn = (DateTime)row[9],
                            LastUpdatedOn = (DateTime)row[10],
                            Comments = row[11]!=DBNull.Value?(string)row[11]:"",
                            ClientName = (string)row[12],
                            PresetName = row[13]!=DBNull.Value?(string)row[13]:""
                        })];
                        QueueReportsData = [.. finalReportsData.Where(row => row.StatusCd != 200)];
                    }
                    else
                    {
                        return NotFound();
                    }

                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                     String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                               );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                finally
                {
                    finalReportsDataTable?.Dispose();
                }
            }

            return QueueReportsData;
        }

        [HttpPut]
        [Route("CreateNewPreset")]
        public ActionResult<int> CreateNewPreset([FromHeader(Name = "X-API-KEY")] string apiKey, [FromBody] NewPreset preset)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            string newPresetInsertQuery = String.Format("EXEC CREATE_NEW_PRESET @PresetName='{0}';", preset.Name);
            DataTable newPresetInsertedTable = new();
            DataTable newPresetReportsInsertedTable = new();
            int presetID = 0;
            string presetName = string.Empty;
            using SqlConnection connection = new(connectionString);
            using SqlCommand command = new(newPresetInsertQuery, connection);
            try
            {
                connection.Open();

                using SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    newPresetInsertedTable.Load(reader);
                    reader?.Dispose();
                    presetID = newPresetInsertedTable.AsEnumerable().Select(row => Convert.ToInt32(row[0])).FirstOrDefault();
                    presetName = newPresetInsertedTable.AsEnumerable().Select(row => Convert.ToInt32(row[0])).FirstOrDefault().ToString();
                    foreach (ReportList reportList in preset.Reports)
                    {
                        string newPresetReportInsertQuery = String.Format("EXEC ADD_REPORT_TO_PRESET @PresetId={0}, @ReportId={1};", presetID, reportList.Id);
                        using SqlCommand command2 = new(newPresetReportInsertQuery, connection);
                        try
                        {
                            using SqlDataReader reader2 = command2.ExecuteReader();
                            reader2?.Dispose();
                        }
                        catch (SqlException ex)
                        {
                            if (connection.State == ConnectionState.Open)
                            {
                                connection.Close();
                            }
                            _logger.LogError(
                                String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                            );
                            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                        }
                    }
                }
                reader?.Dispose();
                //return StatusCode(StatusCodes.Status200OK, String.Format("The Preset {0} has been created successfully", presetName));
                return Ok(presetID);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
            finally
            {
                newPresetInsertedTable?.Dispose();
                newPresetReportsInsertedTable?.Dispose();
            }
        }

        [HttpPut]
        [Route("CreateNewFinalReportRequest")]
        public ActionResult<int> CreateNewFinalReportRequest([FromHeader(Name = "X-API-KEY")] string apiKey, [FromBody] FinalReportRequest finalReportRequest)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            string newfinalReportInsertQuery = string.Empty;

            newfinalReportInsertQuery = finalReportRequest.PresetID == 0 ? String.Format
                                                                                    (
                                                                                        "EXEC INSERT_FINAL_REPORT_REQUEST " +
                                                                                        "@Accountnumber={0}, @Reporttitle='{1}', " +
                                                                                        "@Reportdate='{2}', @Createdby='{3}', " +
                                                                                        "@Statuscode={4}, @Reportids='{5}', @Comments='{6}';",
                                                                                        finalReportRequest.AccountNumber, finalReportRequest.ReportTitle,
                                                                                        finalReportRequest.ReportDate.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                                                                        finalReportRequest.CreatedBy, 500, finalReportRequest.ReportIDs,
                                                                                        finalReportRequest.Comments
                                                                                    ) :
                                                                            String.Format
                                                                                    (
                                                                                        "EXEC INSERT_FINAL_REPORT_REQUEST " +
                                                                                        "@Accountnumber={0}, @Reporttitle='{1}', " +
                                                                                        "@Reportdate='{2}', @PresetID={3}, @Createdby='{4}', " +
                                                                                        "@Statuscode={5}, @Reportids='{6}', @Comments='{7}';",
                                                                                        finalReportRequest.AccountNumber, finalReportRequest.ReportTitle,
                                                                                        finalReportRequest.ReportDate.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                                                                        finalReportRequest.PresetID, finalReportRequest.CreatedBy,
                                                                                        500, finalReportRequest.ReportIDs, finalReportRequest.Comments
                                                                                    );

            DataTable newfinalReportInsertedTable = new();
            using SqlConnection connection = new(connectionString);
            using SqlCommand command = new(newfinalReportInsertQuery, connection);
            try
            {
                connection.Open();

                using SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    newfinalReportInsertedTable.Load(reader);
                    reader?.Dispose();
                }
                _logger.LogInformation(
                                    String.Format("The Final Report request has been created successfully. Final Report ID: {0}", newfinalReportInsertedTable.AsEnumerable().Select(row => (decimal)row[0]).FirstOrDefault()));
                return StatusCode(StatusCodes.Status200OK, new
                {
                    message = String.Format("The Final Report request has been created successfully. Final Report ID: {0}",
                                                           newfinalReportInsertedTable.AsEnumerable().Select(row => (decimal)row[0]).FirstOrDefault())
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
            finally
            {
                newfinalReportInsertedTable?.Dispose();
            }
        }

        [HttpPatch]
        [Route("UpdatePreset")]
        public async Task<IActionResult> UpdatePreset([FromHeader(Name = "X-API-KEY")] string apiKey, [FromBody] UpdatePreset updatePreset)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }
            try
            {
                foreach (PresetInfo presetInfo in updatePreset.RemovedSelectedReports)
                {
                    await _databaseInteractor.DeleteReportFromAPreset(updatePreset.PresetId, presetInfo);
                }

                foreach (PresetInfo presetInfo in updatePreset.NewSelectedReports)
                {
                    await _databaseInteractor.AddReportToAPreset(updatePreset.PresetId, presetInfo);
                }

                return StatusCode(StatusCodes.Status200OK, new { message = "Preset has been Updated Successfully" });
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPatch]
        [Route("RegenerateFinalReport")]
        public IActionResult RegenerateFinalReport([FromHeader(Name = "X-API-KEY")] string apiKey, [FromBody] int finalReportID)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            string getAFinalReportDetailquery = String.Format("EXEC GET_FINAL_REPORT_REQUEST_BY_REPORT_ID @ReportId={0}", finalReportID);
            using SqlConnection connection = new(connectionString);
            using SqlCommand command = new(getAFinalReportDetailquery, connection);
            try
            {
                connection.Open();
                using SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Dispose();
                    string regenreqAFinalReportquery = String.Format("EXEC REGENERATE_FINAL_REPORT @ReportId={0}", finalReportID);
                    using SqlCommand command2 = new(regenreqAFinalReportquery, connection);
                    try
                    {
                        using SqlDataReader reader2 = command2.ExecuteReader();
                        reader2.Dispose();
                        return StatusCode(StatusCodes.Status200OK, new { message = "The Regeneration request for the final report has been done successfully." });
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError(
                                            String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                        );
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "The Final Report was not found" });
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
            finally
            {
                connection?.Close();
                command?.Dispose();
            }
        }

        [HttpPut]
        [Route("ProcessNewFinalReportRequest")]
        public async Task<IActionResult> ProcessNewFinalReportRequestAsync([FromHeader(Name = "X-API-KEY")] string apiKey, [FromBody] int finalReportID)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            string getNewFinalReportInsertedQuery = String.Format("EXEC GET_FINAL_REPORT_REQUEST_BY_REPORT_ID @Reportid={0}", finalReportID);
            DataTable getNewFinalReportInsertedTable = new();
            using SqlConnection connection = new(connectionString);
            using SqlCommand command = new(getNewFinalReportInsertedQuery, connection);
            try
            {
                connection.Open();

                using SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    getNewFinalReportInsertedTable.Load(reader);
                    reader?.Dispose();
                    int AccountNumber = getNewFinalReportInsertedTable.AsEnumerable().Select(row => Convert.ToInt32(row[1])).FirstOrDefault();
                    var ReportDate = getNewFinalReportInsertedTable.AsEnumerable().Select(row => (DateTime)row[4]).FirstOrDefault();
                    string[] ReportIds = (getNewFinalReportInsertedTable.AsEnumerable().Select(row => (string)row[8]).FirstOrDefault() ?? "1, 2").Replace(" ", "").Split(',');
                    Task<string> mergedReportTask = _service.ProcessReportAsync(AccountNumber, ReportIds, ReportDate.ToShortDateString());
                    string mergedReport = await mergedReportTask;
                    _databaseInteractor.UpdateFinalReportRequest(finalReportID, 200, mergedReport);
                    return StatusCode(StatusCodes.Status200OK, "Report has been processed successfully");
                }
                else
                {
                    return StatusCode(StatusCodes.Status404NotFound, $"Final Report Id:{finalReportID} not found");
                }
            }
            catch (SqlException ex)
            {
                _databaseInteractor.UpdateFinalReportRequest(finalReportID, 400);
                _logger.LogError(
                                    String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
            catch (Exception ex)
            {
                _databaseInteractor.UpdateFinalReportRequest(finalReportID, 400);
                _logger.LogError(
                                    String.Format("An unexpected error occurred.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
            finally
            {
                getNewFinalReportInsertedTable?.Dispose();
            }
        }

        [HttpDelete]
        [Route("DeletePreset")]
        public IActionResult DeletePreset([FromHeader(Name = "X-API-KEY")] string apiKey, [FromBody] int presetId)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            string getAPresetDetailquery = String.Format("EXEC GET_A_PRESET_DETAIL @PresetId={0}", presetId);
            using (SqlConnection connection = new(connectionString))
            {
                using SqlCommand command = new(getAPresetDetailquery, connection);
                try
                {
                    connection.Open();
                    using SqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Dispose();
                        string deleteAPresetDetailQuery = String.Format("EXEC DELETE_PRESET @PresetId={0}", presetId);
                        using SqlCommand command2 = new(deleteAPresetDetailQuery, connection);
                        try
                        {
                            using SqlDataReader reader2 = command2.ExecuteReader();
                            reader2.Dispose();
                        }
                        catch (SqlException ex)
                        {
                            _logger.LogError(
                                                String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                            );
                            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
                        }
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status404NotFound, "The Preset was not found");
                    }
                    return StatusCode(StatusCodes.Status200OK, new { message = "The Preset has been deleted sucessfully" });
                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                        String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                    );
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                        String.Format("An unexpected error occurred.\nError Details:\n{0}", ex)
                                    );
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
                }
            }
        }

        [HttpDelete]
        [Route("DeleteFinalReport")]
        public IActionResult DeleteFinalReport([FromHeader(Name = "X-API-KEY")] string apiKey, [FromBody] int finalReportID)
        {
            if (apiKey != _secretApiKey || apiKey == string.Empty)
            {
                return Unauthorized(new { message = "Invalid API Key" });
            }

            string getAFinalReportDetailquery = String.Format("EXEC GET_FINAL_REPORT_REQUEST_BY_REPORT_ID @ReportId={0}", finalReportID);
            using SqlConnection connection = new(connectionString);
            using SqlCommand command = new(getAFinalReportDetailquery, connection);
            try
            {
                connection.Open();
                using SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Dispose();
                    string deleteAFinalReportquery = String.Format("EXEC DELETE_FINAL_REPORT @ReportId={0}", finalReportID);
                    using SqlCommand command2 = new(deleteAFinalReportquery, connection);
                    try
                    {
                        using SqlDataReader reader2 = command2.ExecuteReader();
                        reader2.Dispose();
                        return StatusCode(StatusCodes.Status200OK, new { message = "The Final Report has been deleted successfully" });
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError(
                                            String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                        );
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
                    }
                }
                else
                {
                    return StatusCode(StatusCodes.Status404NotFound, new { message = "The Final Report was not found" });
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred while executing the query.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred.\nError Details:\n{0}", ex)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
            finally
            {
                connection?.Close();
                command?.Dispose();
            }
        }

        //[HttpPost("GenerateToken")]
        //public IActionResult GenerateToken([FromBody] LoginRequest request)
        //{
        //    // You'd validate user credentials here
        //    if (request.Username != "admin" || request.Password != "password")
        //        return Unauthorized();

        //    var key = $"{_settings.SecretKey}" ?? string.Empty;
        //    var issuer = _configuration["Jwt:Issuer"];

        //    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        //    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        //    var claims = new[]
        //    {
        //        new Claim(JwtRegisteredClaimNames.Sub, request.Username),
        //        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        //        new Claim("role", "Admin")
        //    };

        //    var token = new JwtSecurityToken(
        //        issuer: issuer,
        //        audience: issuer,
        //        claims: claims,
        //        expires: DateTime.UtcNow.AddMinutes(60),
        //        signingCredentials: credentials
        //    );

        //    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        //    return Ok(new { token = jwt });
        //}

        #region Template Code
        //// GET api/<FinanceController>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/<FinanceController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/<FinanceController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/<FinanceController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
        #endregion
    }

    public class ReportList
    {
        public int Id { get; set; }
        public required string Name { get; set; }
    }

    public class Preset
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required ReportList[] Reports { get; set; }
        public required string CreatedBy { get; set; } = string.Empty;
    }

    public class PresetInfo
    {
        public required ReportList Reports { get; set; }
        public required bool Selected { get; set; }
    }

    public class NewPreset
    {
        public required string Name { get; set; }
        public required ReportList[] Reports { get; set; }
        public required string CreatedBy { get; set; } = string.Empty;
    }

    public class Account
    {
        public Int64 AccountNumber { get; set; }
        public required string ClientName { get; set; }
    }

    public class PortfolioPerformance
    {
        public Int64 PortfolioID { get; set; }
        public Int64 AccountNumber { get; set; }
        public required string ClientName { get; set; }
        public decimal TotalPortfolioValue { get; set; }

        // Investment Growth
        public decimal InvestmentGrowthYTD { get; set; }
        public decimal InvestmentGrowth1Y { get; set; }
        public decimal InvestmentGrowth3Y { get; set; }
        public decimal InvestmentGrowth5Y { get; set; }
        public decimal InvestmentGrowth10Y { get; set; }
        public decimal InvestmentGrowthSinceInception { get; set; }

        // Annualized Returns
        public decimal AnnualizedReturnYTD { get; set; }
        public decimal AnnualizedReturn1Y { get; set; }
        public decimal AnnualizedReturn3Y { get; set; }
        public decimal AnnualizedReturn5Y { get; set; }
        public decimal AnnualizedReturn10Y { get; set; }
        public decimal AnnualizedReturnSinceInception { get; set; }
        public required string RiskLevel { get; set; }
        public decimal BenchmarkPerformance { get; set; }
    }

    public class AssetAllocation
    {
        public Int64 AllocationID { get; set; }
        public Int64 AccountNumber { get; set; }
        public required string ClientName { get; set; }
        public required string[] AssetClass { get; set; }
        public required decimal[] AllocationPercentage { get; set; }
        public required decimal[] MarketValue { get; set; }
        public required decimal[] ExpectedReturnPercentage { get; set; }
        public required decimal[] HistoricalPerformance3Y { get; set; }
        public required decimal[] HistoricalPerformance5Y { get; set; }
        public required string[] VolatilityRiskLevel { get; set; }
        public required decimal[] TargetAllocationPercentage { get; set; }
        public required decimal[] DeviationFromTarget { get; set; }
        public required bool[] RebalancingRequired { get; set; }
        public required string[] LiquidityLevel { get; set; }
        public required DateTime[] MaturityDate { get; set; }
        public required decimal[] DividendYield { get; set; }
        public required string[] AdvisorNotes { get; set; }
        public required string[] CurrencyType { get; set; }
    }

    public class FinalReport
    {
        public Int64 FinalReportID { get; set; }
        public Int64 AccountNumber { get; set; }
        public required string ReportTitle { get; set; }
        public required byte[] ReportPdf { get; set; }
        public DateTime ReportDate { get; set; }
        public Int64 PresetID { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public Int64 StatusCd { get; set; }
        public required string ReportIDs { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime LastUpdatedOn { get; set; }
        public required string Comments { get; set; }
        public required string ClientName { get; set; }
        public required string PresetName { get; set; }
    }

    public class FinalReportRequest
    {
        public Int64 AccountNumber { get; set; }
        public required string ReportTitle { get; set; }
        public DateTime ReportDate { get; set; }
        public Int64 PresetID { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public required string ReportIDs { get; set; }
        public string Comments { get; set; } = string.Empty;
    }

    public class UpdatePreset
    {
        public required int PresetId { get; set; }
        public required PresetInfo[] NewSelectedReports { get; set; }
        public required PresetInfo[] RemovedSelectedReports { get; set; }

    }

    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
