using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketData.Migrations
{
    /// <inheritdoc />
    public partial class ChangeIntervalToMilliseconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TickIntervalSeconds",
                table: "Instruments",
                newName: "TickIntervalMillieconds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TickIntervalMillieconds",
                table: "Instruments",
                newName: "TickIntervalSeconds");
        }
    }
}
