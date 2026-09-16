using System;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaultDesk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaultTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Registration = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    VehicleMake = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    VehicleModel = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    VehicleVariant = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    VehicleYear = table.Column<int>(type: "int", nullable: false),
                    VehicleEngineSizeCc = table.Column<int>(type: "int", nullable: true),
                    VehicleFuelType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomerContact = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TriageTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    TriageCategory = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TriageSeverity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TriageSafeToDrive = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TriageSymptoms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriageLikelySystems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriageAdviserQuestions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriageCustomerSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaultTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Investigations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportMarkdown = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AiProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AiModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    DurationTicks = table.Column<long>(type: "bigint", nullable: false),
                    WebSearchUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Investigations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Investigations_FaultTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "FaultTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketEmbeddings",
                columns: table => new
                {
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Vector = table.Column<SqlVector<float>>(type: "vector(1536)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketEmbeddings", x => x.TicketId);
                    table.ForeignKey(
                        name: "FK_TicketEmbeddings_FaultTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "FaultTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaultTickets_CreatedAt",
                table: "FaultTickets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FaultTickets_Reference",
                table: "FaultTickets",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaultTickets_Registration",
                table: "FaultTickets",
                column: "Registration");

            migrationBuilder.CreateIndex(
                name: "IX_FaultTickets_Status",
                table: "FaultTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Investigations_TicketId",
                table: "Investigations",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEmbeddings_ModelId",
                table: "TicketEmbeddings",
                column: "ModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Investigations");

            migrationBuilder.DropTable(
                name: "TicketEmbeddings");

            migrationBuilder.DropTable(
                name: "FaultTickets");
        }
    }
}
