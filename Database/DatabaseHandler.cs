using Android.Content;
using Android.Util;
using SQLite;
using WsjtxWatcher.Utils.AppPackage;
using WsjtxWatcher.Utils.UdpServer;

namespace WsjtxWatcher.Database;

public class DatabaseHandler
{
    private static DatabaseHandler _instance;

    private static readonly string Tag = "DBHandler";

    public static Dictionary<string, string> Countries;
    private SQLiteConnection _db;
    private readonly Context _ctx;

    private bool _dbExists;
    private readonly string _dbPath;

    private DatabaseHandler(Context ctx)
    {
        this._ctx = ctx;
        var isSameVersion = ChkInstallTime.IsSameVersion(ctx);
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wsjtx-watcher-database.db");
        var connectionString = new SQLiteConnectionString(_dbPath,
            SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.SharedCache, true);
        _dbExists = File.Exists(_dbPath);
        // if (dbExists)
        // {
        //     // 测试用，记得删掉！！！
        //     File.Delete(dbPath);
        // }
        if (!(_dbExists && isSameVersion))
        {
            Serilog.Log.Debug("Different version.");
            try
            {
                File.Delete(_dbPath);
            }
            catch
            {
                // ignored
            }

            Serilog.Log.Debug("Creating db..");
            _db = new SQLiteConnection(connectionString);
            _db.CreateTable<CallsignDatabase>();
            Serilog.Log.Debug("CallsignDatabase Created!");
            _db.CreateTable<CountryDatabase>();
            Serilog.Log.Debug("CountryDatabase Created!");
            _db.CreateTable<CallsignGridDatabase>();
            Serilog.Log.Debug("CallsignGridDatabase Created!");
            InitTableData();
            _dbExists = true;
        }
        else
        {
            Serilog.Log.Debug("Same version. Skipping..");
            _db = new SQLiteConnection(connectionString);
        }

        _db.EnableWriteAheadLogging();
    }

    public void ResetDatabase()
    {
        _db.Close();
        try
        {
            UdpServer.GetInstance().StopServer();
        }
        catch
        {
            //ignored
        }

        if (_dbExists) File.Delete(_dbPath);
        var connectionString = new SQLiteConnectionString(_dbPath,
            SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.ProtectionComplete |
            SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.FullMutex, true);
        _db = new SQLiteConnection(connectionString);
        _db.CreateTable<CallsignDatabase>();
        Serilog.Log.Debug("RST-CallsignDatabase Created!");
        _db.CreateTable<CountryDatabase>();
        Serilog.Log.Debug("RST-CountryDatabase Created!");
        _db.CreateTable<CallsignGridDatabase>();
        Serilog.Log.Debug("RST-CallsignGridDatabase Created!");
        InitTableData();
        Serilog.Log.Debug("RST-Done!");
        _dbExists = true;
    }

    public static DatabaseHandler GetInstance(Context ctx)
    {
        if (_instance == null) _instance = new DatabaseHandler(ctx);

        return _instance;
    }

    public void AddCallsignGrid(string callsign, string grid)
    {
        // var stocks = _db.Table<CallsignGridDatabase>().Where(v=>v.Callsign==callsign);
        // stocks.ToListAsync().ContinueWith((t) =>
        // {
        //     if (t.Result.Count != 0)
        //     {
        //         // 反正只有一个，不想写sql
        //         foreach (var callsignGridDatabase in t.Result)
        //         {
        //             _db.DeleteAsync<CallsignGridDatabase>(callsignGridDatabase);
        //         }
        //     }
        // });
        _db.InsertOrReplace(new CallsignGridDatabase
        {
            Callsign = callsign,
            Grid = grid
        });
        // _db.ExecuteAsync("DELETE FROM callsign_grid WHERE callsign=?", callsign).ConfigureAwait(false).GetAwaiter().GetResult();
        // _db.InsertAsync(new CallsignGridDatabase
        // {
        //     Callsign = callsign,
        //     Grid = grid
        // }).ConfigureAwait(false);
    }

    // 查找呼号对应的坐标
    public string QueryGrid(string callsign)
    {
        var grids = _db
            .Query<CallsignGridDatabase>("SELECT * FROM callsign_grid WHERE callsign=? LIMIT 1", callsign);
        if (grids.Count == 0) return "";
        return grids[0].Grid;
    }

    public CountryDatabase QueryCountryByCallsign(string callsign)
    {
        // 不能用LEFT JOIN
        var countriesRes = _db.Query<CountryDatabase>(
            "select a.*,b.* from callsigns as a left join countries as b on a.country_id =b.id WHERE (SUBSTR(?,1,LENGTH(callsign))=callsign) OR (callsign='='||?) order by LENGTH(callsign) desc LIMIT 1",
            callsign, callsign);
        if (countriesRes.Count == 0) return new CountryDatabase();
        // var cal = _db.Query<CallsignDatabase>($"SELECT callsign FROM callsigns WHERE (SUBSTR(\"{callsign}\", 1, LENGTH(callsign)) = callsign) OR (callsign = \"=\" || \"{callsign}\")) LIMIT 1");
        // if (cal.Count == 0)
        // {
        //     return new CountryDatabase();
        // }
        //
        // var res = _db.Query<CountryDatabase>(
        //     $"SELECT a.*, b.* FROM callsigns AS a, countries AS b WHERE a.country_id = b.id  AND a.callsign={cal[0].Callsign}  ORDER BY LENGTH(a.callsign) DESC LIMIT 1;");
        //
        // if (res.Count == 0)
        // {
        //     return new CountryDatabase();
        // }
        return countriesRes[0];
    }

    public CountryDatabase QueryCountryByName(string countryEnName)
    {
        var countryRes =
            _db.Query<CountryDatabase>("SELECT * FROM countries WHERE country_en=? LIMIT 1", countryEnName);
        if (countryRes.Count == 0) return new CountryDatabase();

        return countryRes[0];
    }
    
    public List<CountryDatabase> QueryCountriesByNameOrDxcc(string query)
    {
        return _db.Query<CountryDatabase>("SELECT * FROM countries WHERE country_en LIKE '%' || ? || '%' or country_cn LIKE '%' || ? || '%' or dxcc LIKE '%' || ? || '%'",query,query,query);
    }

    public List<CountryDatabase> QueryAllCountries()
    {
        return _db.Query<CountryDatabase>("SELECT * FROM countries");
    }

    private void InitTableData()
    {
        // 国家信息
        InitCountryDic();
        Serilog.Log.Debug("RST->initCountryDic!");
        InitPrefixAndCountry();
        Serilog.Log.Debug("RST->initInitCallsign!");
    }

    private void InitPrefixAndCountry()
    {
        var assetManager = _ctx.Assets;
        try
        {
            var inputStream = assetManager.Open("cty.dat");
            using var reader = new StreamReader(inputStream);
            var result = reader.ReadToEnd();
            var callsigns = new List<CallsignDatabase>();
            var countries = new List<CountryDatabase>();
            var st = result.Split(";");
            for (var i = 0; i < st.Length; i++)
            {
                if (!st[i].Contains(":")) continue;
                var cdb = new CountryDatabase(st[i]);
                cdb.CountryNameCn = SearchEnForCountryNameCn(cdb.CountryNameEn);
                cdb.Id = i + 1;

                countries.Add(cdb);
                // await _conn!.InsertAsync(cdb);
                // calculate callsig

                # region callsign

                if (!st[i].Contains(":")) continue;
                var info = st[i].Split(":");
                if (info.Length < 9) continue;
                var ls = info[8].Replace("\n", "").Split(",");
                // await _conn.RunInTransactionAsync(tran =>
                // {
                for (var j = 0; j < ls.Length; j++)
                {
                    if (ls[j].Contains(")")) ls[j] = ls[j].Substring(0, ls[j].IndexOf("("));
                    if (ls[j].Contains("[")) ls[j] = ls[j].Substring(0, ls[j].IndexOf("["));
                    callsigns.Add(new CallsignDatabase
                    {
                        Callsign = ls[j].Trim(),
                        CountryId = i + 1
                    });
                }
                // });

                # endregion

                // _db.InsertAsync(cdb).GetAwaiter().GetResult();
            }

            _db.InsertAll(countries);
            _db.InsertAll(callsigns);
        }
        catch (Exception e)
        {
            Serilog.Log.Warning(e.Message);
            //ignored
        }
    }

    private void InitCountryDic()
    {
        var assetManager = _ctx.Assets;
        try
        {
            var inputStream = assetManager.Open("country_en2cn.dat");
            using (var reader = new StreamReader(inputStream))
            {
                var result = reader.ReadToEnd();
                var st = result.Split("\n");
                Countries = new Dictionary<string, string>();
                for (var i = 0; i < st.Length; i++)
                {
                    if (!st[i].Contains(":")) continue;
                    var cc = st[i].Split(":");
                    Countries[cc[0]] = cc[1];
                }
            }
        }
        catch (Exception e)
        {
            Serilog.Log.Warning(e.Message);
        }
    }
    

    private string SearchEnForCountryNameCn(string country)
    {
        if (Countries.TryGetValue(country, out var cnCountry)) return cnCountry;
        return null;
    }
}