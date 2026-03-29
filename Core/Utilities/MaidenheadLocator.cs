using System.Globalization;
using System.Text;

namespace WsjtxWatcher.Core.Utilities;

public static class MaidenheadLocator
{
    private const double EarthRadiusMeters = 6_371_393d;
    private const double MaxLatitude = 85d;

    public static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Equals("RR73", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return IsGridCoreValid(value, allowTwoCharacters: false);
    }

    public static GeoPoint? ToPoint(string grid)
    {
        if (string.IsNullOrWhiteSpace(grid))
        {
            return null;
        }

        var normalizedGrid = grid.Trim().ToUpperInvariant();
        if (!IsGridCoreValid(normalizedGrid, allowTwoCharacters: true))
        {
            return null;
        }

        double latBase = normalizedGrid.Length == 2 ? normalizedGrid[1] - 'A' + 0.5d : normalizedGrid[1] - 'A';
        latBase *= 10d;
        var latSquare = normalizedGrid.Length >= 4 ? normalizedGrid[3] - '0' : 0d;
        var latSubsquare = normalizedGrid.Length == 6 ? (normalizedGrid[5] - 'A' + 0.5d) / 18d : 0d;
        var latitude = Math.Clamp(latBase + latSquare + latSubsquare - 90d, -MaxLatitude, MaxLatitude);

        double lonBase = normalizedGrid.Length == 2 ? normalizedGrid[0] - 'A' + 0.5d : normalizedGrid[0] - 'A';
        lonBase *= 20d;
        var lonSquare = normalizedGrid.Length >= 4 ? (normalizedGrid[2] - '0') * 2d : 0d;
        var lonSubsquare = normalizedGrid.Length == 6 ? (normalizedGrid[4] - 'A' + 0.5d) * 2d / 18d : 0d;
        var longitude = lonBase + lonSquare + lonSubsquare - 180d;

        return new GeoPoint(latitude, longitude);
    }

    public static double GetDistanceKilometers(string firstGrid, string secondGrid)
    {
        var first = ToPoint(firstGrid);
        var second = ToPoint(secondGrid);
        return first is null || second is null ? 0d : GetDistanceKilometers(first, second);
    }

    public static double GetDistanceKilometers(GeoPoint first, GeoPoint second)
    {
        var lonA = DegreesToRadians(first.Longitude);
        var latA = DegreesToRadians(first.Latitude);
        var lonB = DegreesToRadians(second.Longitude);
        var latB = DegreesToRadians(second.Latitude);

        var cos = Math.Cos(latA) * Math.Cos(latB) * Math.Cos(lonA - lonB)
                  + Math.Sin(latA) * Math.Sin(latB);
        var arc = Math.Acos(Math.Clamp(cos, -1d, 1d));
        return EarthRadiusMeters * arc / 1000d;
    }

    public static double GetBearingDegrees(string firstGrid, string secondGrid)
    {
        var first = ToPoint(firstGrid);
        var second = ToPoint(secondGrid);
        return first is null || second is null ? 0d : GetBearingDegrees(first, second);
    }

    public static double GetBearingDegrees(GeoPoint first, GeoPoint second)
    {
        var latA = DegreesToRadians(first.Latitude);
        var lonA = DegreesToRadians(first.Longitude);
        var latB = DegreesToRadians(second.Latitude);
        var lonB = DegreesToRadians(second.Longitude);
        var deltaLon = lonB - lonA;

        var y = Math.Sin(deltaLon) * Math.Cos(latB);
        var x = Math.Cos(latA) * Math.Sin(latB) -
                Math.Sin(latA) * Math.Cos(latB) * Math.Cos(deltaLon);
        var theta = RadiansToDegrees(Math.Atan2(y, x));
        return (theta + 360d) % 360d;
    }

    public static string FormatDistance(double distanceKilometers)
    {
        if (double.IsNaN(distanceKilometers) || double.IsInfinity(distanceKilometers) || distanceKilometers < 0d)
        {
            return string.Empty;
        }

        return Math.Round(distanceKilometers, MidpointRounding.AwayFromZero).ToString("F0", CultureInfo.InvariantCulture) + " km";
    }

    public static string ToGrid(GeoPoint location)
    {
        var longitude = location.Longitude + 180d;
        var latitude = location.Latitude + 90d;
        var builder = new StringBuilder();

        var index = (int)(longitude / 20d);
        builder.Append((char)(index + 'A'));
        longitude -= index * 20d;

        index = (int)(latitude / 10d);
        builder.Append((char)(index + 'A'));
        latitude -= index * 10d;

        index = (int)(longitude / 2d);
        builder.Append((char)(index + '0'));
        longitude -= index * 2d;

        index = (int)latitude;
        builder.Append((char)(index + '0'));
        latitude -= index;

        index = (int)(longitude / 0.083333d);
        builder.Append((char)(index + 'a'));

        index = (int)(latitude / 0.0416665d);
        builder.Append((char)(index + 'a'));

        return builder.ToString()[..4].ToUpperInvariant();
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180d / Math.PI;
    }

    private static bool IsGridCoreValid(string value, bool allowTwoCharacters)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Equals("RR73", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("RR", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if ((allowTwoCharacters && normalized.Length == 2))
        {
            return IsFieldPair(normalized[0], normalized[1]);
        }

        if (normalized.Length is not 4 and not 6)
        {
            return false;
        }

        if (!IsFieldPair(normalized[0], normalized[1]) ||
            !char.IsDigit(normalized[2]) ||
            !char.IsDigit(normalized[3]))
        {
            return false;
        }

        return normalized.Length != 6 || IsSubsquarePair(normalized[4], normalized[5]);
    }

    private static bool IsFieldPair(char longitude, char latitude)
    {
        return longitude is >= 'A' and <= 'R'
               && latitude is >= 'A' and <= 'R';
    }

    private static bool IsSubsquarePair(char longitude, char latitude)
    {
        return longitude is >= 'A' and <= 'X'
               && latitude is >= 'A' and <= 'X';
    }
}

public sealed record GeoPoint(double Latitude, double Longitude);
