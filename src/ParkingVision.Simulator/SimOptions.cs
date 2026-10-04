namespace ParkingVision.Simulator;

public enum Scenario { Normal, Busy, Quiet }

public class SimOptions
{
    /// <summary>Simulated minutes per real minute. 30 => a 90 min stay lasts ~3 real minutes. Tickets/validity are scaled the same way.</summary>
    public double Speed { get; set; } = 30;
    public double StartHour { get; set; } = DateTime.Now.Hour + DateTime.Now.Minute / 60.0;
    public Scenario Scenario { get; set; } = Scenario.Normal;

    public double MeanStayMinutes { get; set; } = 90;       // simulated minutes
    public double PayRate { get; set; } = 0.80;             // share of arrivals that buy a ticket
    public double TicketOverUnder { get; set; } = 0.25;     // ticket length = stay * U(1-x, 1+x) -> some expire early (violations)
    public decimal PricePerHour { get; set; } = 6m;

    /// <summary>Real seconds a camera needs to "notice" a change (stand-in for the vision debouncer).</summary>
    public double CameraDelaySeconds { get; set; } = 4;
    public double CameraKeepAliveSeconds { get; set; } = 20;
    /// <summary>Chance per keep-alive that a camera reports a flipped value for one cycle (detector glitch). 0 = perfect cameras.</summary>
    public double CameraNoise { get; set; } = 0.0;

    public HashSet<string> OfflineCameras { get; } = new(StringComparer.OrdinalIgnoreCase);
}
