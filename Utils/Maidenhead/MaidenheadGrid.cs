using System.Text;

namespace WsjtxWatcher.Utils.Maidenhead;

// * TNX FT8CN----
public class MaidenheadGrid
{
    private const string TAG = "MaidenheadGrid";
    private const double EARTH_RADIUS = 6371393; // 平均半径，单位：米

    public static LatLng GridToLatLng(string grid)
    {
        if (string.IsNullOrEmpty(grid)) return null;

        if (grid.Length != 2 && grid.Length != 4 && grid.Length != 6)
            return null;

        if (grid.Equals("RR73", StringComparison.OrdinalIgnoreCase) ||
            grid.Equals("RR", StringComparison.OrdinalIgnoreCase))
            return null;

        double x = 0, y = 0, z = 0;
        double lat = 0;

        if (grid.Length == 2)
            x = grid.ToUpper()[1] - 'A' + 0.5;
        else
            x = grid.ToUpper()[1] - 'A';
        x *= 10;

        if (grid.Length == 4)
            y = grid[3] - '0' + 0.5;
        else if (grid.Length == 6) y = grid[3] - '0';

        if (grid.Length == 6)
        {
            z = grid.ToUpper()[5] - 'A' + 0.5;
            z *= 1 / 18.0;
        }

        lat = x + y + z - 90;

        // 经度计算
        x = 0;
        y = 0;
        z = 0;
        double lng = 0;

        if (grid.Length == 2)
            x = grid.ToUpper()[0] - 'A' + 0.5;
        else
            x = grid.ToUpper()[0] - 'A';
        x *= 20;

        if (grid.Length == 4)
            y = grid[2] - '0' + 0.5;
        else if (grid.Length == 6) y = grid[2] - '0';
        y *= 2;

        if (grid.Length == 6)
        {
            z = grid.ToUpper()[4] - 'A' + 0.5;
            z *= 2 / 18.0;
        }

        lng = x + y + z - 180;

        lat = Math.Clamp(lat, -85, 85); // 防止在地图上越界

        return new LatLng(lat, lng);
    }

    public static LatLng[] GridToPolygon(string grid)
    {
        if (grid.Length != 2 && grid.Length != 4 && grid.Length != 6) return null;

        var latLngs = new LatLng[4];

        // 纬度1
        double x = grid.ToUpper()[1] - 'A';
        x *= 10;
        double y = 0;
        double z = 0;
        double lat1;

        if (grid.Length > 2) y = grid[3] - '0';
        if (grid.Length > 4)
        {
            z = grid.ToUpper()[5] - 'A';
            z *= 1 / 18.0;
        }

        lat1 = x + y + z - 90;
        lat1 = Math.Clamp(lat1, -85.0, 85.0);

        // 纬度2
        x = grid.Length == 2 ? grid.ToUpper()[1] - 'A' + 1 : grid.ToUpper()[1] - 'A';
        x *= 10;
        double lat2;

        if (grid.Length == 4)
            y = grid[3] - '0' + 1;
        else if (grid.Length == 6) y = grid[3] - '0';
        if (grid.Length == 6)
        {
            z = grid.ToUpper()[5] - 'A' + 1;
            z *= 1 / 18.0;
        }

        lat2 = x + y + z - 90;
        lat2 = Math.Clamp(lat2, -85.0, 85.0);

        // 经度1
        x = grid.ToUpper()[0] - 'A';
        x *= 20;
        double lng1;

        if (grid.Length > 2)
        {
            y = grid[2] - '0';
            y *= 2;
        }

        if (grid.Length > 4)
        {
            z = grid.ToUpper()[4] - 'A';
            z *= 2 / 18.0;
        }

        lng1 = x + y + z - 180;

        // 经度2
        x = grid.Length == 2 ? grid.ToUpper()[0] - 'A' + 1 : grid.ToUpper()[0] - 'A';
        x *= 20;
        double lng2;

        if (grid.Length == 4)
            y = grid[2] - '0' + 1;
        else if (grid.Length == 6) y = grid[2] - '0';
        y *= 2;
        if (grid.Length == 6)
        {
            z = grid.ToUpper()[4] - 'A' + 1;
            z *= 2 / 18.0;
        }

        lng2 = x + y + z - 180;

        latLngs[0] = new LatLng(lat1, lng1);
        latLngs[1] = new LatLng(lat1, lng2);
        latLngs[2] = new LatLng(lat2, lng2);
        latLngs[3] = new LatLng(lat2, lng1);

        return latLngs;
    }

    public static string GetGridSquare(LatLng location)
    {
        var _long = location.Longitude + 180;
        var _lat = location.Latitude + 90;

        var buff = new StringBuilder();

        // 第一对两个字符
        var index = (int)(_long / 20);
        buff.Append((char)(index + 'A'));
        _long -= index * 20;

        index = (int)(_lat / 10);
        buff.Append((char)(index + 'A'));
        _lat -= index * 10;

        // 第二对两数字
        index = (int)(_long / 2);
        buff.Append((char)(index + '0'));
        _long -= index * 2;

        index = (int)_lat;
        buff.Append((char)(index + '0'));
        _lat -= index;

        // 第三对两个小写字符
        index = (int)(_long / 0.083333);
        buff.Append((char)(index + 'a'));

        index = (int)(_lat / 0.0416665);
        buff.Append((char)(index + 'a'));

        return buff.ToString().Substring(0, 4);
    }

    public static double GetDist(LatLng latLng1, LatLng latLng2)
    {
        var radiansAX = DegreesToRadians(latLng1.Longitude);
        var radiansAY = DegreesToRadians(latLng1.Latitude);
        var radiansBX = DegreesToRadians(latLng2.Longitude);
        var radiansBY = DegreesToRadians(latLng2.Latitude);

        var cos = Math.Cos(radiansAY) * Math.Cos(radiansBY) * Math.Cos(radiansAX - radiansBX)
                  + Math.Sin(radiansAY) * Math.Sin(radiansBY);
        var acos = Math.Acos(cos);

        return EARTH_RADIUS * acos / 1000; // 返回公里
    }

    public static double GetDist(string mGrid1, string mGrid2)
    {
        var latLng1 = GridToLatLng(mGrid1);
        var latLng2 = GridToLatLng(mGrid2);

        if (latLng1 != null && latLng2 != null) return GetDist(latLng1, latLng2);

        return 0;
    }

    public static string GetDistStr(string mGrid1, string mGrid2)
    {
        var dist = GetDist(mGrid1, mGrid2);
        if (dist == 0)
            return "";
        return $"{dist} km";
    }

    public static string GetDistLatLngStr(LatLng latLng1, LatLng latLng2)
    {
        return string.Format("{0} km", GetDist(latLng1, latLng2));
    }

    public static string GetDistStrEN(string mGrid1, string mGrid2)
    {
        var dist = GetDist(mGrid1, mGrid2);
        if (dist == 0)
            return "";
        return $"{dist:0} km";
    }


    public static bool CheckMaidenhead(string s)
    {
        if (s.Length != 4 && s.Length != 6)
        {
            return false;
        }

        if (s.Equals("RR73", StringComparison.OrdinalIgnoreCase)) return false;
        return char.IsLetter(s[0])
               && char.IsLetter(s[1])
               && char.IsDigit(s[2])
               && char.IsDigit(s[3]);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}

public class LatLng
{
    public LatLng(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }
    public double Longitude { get; }
}