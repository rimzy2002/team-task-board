using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace TeamTaskBoard.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            // Oracle's SQL generator omits the scaffolded column Collation property.
            // Include the collation in the column type so CREATE TABLE emits it.
            const string exactText = " CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin";

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    firebase_uid = table.Column<string>(type: "varchar(128)" + exactText, maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_firebase_uid", "CHAR_LENGTH(TRIM(firebase_uid)) > 0");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    owner_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                    table.CheckConstraint("ck_projects_name", "CHAR_LENGTH(TRIM(name)) > 0");
                    table.ForeignKey(
                        name: "FK_projects_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    project_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "varchar(16)" + exactText, maxLength: 16, nullable: false),
                    priority = table.Column<string>(type: "varchar(8)" + exactText, maxLength: 8, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    assignee = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.id);
                    table.CheckConstraint("ck_tasks_priority", "priority IN ('Low', 'Medium', 'High')");
                    table.CheckConstraint("ck_tasks_status", "status IN ('ToDo', 'InProgress', 'Done')");
                    table.CheckConstraint("ck_tasks_title", "CHAR_LENGTH(TRIM(title)) > 0");
                    table.ForeignKey(
                        name: "FK_tasks_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_projects_owner_id",
                table: "projects",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_project_status_priority",
                table: "tasks",
                columns: new[] { "project_id", "status", "priority" });

            migrationBuilder.CreateIndex(
                name: "ux_users_firebase_uid",
                table: "users",
                column: "firebase_uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
