using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Control",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ControlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlTitle = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    ControlDescription = table.Column<string>(type: "nvarchar(1000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Control", x => x.Id);
                    table.UniqueConstraint("AK_Control_ControlId", x => x.ControlId);
                });

            migrationBuilder.CreateTable(
                name: "Risk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiskTitle = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    RiskDescription = table.Column<string>(type: "nvarchar(1000)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Risk", x => x.Id);
                    table.UniqueConstraint("AK_Risk_RiskId", x => x.RiskId);
                });

            migrationBuilder.CreateTable(
                name: "RiskControl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskControlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlMethod = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskControl", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskControl_Control_ControlId",
                        column: x => x.ControlId,
                        principalTable: "Control",
                        principalColumn: "ControlId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RiskControl_Risk_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risk",
                        principalColumn: "RiskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiskControl_ControlId",
                table: "RiskControl",
                column: "ControlId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskControl_RiskId",
                table: "RiskControl",
                column: "RiskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiskControl");

            migrationBuilder.DropTable(
                name: "Control");

            migrationBuilder.DropTable(
                name: "Risk");
        }
    }
}
