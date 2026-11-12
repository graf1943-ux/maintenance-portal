using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RoadbikeCare.Api.Data;

public sealed class RoadbikeCareDesignTimeFactory : IDesignTimeDbContextFactory<RoadbikeCareDbContext>
{
    public RoadbikeCareDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddUserSecrets<RoadbikeCareDesignTimeFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("RoadbikeCare")
            ?? "Server=localhost;Database=RoadbikeCare;User Id=design-time;Password=design-time-only;TrustServerCertificate=True";
        DbContextOptions<RoadbikeCareDbContext> options = new DbContextOptionsBuilder<RoadbikeCareDbContext>()
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .Options;
        return new RoadbikeCareDbContext(options);
    }
}
