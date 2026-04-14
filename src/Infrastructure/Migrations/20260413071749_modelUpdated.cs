using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliImport.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class modelUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExtractionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FileBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    CurrentChunk = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalChunks = table.Column<int>(type: "int", nullable: false),
                    ProcessedChunks = table.Column<int>(type: "int", nullable: false),
                    ExtractionRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractionJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractionJobs_ExtractionRecords_ExtractionRecordId",
                        column: x => x.ExtractionRecordId,
                        principalTable: "ExtractionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionJobs_CreatedAt",
                table: "ExtractionJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionJobs_ExtractionRecordId",
                table: "ExtractionJobs",
                column: "ExtractionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractionJobs_Status",
                table: "ExtractionJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExtractionJobs");
        }
    }
}
