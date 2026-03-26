using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectJoinCodeAndCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StudentGroupId",
                table: "Subjects",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "JoinCode",
                table: "Subjects",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubjectId",
                table: "Communities",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Communities_SubjectId",
                table: "Communities",
                column: "SubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Communities_Subjects_SubjectId",
                table: "Communities",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Communities_Subjects_SubjectId",
                table: "Communities");

            migrationBuilder.DropIndex(
                name: "IX_Communities_SubjectId",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "JoinCode",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Communities");

            migrationBuilder.AlterColumn<string>(
                name: "StudentGroupId",
                table: "Subjects",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
