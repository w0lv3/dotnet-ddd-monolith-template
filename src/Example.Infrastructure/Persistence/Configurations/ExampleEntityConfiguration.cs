using Example.Domain.Entities;
using Example.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Example.Infrastructure.Persistence.Configurations;

public sealed class ExampleEntityConfiguration : IEntityTypeConfiguration<ExampleEntity>
{
    public void Configure(EntityTypeBuilder<ExampleEntity> builder)
    {
        builder.ToTable("Examples");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.Name)
            .HasConversion(
                name => name.Value,
                value => ExampleName.Create(value))
            .HasMaxLength(ExampleName.MaximumLength)
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();
    }
}
