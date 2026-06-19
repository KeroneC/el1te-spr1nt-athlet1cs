using El1teSpr1ntTrack.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace El1teSpr1ntTrack.Infrastructure.Data.Configurations;

internal static class CmsConfigurationExtensions
{
    public static void ConfigureCmsEntity<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string tableName)
        where TEntity : CmsEntityBase
    {
        builder.ToTable(tableName);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(entity => entity.UpdatedAtUtc);
    }
}
