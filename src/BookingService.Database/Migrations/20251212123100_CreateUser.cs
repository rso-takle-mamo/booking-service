using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using BookingService.Database;

#nullable disable

namespace BookingService.Database.Migrations
{
    [DbContext(typeof(BookingDbContext))]
    [Migration("20251212123100_CreateUser")]
    internal class CreateUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var userName = EnvironmentVariables.GetRequiredVariable("DATABASE_USERNAME");
            var password = EnvironmentVariables.GetRequiredVariable("DATABASE_PASSWORD");
            // migrationBuilder.Sql($"DO $$ BEGIN IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '{userName}') THEN CREATE USER {userName} WITH PASSWORD '{password}'; END IF; END $$;");
            migrationBuilder.Sql($"CREATE USER {userName} WITH PASSWORD '{password}';");
            migrationBuilder.Sql($"ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO {userName};");
            migrationBuilder.Sql($"GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO {userName};");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var userName = EnvironmentVariables.GetRequiredVariable("DATABASE_USERNAME");
            migrationBuilder.Sql($"DROP OWNED BY {userName};");
            migrationBuilder.Sql($"DROP USER {userName};");
        }
    }
}