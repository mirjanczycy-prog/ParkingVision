namespace ParkingVision.Vision;

/// <summary>
/// Anti-flicker: a raw reading must differ from the stable state continuously for <c>stableFor</c> before the stable state flips.
/// Passing cars (seconds) never flip it, a parked car (minutes) does.
/// </summary>
public sealed class SpotDebouncer
{
    private bool? _stable;
    private bool? _candidate;
    private DateTime _candidateSince;

    public bool? Stable => _stable;

    public (bool Stable, bool Changed) Update(bool raw, DateTime nowUtc, TimeSpan stableFor)
    {
        if (_stable is null) { _stable = raw; return (raw, true); }   // first sample initialises state

        if (raw == _stable) { _candidate = null; return (_stable.Value, false); }

        if (_candidate != raw) { _candidate = raw; _candidateSince = nowUtc; return (_stable.Value, false); }

        if (nowUtc - _candidateSince >= stableFor)
        {
            _stable = raw; _candidate = null;
            return (raw, true);
        }
        return (_stable.Value, false);
    }
}
