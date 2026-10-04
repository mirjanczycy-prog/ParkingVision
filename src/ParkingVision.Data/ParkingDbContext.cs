using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ParkingVision.Core;

namespace ParkingVision.Data;

public class ParkingDbContext(DbContextOptions<ParkingDbContext> options) : DbContext(options)
{
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Spot> Spots => Set<Spot>();
    public DbSet<Parkomat> Parkomats => Set<Parkomat>();
    public DbSet<ParkomatSpot> ParkomatSpots => Set<ParkomatSpot>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<CameraSpot> CameraSpots => Set<CameraSpot>();
    public DbSet<SpotObservation> SpotObservations => Set<SpotObservation>();
    public DbSet<SpotState> SpotStates => Set<SpotState>();
    public DbSet<ParkomatTicket> ParkomatTickets => Set<ParkomatTicket>();

    protected override void ConfigureConventions(ModelConfigurationBuilder cb)
    {
        // SQLite loses DateTime.Kind; force UTC on the way out so JSON gets a trailing "Z".
        cb.Properties<DateTime>().HaveConversion<UtcConverter>();
        cb.Properties<DateTime?>().HaveConversion<NullableUtcConverter>();
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Zone>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Spot>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Parkomat>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Camera>().HasIndex(x => x.Code).IsUnique();

        b.Entity<Spot>().HasOne(s => s.Zone).WithMany(z => z.Spots).HasForeignKey(s => s.ZoneId);
        b.Entity<Parkomat>().HasOne(p => p.Zone).WithMany(z => z.Parkomats).HasForeignKey(p => p.ZoneId);

        b.Entity<ParkomatSpot>().HasKey(x => new { x.ParkomatId, x.SpotId });
        b.Entity<ParkomatSpot>().HasOne(x => x.Parkomat).WithMany(p => p.Spots).HasForeignKey(x => x.ParkomatId);
        b.Entity<ParkomatSpot>().HasOne(x => x.Spot).WithMany(s => s.Parkomats).HasForeignKey(x => x.SpotId);

        b.Entity<CameraSpot>().HasKey(x => new { x.CameraId, x.SpotId });
        b.Entity<CameraSpot>().HasOne(x => x.Camera).WithMany(c => c.Spots).HasForeignKey(x => x.CameraId);
        b.Entity<CameraSpot>().HasOne(x => x.Spot).WithMany(s => s.Cameras).HasForeignKey(x => x.SpotId);

        b.Entity<SpotState>().HasKey(x => x.SpotId);
        b.Entity<SpotState>().HasOne(x => x.Spot).WithOne(s => s.State).HasForeignKey<SpotState>(x => x.SpotId);

        b.Entity<SpotObservation>().HasIndex(x => new { x.SpotId, x.CameraId, x.TimestampUtc });
        b.Entity<SpotObservation>().HasIndex(x => x.TimestampUtc);

        b.Entity<ParkomatTicket>().HasIndex(x => new { x.ParkomatId, x.ExternalTicketId }).IsUnique();
        b.Entity<ParkomatTicket>().HasIndex(x => new { x.ZoneId, x.ValidToUtc });
    }
}

public class UtcConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

public class NullableUtcConverter() : ValueConverter<DateTime?, DateTime?>(
    v => v.HasValue ? (v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : v,
    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
