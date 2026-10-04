namespace ParkingVision.Core;

public enum SpotType { Car = 0, Motorcycle = 1, Disabled = 2, Electric = 3, Loading = 4 }

public enum OccupancyStatus { Unknown = 0, Free = 1, Occupied = 2 }

/// <summary>Where the current SpotState came from.</summary>
public enum DataSource { None = 0, Camera = 1, Estimated = 2, Manual = 3 }

public enum CameraStatus { Unknown = 0, Online = 1, Offline = 2 }

/// <summary>How trustworthy the zone-level availability number is.</summary>
public enum AvailabilityConfidence { NoData = 0, Estimated = 1, Mixed = 2, Live = 3 }
