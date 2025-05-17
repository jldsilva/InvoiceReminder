using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace InvoiceReminder.Data.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class Initial_Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.EnsureSchema(
                name: "invoice_reminder");

            _ = migrationBuilder.CreateTable(
                name: "user",
                schema: "invoice_reminder",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    telegram_chat_id = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "date", nullable: false),
                    updated_at = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_user", x => x.id));

            _ = migrationBuilder.CreateTable(
                name: "invoice",
                schema: "invoice_reminder",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    beneficiary = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    barcode = table.Column<string>(type: "text", nullable: false),
                    due_date = table.Column<DateTime>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "date", nullable: false),
                    updated_at = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_invoice", x => x.id);
                    _ = table.ForeignKey(
                        name: "FK_invoice_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "invoice_reminder",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "job_schedule",
                schema: "invoice_reminder",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cron_expression = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "date", nullable: false),
                    updated_at = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_job_schedule", x => x.id);
                    _ = table.ForeignKey(
                        name: "FK_job_schedule_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "invoice_reminder",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "scan_email_definition",
                schema: "invoice_reminder",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_type = table.Column<int>(type: "integer", nullable: false),
                    beneficiary = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sender_email_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    attachment_filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "date", nullable: false),
                    updated_at = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_scan_email_definition", x => x.id);
                    _ = table.ForeignKey(
                        name: "FK_scan_email_definition_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "invoice_reminder",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateIndex(
                name: "IX_invoice_user_id",
                schema: "invoice_reminder",
                table: "invoice",
                column: "user_id");

            _ = migrationBuilder.CreateIndex(
                name: "IX_job_schedule_user_id",
                schema: "invoice_reminder",
                table: "job_schedule",
                column: "user_id");

            _ = migrationBuilder.CreateIndex(
                name: "IX_scan_email_definition_user_id",
                schema: "invoice_reminder",
                table: "scan_email_definition",
                column: "user_id");

            _ = migrationBuilder.CreateIndex(
                name: "idx_user_email",
                schema: "invoice_reminder",
                table: "user",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropTable(
                name: "invoice",
                schema: "invoice_reminder");

            _ = migrationBuilder.DropTable(
                name: "job_schedule",
                schema: "invoice_reminder");

            _ = migrationBuilder.DropTable(
                name: "scan_email_definition",
                schema: "invoice_reminder");

            _ = migrationBuilder.DropTable(
                name: "user",
                schema: "invoice_reminder");
        }
    }
}
