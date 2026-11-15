using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RoadbikeCare.Api.Data;

#nullable disable

namespace RoadbikeCare.Api.Data.Migrations;

[DbContext(typeof(RoadbikeCareDbContext))]
[Migration("20261116000000_AddQrAccess")]
partial class AddQrAccess
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0").HasAnnotation("Relational:MaxIdentifierLength", 128);
        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);
        modelBuilder.Entity("RoadbikeCare.Api.Models.QrAccessGrant", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int");
            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));
            b.Property<int>("CustomerId").HasColumnType("int");
            b.Property<DateTime>("ExpiresAtUtc").HasColumnType("datetime2");
            b.Property<DateTime?>("RedeemedAtUtc").HasColumnType("datetime2");
            b.Property<string>("TokenHash").IsRequired().HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.HasKey("Id"); b.HasIndex("ExpiresAtUtc"); b.HasIndex("TokenHash").IsUnique();
            b.ToTable("QrAccessGrants");
        });
        modelBuilder.Entity("RoadbikeCare.Api.Models.QrAccessSession", b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int");
            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));
            b.Property<int>("CustomerId").HasColumnType("int");
            b.Property<DateTime>("ExpiresAtUtc").HasColumnType("datetime2");
            b.Property<string>("TokenHash").IsRequired().HasMaxLength(128).HasColumnType("nvarchar(128)");
            b.HasKey("Id"); b.HasIndex("ExpiresAtUtc"); b.HasIndex("TokenHash").IsUnique();
            b.ToTable("QrAccessSessions");
        });
    }
}
