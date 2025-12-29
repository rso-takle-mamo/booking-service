// See https://aka.ms/new-console-template for more information

using Microsoft.EntityFrameworkCore;
using BookingService.Database;

var dbContext = new BookingDbContext();
var targetMigration = Environment.GetEnvironmentVariable("TARGET_MIGRATION");
var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
Console.WriteLine($"Applying pending migrations: [{string.Join(", ", pendingMigrations)}]");
await dbContext.Database.MigrateAsync(targetMigration);
Console.WriteLine("Finished applying pending migrations.");