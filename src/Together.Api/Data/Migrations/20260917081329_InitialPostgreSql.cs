using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Together.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Adults = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.Id);
                    table.CheckConstraint("ck_trips_adults", "\"Adults\" BETWEEN 1 AND 10");
                    table.CheckConstraint("ck_trips_dates", "\"EndDate\" > \"StartDate\"");
                    table.CheckConstraint("ck_trips_name", "char_length(\"Name\") BETWEEN 1 AND 100");
                    table.CheckConstraint("ck_trips_revision", "\"Revision\" >= 0");
                    table.ForeignKey(
                        name: "FK_trips_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trip_children",
                columns: table => new
                {
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_children", x => new { x.TripId, x.Position });
                    table.CheckConstraint("ck_trip_children_age", "\"Age\" BETWEEN 0 AND 17");
                    table.ForeignKey(
                        name: "FK_trip_children_trips_TripId",
                        column: x => x.TripId,
                        principalTable: "trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trip_selected_criteria",
                columns: table => new
                {
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    Criterion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_selected_criteria", x => new { x.TripId, x.Criterion });
                    table.CheckConstraint("ck_selected_criteria_value", "\"Criterion\" IN ('budget','travel','transfers','kitchen','crib','playground','distance')");
                    table.ForeignKey(
                        name: "FK_trip_selected_criteria_trips_TripId",
                        column: x => x.TripId,
                        principalTable: "trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Destination = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Accommodation = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    RoadDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TravelMinutes = table.Column<int>(type: "integer", nullable: true),
                    Transfers = table.Column<int>(type: "integer", nullable: true),
                    Kitchen = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Crib = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Playground = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DistanceMeters = table.Column<int>(type: "integer", nullable: true),
                    DistanceTarget = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NeedsBudgetReview = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variants", x => x.Id);
                    table.UniqueConstraint("AK_variants_TripId_Id", x => new { x.TripId, x.Id });
                    table.CheckConstraint("ck_variants_amenities", "\"Kitchen\" IN ('yes','no','unknown') AND \"Crib\" IN ('yes','no','unknown') AND \"Playground\" IN ('yes','no','unknown')");
                    table.CheckConstraint("ck_variants_distance", "\"DistanceMeters\" IS NULL OR \"DistanceMeters\" BETWEEN 0 AND 1000000");
                    table.CheckConstraint("ck_variants_revision", "\"Revision\" >= 0");
                    table.CheckConstraint("ck_variants_transfers", "\"Transfers\" IS NULL OR \"Transfers\" BETWEEN 0 AND 20");
                    table.CheckConstraint("ck_variants_travel_minutes", "\"TravelMinutes\" IS NULL OR \"TravelMinutes\" BETWEEN 0 AND 100000");
                    table.ForeignKey(
                        name: "FK_variants_trips_TripId",
                        column: x => x.TripId,
                        principalTable: "trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trip_selected_variants",
                columns: table => new
                {
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_selected_variants", x => new { x.TripId, x.VariantId });
                    table.ForeignKey(
                        name: "FK_trip_selected_variants_trips_TripId",
                        column: x => x.TripId,
                        principalTable: "trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trip_selected_variants_variants_TripId_VariantId",
                        columns: x => new { x.TripId, x.VariantId },
                        principalTable: "variants",
                        principalColumns: new[] { "TripId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variant_expenses",
                columns: table => new
                {
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AmountKopecks = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variant_expenses", x => new { x.VariantId, x.Category });
                    table.CheckConstraint("ck_variant_expenses_amount", "\"AmountKopecks\" IS NULL OR \"AmountKopecks\" BETWEEN 0 AND 10000000000");
                    table.CheckConstraint("ck_variant_expenses_category", "\"Category\" IN ('travel','accommodation','food','localTransport','entertainment','other')");
                    table.ForeignKey(
                        name: "FK_variant_expenses_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trip_selected_criteria_TripId_Position",
                table: "trip_selected_criteria",
                columns: new[] { "TripId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trip_selected_variants_TripId_Position",
                table: "trip_selected_variants",
                columns: new[] { "TripId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trips_OwnerId_UpdatedAt",
                table: "trips",
                columns: new[] { "OwnerId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "trip_children");

            migrationBuilder.DropTable(
                name: "trip_selected_criteria");

            migrationBuilder.DropTable(
                name: "trip_selected_variants");

            migrationBuilder.DropTable(
                name: "variant_expenses");

            migrationBuilder.DropTable(
                name: "variants");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
