using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace El1teSpr1ntTrack.Infrastructure.Data;

public sealed class El1teDbContextFactory : IDesignTimeDbContextFactory<El1teDbContext>
{
    public El1teDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<El1teDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=El1teSpr1ntTrack_DesignTime;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new El1teDbContext(optionsBuilder.Options);
    }
}
