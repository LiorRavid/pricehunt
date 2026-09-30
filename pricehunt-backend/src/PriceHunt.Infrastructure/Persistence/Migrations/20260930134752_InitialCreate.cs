using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PriceHunt.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Searches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Origin = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OriginNormalized = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Destination = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DestinationNormalized = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ShipDateFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ShipDateTo = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Searches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchSuppliers",
                columns: table => new
                {
                    SearchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchSuppliers", x => new { x.SearchId, x.SupplierId });
                    table.ForeignKey(
                        name: "FK_SearchSuppliers_Searches_SearchId",
                        column: x => x.SearchId,
                        principalTable: "Searches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SearchSuppliers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SearchId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    PriceMinorUnits = table.Column<long>(type: "INTEGER", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierResponses_Searches_SearchId",
                        column: x => x.SearchId,
                        principalTable: "Searches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierResponses_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Searches_DestinationNormalized",
                table: "Searches",
                column: "DestinationNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_Searches_OriginNormalized",
                table: "Searches",
                column: "OriginNormalized");

            migrationBuilder.CreateIndex(
                name: "IX_Searches_Status",
                table: "Searches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SearchSuppliers_SupplierId",
                table: "SearchSuppliers",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierResponses_Outcome_ReceivedAt",
                table: "SupplierResponses",
                columns: new[] { "Outcome", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierResponses_PriceMinorUnits",
                table: "SupplierResponses",
                column: "PriceMinorUnits");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierResponses_ReceivedAt",
                table: "SupplierResponses",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierResponses_ResponseTimeMs",
                table: "SupplierResponses",
                column: "ResponseTimeMs");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierResponses_SearchId_SupplierId",
                table: "SupplierResponses",
                columns: new[] { "SearchId", "SupplierId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierResponses_SupplierId_ReceivedAt",
                table: "SupplierResponses",
                columns: new[] { "SupplierId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchSuppliers");

            migrationBuilder.DropTable(
                name: "SupplierResponses");

            migrationBuilder.DropTable(
                name: "Searches");

            migrationBuilder.DropTable(
                name: "Suppliers");
        }
    }
}
