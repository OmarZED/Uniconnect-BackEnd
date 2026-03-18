using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniConnect.Migrations
{
    /// <inheritdoc />
    public partial class AddPostsCommentsSubjectsInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunityInvitations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CommunityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InviterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InviteeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    InviteeEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityInvitations_AspNetUsers_InviteeId",
                        column: x => x.InviteeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityInvitations_AspNetUsers_InviterId",
                        column: x => x.InviterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommunityInvitations_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentGroupId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TeacherId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subjects_AspNetUsers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subjects_StudentGroups_StudentGroupId",
                        column: x => x.StudentGroupId,
                        principalTable: "StudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityInvitation_UniquePending",
                table: "CommunityInvitations",
                columns: new[] { "CommunityId", "InviteeEmail", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityInvitations_InviteeId",
                table: "CommunityInvitations",
                column: "InviteeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityInvitations_InviterId",
                table: "CommunityInvitations",
                column: "InviterId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_StudentGroupId",
                table: "Subjects",
                column: "StudentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_TeacherId",
                table: "Subjects",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityInvitations");

            migrationBuilder.DropTable(
                name: "Subjects");
        }
    }
}
