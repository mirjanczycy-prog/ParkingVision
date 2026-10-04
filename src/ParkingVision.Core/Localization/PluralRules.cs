namespace ParkingVision.Core.Localization;

/// <summary>Plural categories used by the UI string table. pl: one/few/many, en: one/many (see docs/07, "Wielojęzyczność").</summary>
public static class PluralRules
{
    public static string Category(string language, long n)
    {
        n = Math.Abs(n);
        if (language == "pl")
        {
            if (n == 1) return "one";
            long m10 = n % 10, m100 = n % 100;
            if (m10 >= 2 && m10 <= 4 && !(m100 >= 12 && m100 <= 14)) return "few";
            return "many";
        }
        return n == 1 ? "one" : "many";
    }
}
