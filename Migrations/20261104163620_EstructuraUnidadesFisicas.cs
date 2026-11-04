using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantInventory.Migrations
{
    /// <inheritdoc />
    public partial class EstructuraUnidadesFisicas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnidadesFisicas",
                columns: table => new
                {
                    CodigoQR = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProductoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesFisicas", x => x.CodigoQR);
                    table.ForeignKey(
                        name: "FK_UnidadesFisicas_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesFisicas_ProductoId",
                table: "UnidadesFisicas",
                column: "ProductoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnidadesFisicas");
        }
    }
}
