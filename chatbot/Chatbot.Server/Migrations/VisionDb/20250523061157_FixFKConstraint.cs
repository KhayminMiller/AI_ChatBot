using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chatbot.Server.Migrations.VisionDb
{
    /// <inheritdoc />
    public partial class FixFKConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceCount",
                table: "FaceDetectionImages");

            migrationBuilder.RenameColumn(
                name: "OriginalImage",
                table: "FaceDetectionImages",
                newName: "ImageData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageData",
                table: "FaceDetectionImages",
                newName: "OriginalImage");

            migrationBuilder.AddColumn<int>(
                name: "FaceCount",
                table: "FaceDetectionImages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
