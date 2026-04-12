using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartShip.IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleRolePerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
;WITH Ranked AS
(
    SELECT [UserId], [RoleId],
           ROW_NUMBER() OVER (PARTITION BY [UserId] ORDER BY [AssignedAt] DESC, [RoleId] DESC) AS rn
    FROM [UserRoles]
)
DELETE FROM [UserRoles]
WHERE EXISTS
(
    SELECT 1
    FROM Ranked r
    WHERE r.[UserId] = [UserRoles].[UserId]
      AND r.[RoleId] = [UserRoles].[RoleId]
      AND r.rn > 1
);
");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles");
        }
    }
}
