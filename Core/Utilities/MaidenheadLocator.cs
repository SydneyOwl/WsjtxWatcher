using System.Globalization;
using System.Text;

namespace WsjtxWatcher.Core.Utilities;

public static class MaidenheadLocator
{
    private const double EarthRadiusMeters = 6_371_393d;

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

        return value.Length is 4 or 6
               && char.IsLetter(value[0])
               && char.IsLetter(value[1])
               && char.IsDigit(value[2])
               && char.IsDigit(value[3]);
    }

    public static GeoPoint? ToPoint(string grid)
    {
        if (string.IsNullOrWhiteSpace(grid) || (grid.Length != 2 && grid.Length != 4 && grid.Length != 6))
        {
            return null;
        }

        if (grid.Equals("RR73", StringComparison.OrdinalIgnoreCase) ||
            grid.Equals("RR", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        double latBase = grid.Length == 2 ? char.ToUpperInvariant(grid[1]) - 'A' + 0.5d : char.ToUpperInvariant(grid[1]) - 'A';
        latBase *= 10d;
        var latSquare = grid.Length >= 4 ? grid[3] - '0' : 0d;
        var latSubsquare = grid.Length == 6 ? (char.ToUpperInvariant(grid[5]) - 'A' + 0.5d) / 18d : 0d;
        var latitude = Math.Clamp(latBase + latSquare + latSubsquare - 90d, -85d, 85d);

        double lonBase = grid.Length == 2 ? char.ToUpperInvariant(grid[0]) - 'A' + 0.5d : char.ToUpperInvariant(grid[0]) - 'A';
        lonBase *= 20d;
        var lonSquare = grid.Length >= 4 ? (grid[2] - '0') * 2d : 0d;
        var lonSubsquare = grid.Length == 6 ? (char.ToUpperInvariant(grid[4]) - 'A' + 0.5d) * 2d / 18d : 0d;
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
        var arc = Math.Acos(cos);
        return EarthRadiusMeters * arc / 1000d;
    }

    public static string FormatDistance(double distanceKilometers)
    {
        return distanceKilometers <= 0d
            ? "? km"
            : Math.Floor(distanceKilometers).ToString(CultureInfo.InvariantCulture) + " km";
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

        return builder.ToString()[..4];
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }
}

public sealed record GeoPoint(double Latitude, double Longitude);
