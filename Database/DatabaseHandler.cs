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
    private SQLiteAsyncConnection _db;
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
            _db = new SQLiteAsyncConnection(connectionString);
            _db.CreateTableAsync<CallsignDatabase>().ContinueWith(_ => { Serilog.Log.Debug("CallsignDatabase Created!"); })
                .ConfigureAwait(false).GetAwaiter().GetResult();
            _db.CreateTableAsync<CountryDatabase>().ContinueWith(_ => { Serilog.Log.Debug("CountryDatabase Created!"); })
                .ConfigureAwait(false).GetAwaiter().GetResult();
            _db.CreateTableAsync<CallsignGridDatabase>().ContinueWith(_ =>
            {
                Serilog.Log.Debug("CallsignGridDatabase Created!");
            }).ConfigureAwait(false).GetAwaiter().GetResult();
            InitTableData();
            _dbExists = true;
        }
        else
        {
            Serilog.Log.Debug("Same version. Skipping..");
            _db = new SQLiteAsyncConnection(connectionString);
        }

        _db.EnableWriteAheadLoggingAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public void ResetDatabase()
    {
        _db.CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
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
        _db = new SQLiteAsyncConnection(connectionString);
        _db.CreateTableAsync<CallsignDatabase>().ContinueWith(_ => { Serilog.Log.Debug("RST-CallsignDatabase Created!"); })
            .ConfigureAwait(false).GetAwaiter().GetResult();
        _db.CreateTableAsync<CountryDatabase>().ContinueWith(_ => { Serilog.Log.Debug("RST-CountryDatabase Created!"); })
            .ConfigureAwait(false).GetAwaiter().GetResult();
        _db.CreateTableAsync<CallsignGridDatabase>().ContinueWith(_ =>
        {
            Serilog.Log.Debug("RST-CallsignGridDatabase Created!");
        }).ConfigureAwait(false).GetAwaiter().GetResult();
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
        _db.InsertOrReplaceAsync(new CallsignGridDatabase
        {
            Callsign = callsign,
            Grid = grid
        }).ConfigureAwait(false);
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
            .QueryAsync<CallsignGridDatabase>("SELECT * FROM callsign_grid WHERE callsign=? LIMIT 1", callsign)
            .ConfigureAwait(false).GetAwaiter().GetResult();
        if (grids.Count == 0) return "";
        return grids[0].Grid;
    }

    public CountryDatabase QueryCountryByCallsign(string callsign)
    {
        // 不能用LEFT JOIN
        var countriesRes = _db.QueryAsync<CountryDatabase>(
            "select a.*,b.* from callsigns as a left join countries as b on a.country_id =b.id WHERE (SUBSTR(?,1,LENGTH(callsign))=callsign) OR (callsign='='||?) order by LENGTH(callsign) desc LIMIT 1",
            callsign, callsign).ConfigureAwait(false).GetAwaiter().GetResult();
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
            _db.QueryAsync<CountryDatabase>("SELECT * FROM countries WHERE country_en=? LIMIT 1", countryEnName)
                .ConfigureAwait(false)
                .GetAwaiter().GetResult();
        if (countryRes.Count == 0) return new CountryDatabase();

        return countryRes[0];
    }
    
    public List<CountryDatabase> QueryCountriesByNameOrDxcc(string query)
    {
        return _db.QueryAsync<CountryDatabase>("SELECT * FROM countries WHERE country_en LIKE '%' || ? || '%' or country_cn LIKE '%' || ? || '%' or dxcc LIKE '%' || ? || '%'",query,query,query).GetAwaiter().GetResult();
    }

    public List<CountryDatabase> QueryAllCountries()
    {
        return _db.QueryAsync<CountryDatabase>("SELECT * FROM countries").GetAwaiter().GetResult();
    }

    private void InitTableData()
    {
        // 国家信息
        InitCountryDic();
        Serilog.Log.Debug("RST->initCountryDic!");
        InitCountryData();
        Serilog.Log.Debug("RST->initInitCountryData!");
        InitCallsign();
        Serilog.Log.Debug("RST->initInitCallsign!");
    }

    private void InitCallsign()
    {
        var assetManager = _ctx.Assets;
        try
        {
            var a = 0;
            var inputStream = assetManager.Open("cty.dat");
            using (var reader = new StreamReader(inputStream))
            {
                var result = reader.ReadToEnd();
                var st = result.Split(";");
                for (var j = 0; j < st.Length; j++)
                {
                    if (!st[j].Contains(":")) continue;
                    var info = st[j].Split(":");
                    if (info.Length < 9) continue;
                    var ls = info[8].Replace("\n", "").Split(",");
                    _db.RunInTransactionAsync(tran =>
                    {
                        for (var i = 0; i < ls.Length; i++)
                        {
                            if (ls[i].Contains(")")) ls[i] = ls[i].Substring(0, ls[i].IndexOf("("));
                            if (ls[i].Contains("[")) ls[i] = ls[i].Substring(0, ls[i].IndexOf("["));
                            tran.Insert(new CallsignDatabase
                            {
                                Callsign = ls[i].Trim(),
                                CountryId = j + 1
                            });
                        }
                    }).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
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
        catch (IOException e)
        {
        }
    }

    private void InitCountryData()
    {
        var assetManager = _ctx.Assets;
        try
        {
            var inputStream = assetManager.Open("cty.dat");
            using (var reader = new StreamReader(inputStream))
            {
                var result = reader.ReadToEnd();
                var st = result.Split(";");
                for (var i = 0; i < st.Length; i++)
                {
                    if (!st[i].Contains(":")) continue;
                    var cdb = new CountryDatabase(st[i]);
                    cdb.CountryNameCn = SearchEnForCountryNameCn(cdb.CountryNameEn);
                    cdb.Id = i + 1;
                    _db.InsertAsync(cdb).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
        }
        catch (Exception e)
        {
            Serilog.Log.Warning(e.Message);
            //ignored
        }
    }

    private string SearchEnForCountryNameCn(string country)
    {
        if (Countries.TryGetValue(country, out var cnCountry)) return cnCountry;
        return null;
    }
}