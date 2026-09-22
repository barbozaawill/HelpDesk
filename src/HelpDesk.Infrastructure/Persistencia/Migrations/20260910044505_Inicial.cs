using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpDesk.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    Perfil = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chamados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titulo = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    Prioridade = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    CategoriaId = table.Column<int>(type: "integer", nullable: false),
                    SolicitanteId = table.Column<int>(type: "integer", nullable: false),
                    AtendenteId = table.Column<int>(type: "integer", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PrazoPrimeiraResposta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PrazoResolucao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PrimeiraRespostaRegistrada = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chamados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chamados_categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamados_usuarios_AtendenteId",
                        column: x => x.AtendenteId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamados_usuarios_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "interacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChamadoId = table.Column<int>(type: "integer", nullable: false),
                    AutorId = table.Column<int>(type: "integer", nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Interna = table.Column<bool>(type: "boolean", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_interacoes_chamados_ChamadoId",
                        column: x => x.ChamadoId,
                        principalTable: "chamados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_interacoes_usuarios_AutorId",
                        column: x => x.AutorId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_Nome",
                table: "categorias",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chamados_AtendenteId",
                table: "chamados",
                column: "AtendenteId");

            migrationBuilder.CreateIndex(
                name: "IX_chamados_CategoriaId",
                table: "chamados",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_chamados_SolicitanteId",
                table: "chamados",
                column: "SolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_chamados_Status",
                table: "chamados",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_interacoes_AutorId",
                table: "interacoes",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_interacoes_ChamadoId",
                table: "interacoes",
                column: "ChamadoId");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Email",
                table: "usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "interacoes");

            migrationBuilder.DropTable(
                name: "chamados");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
