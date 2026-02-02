using Microsoft.EntityFrameworkCore;

namespace MTU.Data
{
    public static class DbPatcher
    {
        public static async Task ApplyPatchesAsync(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<MTUSocialDbContext>();
                try
                {
                    // Check and Add SenderId column if missing
                    await context.Database.ExecuteSqlRawAsync(@"
                        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Notifications')
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Notifications' AND COLUMN_NAME = 'SenderId')
                            BEGIN
                                ALTER TABLE Notifications ADD SenderId int NULL;
                                ALTER TABLE Notifications ADD CONSTRAINT FK_Notifications_Users_SenderId FOREIGN KEY (SenderId) REFERENCES Users(UserId);
                            END
                        END
                    ");

                    // Check and Add FileUrl/FileName columns if might be missing (Patch 2)
                    await context.Database.ExecuteSqlRawAsync(@"
                        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Posts')
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Posts' AND COLUMN_NAME = 'FileUrl')
                            BEGIN
                                ALTER TABLE Posts ADD FileUrl nvarchar(255) NULL;
                                ALTER TABLE Posts ADD FileName nvarchar(255) NULL;
                            END
                        END
                    ");
                }
                catch (Exception ex)
                {
                    // Log error but don't crash app, allows debugging
                    Console.WriteLine($"Error applying DB patch: {ex.Message}");
                }
            }
        }
    }
}
