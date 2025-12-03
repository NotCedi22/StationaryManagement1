using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StationaryManagement1.Migrations
{
    /// <inheritdoc />
    public partial class CreateFrequentItemsView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        CREATE VIEW vw_FrequentItems AS
        SELECT 
            si.ItemId,
            si.ItemName,
            SUM(ri.Quantity) AS TotalRequested,
            COUNT(DISTINCT sr.EmployeeId) AS RequestorCount,
            SUM(ri.Quantity * ri.UnitCost) AS TotalSpent
        FROM StationeryItems si
        JOIN RequestItems ri ON si.ItemId = ri.ItemId
        JOIN StationeryRequests sr ON ri.RequestId = sr.RequestId
        GROUP BY si.ItemId, si.ItemName
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS vw_FrequentItems");
        }

    }
}
