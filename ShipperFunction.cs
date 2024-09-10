using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SaleApp_Backend;
using SaleApp_Backend.Models;
using SaleApp_Backend.Services;

namespace sales_function_app
{
    public class ShipperFunction
    {
        private readonly ILogger<ShipperFunction> _logger;
        private readonly IShipperService _shipperService;
        private readonly IConfiguration _config;

        public ShipperFunction(ILogger<ShipperFunction> logger, IShipperService shipperService, IConfiguration config)
        {
            _logger = logger;
            _shipperService = shipperService;
            _config = config;
        }

        [Function("PostShipper")]
        public async Task<IActionResult> PostShipper(
            [HttpTrigger("post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            _logger.LogInformation("Processing a request to create a new shipper.");

            Shipper shipper = await req.ReadFromJsonAsync<Shipper>();

            if (shipper == null)
            {
                return new BadRequestObjectResult(new { Message = "Invalid input." });
            }

            try
            {
                // Check if the email ends with '.com'
                if (shipper.Email != null && shipper.Email.EndsWith(".com"))
                {
                    //await _shipperService.CreateShipperAsync(shipper);
                    //bool uploaded = await Helper.UploadBlob(_config, shipper);

                    // Update the record in Azure SQL Server
                    await UpdateRecordInSqlServer(shipper);

                    return new OkObjectResult(new { Message = "Record created and updated successfully.", InsertedRecord = shipper });
                }
                else
                {
                    return new BadRequestObjectResult(new { Message = "Invalid email. Record not updated." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the shipper.");
                return new BadRequestObjectResult(new { Message = "An error occurred.", Error = ex.Message });
            }
        }

        private async Task UpdateRecordInSqlServer(Shipper shipper)
        {
            try
            {
                string connectionString = "Server=tcp:salesappsprintserver.database.windows.net,1433;Initial Catalog=northwind-pubs;Persist Security Info=False;User ID=testuser;Password=password@123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=true;Connection Timeout=30;";


                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("UPDATE Shippers SET CompanyName = @CompanyName, Phone = @Phone, Email = @Email, Password = @Password WHERE ShipperId = @ShipperId", connection);
                    command.Parameters.AddWithValue("@ShipperId", shipper.ShipperId);
                    command.Parameters.AddWithValue("@CompanyName", shipper.CompanyName);
                    command.Parameters.AddWithValue("@Phone", shipper.Phone ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Email", shipper.Email);
                    command.Parameters.AddWithValue("@Password", shipper.Password ?? (object)DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the shipper.");
            }
        }
    }
}
