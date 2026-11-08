using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AccountDeposits.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "operations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reference = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    rate_per_eur = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operations", x => x.id);
                    table.CheckConstraint("ck_operations_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_operations_balance_after_non_negative", "balance_after >= 0");
                    table.CheckConstraint("ck_operations_fee_non_negative", "fee >= 0");
                    table.CheckConstraint("ck_operations_rate_per_eur_positive", "rate_per_eur > 0");
                    table.CheckConstraint("ck_operations_type", "type IN ('Deposit', 'Withdrawal', 'TransferOut', 'TransferIn')");
                    table.ForeignKey(
                        name: "fk_operations_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_operations_account_id_occurred_at_id",
                table: "operations",
                columns: new[] { "account_id", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_operations_reference_account_id",
                table: "operations",
                columns: new[] { "reference", "account_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "operations");
        }
    }
}
