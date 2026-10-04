namespace ParkingVision.Core;

// ---------- Ingest contracts (edge -> platform). Spots/cameras/parkomats are referenced by stable CODE. ----------
public record ObservationItemDto(string SpotCode, bool Occupied, double Confidence, string? VehicleClass = null);
public record ObservationBatchDto(string CameraCode, DateTime TimestampUtc, List<ObservationItemDto> Items);
public record ParkomatTicketDto(string ParkomatCode, string ExternalTicketId, DateTime IssuedUtc, DateTime ValidToUtc, decimal Amount, string? PlateHash = null);

// ---------- Read contracts (platform -> apps) ----------
public record ZoneAvailabilityDto(
    int ZoneId, string Code, string Name, string City,
    double Lat, double Lon, double? DistanceM,
    int Total, int FreeLive, int OccupiedLive, int WithoutLiveData,
    int EstimatedFreeAmongUncovered, int FreeTotalEstimate,
    int ActiveTickets, AvailabilityConfidence Confidence, DateTime? UpdatedUtc);

public record SpotDto(
    int Id, string Code, string? StreetName, SpotType Type, double Lat, double Lon,
    OccupancyStatus Status, double Confidence, DataSource Source, DateTime? SinceUtc, DateTime? UpdatedUtc);

public record ParkomatDto(int Id, string Code, string Name, double Lat, double Lon, int SpotCount, double? DistanceM);

public record ZoneDetailDto(ZoneAvailabilityDto Availability, string? TariffInfo, List<SpotDto> Spots, List<ParkomatDto> Parkomats);

// ---------- Admin / topology views ----------
public record CameraShareDto(string CameraCode, int Spots);
public record ParkomatCoverageDto(string ParkomatCode, string Name, int SpotCount, int SpotsWithoutCamera, List<CameraShareDto> Cameras);
public record CameraInfoDto(string Code, string Name, CameraStatus Status, DateTime? LastHeartbeatUtc, bool IsSimulated,
                            List<string> SpotCodes, List<string> ParkomatCodes);
