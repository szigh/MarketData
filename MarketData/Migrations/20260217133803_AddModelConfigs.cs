using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketData.Migrations
{
    /// <inheritdoc />
    public partial class AddModelConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelType",
                table: "Instruments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FlatConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstrumentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlatConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlatConfigs_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeanRevertingConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstrumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Mean = table.Column<double>(type: "REAL", nullable: false),
                    Kappa = table.Column<double>(type: "REAL", nullable: false),
                    Sigma = table.Column<double>(type: "REAL", nullable: false),
                    Dt = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeanRevertingConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeanRevertingConfigs_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RandomAdditiveWalkConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstrumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    WalkStepsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RandomAdditiveWalkConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RandomAdditiveWalkConfigs_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RandomMultiplicativeConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstrumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    StandardDeviation = table.Column<double>(type: "REAL", nullable: false),
                    Mean = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RandomMultiplicativeConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RandomMultiplicativeConfigs_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlatConfigs_InstrumentId",
                table: "FlatConfigs",
                column: "InstrumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeanRevertingConfigs_InstrumentId",
                table: "MeanRevertingConfigs",
                column: "InstrumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RandomAdditiveWalkConfigs_InstrumentId",
                table: "RandomAdditiveWalkConfigs",
                column: "InstrumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RandomMultiplicativeConfigs_InstrumentId",
                table: "RandomMultiplicativeConfigs",
                column: "InstrumentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlatConfigs");

            migrationBuilder.DropTable(
                name: "MeanRevertingConfigs");

            migrationBuilder.DropTable(
                name: "RandomAdditiveWalkConfigs");

            migrationBuilder.DropTable(
                name: "RandomMultiplicativeConfigs");

            migrationBuilder.DropColumn(
                name: "ModelType",
                table: "Instruments");
        }
    }
}
