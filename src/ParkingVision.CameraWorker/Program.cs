using ParkingVision.CameraWorker;
using ParkingVision.Core;
using ParkingVision.Data;
using ParkingVision.Vision;

// Force TCP for RTSP: UDP drops packets on Wi-Fi/LTE links and produces smeared frames that confuse the detector.
Environment.SetEnvironmentVariable("OPENCV_FFMPEG_CAPTURE_OPTIONS", "rtsp_transport;tcp");

var builder = Host.CreateApplicationBuilder(args);

var vision = builder.Configuration.GetSection("Vision").Get<VisionOptions>() ?? new VisionOptions();
if (!Path.IsPathRooted(vision.ModelPath))
    vision.ModelPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, vision.ModelPath));
var settings = builder.Configuration.GetSection("Parking").Get<ParkingSettings>() ?? new ParkingSettings();

builder.Services.AddParkingData(builder.Configuration["Database:Path"], settings);
builder.Services.AddSingleton(vision);
builder.Services.AddSingleton<YoloDetector>();
builder.Services.AddSingleton<SpotEvaluator>();
builder.Services.AddHostedService<CameraSupervisor>();

var host = builder.Build();
await DbSetup.EnsureDatabaseAsync(host.Services);
await host.RunAsync();
