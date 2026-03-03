using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeRequestAndPostUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OriginalPostId",
                table: "Posts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportReason",
                table: "Posts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportedByUserId",
                table: "Posts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BadgeRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedType = table.Column<int>(type: "int", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CertificateUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BadgeRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_OriginalPostId",
                table: "Posts",
                column: "OriginalPostId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeRequest_UserId_Status",
                table: "BadgeRequests",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_OriginalPostId",
                table: "Posts",
                column: "OriginalPostId",
                principalTable: "Posts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Posts_OriginalPostId",
                table: "Posts");

            migrationBuilder.DropTable(
                name: "BadgeRequests");

            migrationBuilder.DropIndex(
                name: "IX_Posts_OriginalPostId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "OriginalPostId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ReportReason",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ReportedByUserId",
                table: "Posts");
        }
    }
}
