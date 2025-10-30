namespace LEG.CoreLib.Abstractions.SolarCalculations.Domain;

public record UserProfile(
    List<double> AsList,   // Store as list of doubles
    double[] AsArray       // Store as array of doubles
)
{
    public UserProfile(List<double> profile) : this(profile, [.. profile]) { }
}