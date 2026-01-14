using Microsoft.EntityFrameworkCore;

namespace Tests
{
    public class TestDbContextFactory
    {
        public static Dal.EsnDevContext CreateTestContext()
        {
            var options = new DbContextOptionsBuilder<Dal.EsnDevContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new Dal.EsnDevContext(options);
            context.Database.EnsureCreated();
            return context;
        }
    }
}
