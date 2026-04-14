using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliImport.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExtractionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawAiResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Vendor = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    IsLineItemSumValid = table.Column<bool>(type: "bit", nullable: false),
                    LineItemSum = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProcessingMs = table.Column<long>(type: "bigint", nullable: false),
                    ModelUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtractionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineItems_ExtractionRecords_ExtractionId",
                        column: x => x.ExtractionId,
                        principalTable: "ExtractionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionRecords_CreatedAt",
                table: "ExtractionRecords",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionRecords_Status",
                table: "ExtractionRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LineItems_ExtractionId",
                table: "LineItems",
                column: "ExtractionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineItems");

            migrationBuilder.DropTable(
                name: "ExtractionRecords");
        }
    }
}
