namespace ParkingVision.Vision;

public class VisionOptions
{
    /// <summary>Path to an ONNX export of a COCO-pretrained YOLO model. No training needed. See docs/03-camera-software.md.</summary>
    public string ModelPath { get; set; } = "models/yolo11s.onnx";
    public int InputSize { get; set; } = 640;
    public float MinScore { get; set; } = 0.40f;
    public float NmsIou { get; set; } = 0.50f;

    /// <summary>COCO ids: 1 bicycle, 2 car, 3 motorcycle, 5 bus, 7 truck.</summary>
    public int[] VehicleClassIds { get; set; } = { 1, 2, 3, 5, 7 };

    /// <summary>Share of the spot ROI that must be covered by the vehicle footprint to call the spot occupied.</summary>
    public double CoverageThreshold { get; set; } = 0.30;
    /// <summary>Confidence reported for "free" (no detection) - heuristic, there is no real score for absence.</summary>
    public double FreeConfidence { get; set; } = 0.85;

    public int IntervalSeconds { get; set; } = 3;
    /// <summary>A new state must hold this long before it is reported (anti-flicker for passing cars).</summary>
    public int StableSeconds { get; set; } = 30;
    /// <summary>Re-send unchanged state this often so the platform knows the camera is alive.</summary>
    public int KeepAliveSeconds { get; set; } = 60;
    /// <summary>No fresh frame for this long -> camera treated as offline (nothing is reported).</summary>
    public int FrameStaleSeconds { get; set; } = 30;
}

public readonly record struct Detection(float X1, float Y1, float X2, float Y2, float Score, int ClassId);

public readonly record struct SpotReading(bool Occupied, double Confidence, int? ClassId);

public static class CocoVehicle
{
    public static string? Name(int? id) => id switch
    {
        1 => "bicycle", 2 => "car", 3 => "motorcycle", 5 => "bus", 7 => "truck", _ => null
    };
}
