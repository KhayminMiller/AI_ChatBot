using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chatbot.Server.Migrations
{
    /// <inheritdoc />
    public partial class OCRdB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OcrResults",
                table: "OcrResults");

            migrationBuilder.RenameTable(
                name: "OcrResults",
                newName: "ORCdB");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ORCdB",
                table: "ORCdB",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ORCdB",
                table: "ORCdB");

            migrationBuilder.RenameTable(
                name: "ORCdB",
                newName: "OcrResults");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OcrResults",
                table: "OcrResults",
                column: "Id");
        }
    }
}
