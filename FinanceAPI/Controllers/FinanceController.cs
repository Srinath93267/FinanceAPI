﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;

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
        private readonly string database = string.Empty;
        private readonly string connectionString = string.Empty;
        private readonly string _secretApiKey = string.Empty;
        #endregion
        public FinanceController(IConfiguration configuration, ILogger<FinanceController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            database = _configuration["Database"] ?? string.Empty;
            _secretApiKey = _configuration["ApiSettings:SecretKey"] ?? string.Empty;
            connectionString = String.Format("Server=localhost;Database={0};Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;", database);
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
                                         String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                     );
                        return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                                       String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
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
                                     String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
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
                                     String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
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
                                     String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
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
                                     String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                               );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                finally
                {
                    accountDetailTable?.Dispose();
                }
            }

            return accountList;
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
                                     String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
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
                                     String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                 );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                   String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
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
                            //command2.Dispose();
                        }
                        catch (SqlException ex)
                        {
                            if (connection.State == ConnectionState.Open)
                            {
                                connection.Close();
                            }
                            _logger.LogError(
                                String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
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
                                    String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                                    String.Format("An unexpected error occurred.\n Error Details:\n{0}", ex.Message)
                                );
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
            finally
            {
                newPresetInsertedTable?.Dispose();
                newPresetReportsInsertedTable?.Dispose();
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
                                                String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                            );
                            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                        }
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status404NotFound, "The Preset was not found");
                    }
                    return StatusCode(StatusCodes.Status200OK, "The Preset has been deleted sucessfully");
                }
                catch (SqlException ex)
                {
                    _logger.LogError(
                                        String.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                    );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                                        String.Format("An unexpected error occurred.\n Error Details:\n{0}", ex.Message)
                                    );
                    return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
                }
            }
        }

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
}
