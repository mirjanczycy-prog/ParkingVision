namespace ParkingVision.Core;

// Topology: Zone 1-N Spot. Parkomat N-M Spot (ParkomatSpot). Camera N-M Spot (CameraSpot, with ROI).
// Camera <-> Parkomat is DERIVED through Spot - never stored directly. See docs/02-data-model.md.

public class Zone
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public double CenterLat { get; set; }
    public double CenterLon { get; set; }
    public string? TariffInfo { get; set; }
    public List<Spot> Spots { get; set; } = new();
    public List<Parkomat> Parkomats { get; set; } = new();
}

public class Spot
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int ZoneId { get; set; }
    public Zone Zone { get; set; } = null!;
    public string? StreetName { get; set; }
    public SpotType Type { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public List<ParkomatSpot> Parkomats { get; set; } = new();
    public List<CameraSpot> Cameras { get; set; } = new();
    public SpotState? State { get; set; }
}

public class Parkomat
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public double Lat { get; set; }
    public double Lon { get; set; }
    public int ZoneId { get; set; }
    public Zone Zone { get; set; } = null!;
    public List<ParkomatSpot> Spots { get; set; } = new();
}

public class ParkomatSpot
{
    public int ParkomatId { get; set; }
    public Parkomat Parkomat { get; set; } = null!;
    public int SpotId { get; set; }
    public Spot Spot { get; set; } = null!;
}

public class Camera
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public double Lat { get; set; }
    public double Lon { get; set; }
    /// <summary>rtsp://..., a video file path, or "env:VAR_NAME" (resolved from environment so secrets stay out of the DB).</summary>
    public string? StreamUrl { get; set; }
    public bool IsSimulated { get; set; }
    public bool Enabled { get; set; } = true;
    public CameraStatus Status { get; set; }
    public DateTime? LastHeartbeatUtc { get; set; }
    public List<CameraSpot> Spots { get; set; } = new();
}

public class CameraSpot
{
    public int CameraId { get; set; }
    public Camera Camera { get; set; } = null!;
    public int SpotId { get; set; }
    public Spot Spot { get; set; } = null!;
    /// <summary>Polygon in NORMALIZED image coordinates (0..1), JSON: [[x,y],[x,y],...]. Resolution independent.</summary>
    public string RoiJson { get; set; } = "[]";
    /// <summary>Vote weight when several cameras cover the same spot (e.g. better angle = higher weight).</summary>
    public double Weight { get; set; } = 1.0;
    public bool Enabled { get; set; } = true;
}

/// <summary>Raw (debounced) reading from one camera about one spot. Append-only, purged after retention window.</summary>
public class SpotObservation
{
    public long Id { get; set; }
    public int CameraId { get; set; }
    public int SpotId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public bool Occupied { get; set; }
    public double Confidence { get; set; }
    public string? VehicleClass { get; set; }
}

/// <summary>Current fused state of a spot. One row per spot.</summary>
public class SpotState
{
    public int SpotId { get; set; }
    public Spot Spot { get; set; } = null!;
    public OccupancyStatus Status { get; set; }
    public double Confidence { get; set; }
    public DataSource Source { get; set; }
    public DateTime SinceUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

/// <summary>Ticket issued by a parkomat. Usually does NOT know the exact spot (SpotId nullable).</summary>
public class ParkomatTicket
{
    public long Id { get; set; }
    public int ParkomatId { get; set; }
    public int ZoneId { get; set; }
    public string ExternalTicketId { get; set; } = "";
    public DateTime IssuedUtc { get; set; }
    public DateTime ValidToUtc { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Optional salted hash of plate (privacy). Never store plain plates in this prototype.</summary>
    public string? PlateHash { get; set; }
    public int? SpotId { get; set; }
}
