using ParkingVision.Core;
using ParkingVision.Vision;
using Xunit;

namespace ParkingVision.Tests;

public class DebouncerTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FirstSample_InitialisesState()
    {
        var d = new SpotDebouncer();
        var (stable, changed) = d.Update(true, T0, TimeSpan.FromSeconds(30));
        Assert.True(stable); Assert.True(changed);
    }

    [Fact]
    public void ShortBlip_DoesNotFlipState()
    {
        var d = new SpotDebouncer();
        d.Update(false, T0, TimeSpan.FromSeconds(30));
        d.Update(true, T0.AddSeconds(3), TimeSpan.FromSeconds(30));      // car drives past
        var (stable, changed) = d.Update(false, T0.AddSeconds(6), TimeSpan.FromSeconds(30));
        Assert.False(stable); Assert.False(changed);
    }

    [Fact]
    public void SustainedChange_FlipsAfterStableTime()
    {
        var d = new SpotDebouncer();
        d.Update(false, T0, TimeSpan.FromSeconds(30));
        d.Update(true, T0.AddSeconds(3), TimeSpan.FromSeconds(30));
        var (stable, changed) = d.Update(true, T0.AddSeconds(40), TimeSpan.FromSeconds(30));
        Assert.True(stable); Assert.True(changed);
    }
}

public class FusionTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(3);

    [Fact]
    public void NoFreshReadings_IsUnknown()
    {
        var (s, _) = SpotFusion.Fuse(new[] { new SpotFusion.Reading(Now.AddMinutes(-10), true, .9, 1) }, Now, Ttl);
        Assert.Equal(OccupancyStatus.Unknown, s);
    }

    [Fact]
    public void TwoCamerasDisagree_HigherConfidenceWins()
    {
        var r = new[] { new SpotFusion.Reading(Now, true, .95, 1), new SpotFusion.Reading(Now, false, .60, 1) };
        Assert.Equal(OccupancyStatus.Occupied, SpotFusion.Fuse(r, Now, Ttl).Status);
    }

    [Fact]
    public void Tie_PrefersOccupied()
    {
        var r = new[] { new SpotFusion.Reading(Now, true, .8, 1), new SpotFusion.Reading(Now, false, .8, 1) };
        Assert.Equal(OccupancyStatus.Occupied, SpotFusion.Fuse(r, Now, Ttl).Status);
    }
}

public class EvaluatorTests
{
    private readonly SpotEvaluator _ev = new(new VisionOptions());

    // two side-by-side spots in a 1000x1000 frame
    private static Dictionary<string, NetTopologySuite.Geometries.Polygon> Rois() => new()
    {
        ["L"] = SpotEvaluator.ParseRoi("[[0.1,0.5],[0.4,0.5],[0.4,0.9],[0.1,0.9]]", 1000, 1000)!,
        ["R"] = SpotEvaluator.ParseRoi("[[0.5,0.5],[0.8,0.5],[0.8,0.9],[0.5,0.9]]", 1000, 1000)!,
    };

    [Fact]
    public void CarInsideLeftSpot_OnlyLeftOccupied()
    {
        var det = new[] { new Detection(120, 500, 380, 900, 0.9f, 2) };
        var res = _ev.Evaluate(det, Rois());
        Assert.True(res["L"].Occupied); Assert.False(res["R"].Occupied);
    }

    [Fact]
    public void CarWiderThanSpot_IsAssignedToBestSpotOnly()
    {
        var det = new[] { new Detection(150, 500, 560, 900, 0.9f, 2) };   // mostly L, a little R
        var res = _ev.Evaluate(det, Rois());
        Assert.True(res["L"].Occupied); Assert.False(res["R"].Occupied);
    }

    [Fact]
    public void NoDetections_AllFree()
    {
        var res = _ev.Evaluate(Array.Empty<Detection>(), Rois());
        Assert.All(res.Values, r => Assert.False(r.Occupied));
    }
}

public class LocalizationTests
{
    private static readonly System.Text.RegularExpressions.Regex Ph = new(@"\{\d+\}");

    [Fact]
    public void EveryKeyHasBothLanguages()
    {
        foreach (var (k, v) in ParkingVision.Core.Localization.Strings.Table)
        {
            Assert.False(string.IsNullOrWhiteSpace(v.Pl), "pl missing: " + k);
            Assert.False(string.IsNullOrWhiteSpace(v.En), "en missing: " + k);
        }
    }

    [Fact]
    public void PlaceholdersMatchBetweenLanguages()
    {
        foreach (var (k, v) in ParkingVision.Core.Localization.Strings.Table)
        {
            var pl = Ph.Matches(v.Pl).Select(m => m.Value).OrderBy(x => x).ToList();
            var en = Ph.Matches(v.En).Select(m => m.Value).OrderBy(x => x).ToList();
            Assert.True(pl.SequenceEqual(en), "placeholder mismatch: " + k);
        }
    }

    [Fact]
    public void PluralGroupsHaveOneAndMany()
    {
        var table = ParkingVision.Core.Localization.Strings.Table;
        var bases = table.Keys.Where(k => k.Contains('|')).Select(k => k[..k.IndexOf('|')]).Distinct();
        foreach (var b in bases)
        {
            Assert.True(table.ContainsKey(b + "|one"), b + "|one");
            Assert.True(table.ContainsKey(b + "|many"), b + "|many");
        }
    }

    [Theory]
    [InlineData("pl", 1, "one")]
    [InlineData("pl", 2, "few")]
    [InlineData("pl", 4, "few")]
    [InlineData("pl", 5, "many")]
    [InlineData("pl", 12, "many")]
    [InlineData("pl", 22, "few")]
    [InlineData("pl", 112, "many")]
    [InlineData("pl", 0, "many")]
    [InlineData("en", 1, "one")]
    [InlineData("en", 2, "many")]
    [InlineData("en", 0, "many")]
    public void PluralCategories(string lang, long n, string expected) =>
        Assert.Equal(expected, ParkingVision.Core.Localization.PluralRules.Category(lang, n));
}
