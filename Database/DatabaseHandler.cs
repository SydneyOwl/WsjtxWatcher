using Android.Content;
using Android.Util;
using SQLite;
using WsjtxWatcher.Utils.UdpServer;

namespace WsjtxWatcher.Database;

public class DatabaseHandler
{
    private static DatabaseHandler _instance;

    private static readonly string TAG = "DBHandler";

    public static Dictionary<string, string> countries;
    private SQLiteAsyncConnection _db;
    private Context ctx;

    private bool dbExists;
    private string dbPath;

    private DatabaseHandler(Context ctx)
    {
        this.ctx = ctx;
        var isSameVersion = Utils.AppPackage.ChkInstallTime.IsSameVersion(ctx);
        dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wsjtx-watcher-database.db");
        var connectionString = new SQLiteConnectionString(dbPath,
            SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite  |
            SQLiteOpenFlags.SharedCache, true);
        dbExists = File.Exists(dbPath);
        // if (dbExists)
        // {
        //     // 测试用，记得删掉！！！
        //     File.Delete(dbPath);
        // }
        if (!(dbExists && isSameVersion))
        {
            Log.Debug(TAG, "Different version.");
            try
            {
                File.Delete(dbPath);
            }
            catch
            {
                // ignored
            }
            Log.Debug(TAG, "Creating db..");
            _db = new SQLiteAsyncConnection(connectionString);
            _db.CreateTableAsync<CallsignDatabase>().ContinueWith((_) =>
            {
                Log.Debug(TAG, "CallsignDatabase Created!");
            }).ConfigureAwait(false).GetAwaiter().GetResult();
            _db.CreateTableAsync<CountryDatabase>().ContinueWith((_) =>
            {
                Log.Debug(TAG, "CountryDatabase Created!");
            }).ConfigureAwait(false).GetAwaiter().GetResult();
            _db.CreateTableAsync<CallsignGridDatabase>().ContinueWith((_) =>
            {
                Log.Debug(TAG, "CallsignGridDatabase Created!");
            }).ConfigureAwait(false).GetAwaiter().GetResult();
            InitTableData();
            dbExists = true;
        }
        else
        {
            Log.Debug(TAG, "Same version. Skipping..");
            _db = new SQLiteAsyncConnection(connectionString);
        }
        _db.EnableWriteAheadLoggingAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public void ResetDatabase()
    {
        _db.CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        try
        {
            UdpServer.getInstance().stopServer();
        }
        catch
        {
            //ignored
        }
        
        if (dbExists)
        {
            File.Delete(dbPath);
        }
        var connectionString = new SQLiteConnectionString(dbPath,
            SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.ProtectionComplete |
            SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.FullMutex, true);
        _db = new SQLiteAsyncConnection(connectionString);
        _db.CreateTableAsync<CallsignDatabase>().ContinueWith((_) =>
        {
            Log.Debug(TAG, "RST-CallsignDatabase Created!");
        }).ConfigureAwait(false).GetAwaiter().GetResult();
        _db.CreateTableAsync<CountryDatabase>().ContinueWith((_) =>
        {
            Log.Debug(TAG, "RST-CountryDatabase Created!");
        }).ConfigureAwait(false).GetAwaiter().GetResult();
        _db.CreateTableAsync<CallsignGridDatabase>().ContinueWith((_) =>
        {
            Log.Debug(TAG, "RST-CallsignGridDatabase Created!");
        }).ConfigureAwait(false).GetAwaiter().GetResult();
        InitTableData();
        Log.Debug(TAG, "RST-Done!");
        dbExists = true;
    }
 
    public static DatabaseHandler GetInstance(Context ctx)
    {
        if (_instance == null)
        {
            _instance = new DatabaseHandler(ctx);
        }

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
        var grids = _db.QueryAsync<CallsignGridDatabase>("SELECT * FROM callsign_grid WHERE callsign=? LIMIT 1", callsign).ConfigureAwait(false).GetAwaiter().GetResult();
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
            _db.QueryAsync<CountryDatabase>("SELECT * FROM countries WHERE country_en=? LIMIT 1", countryEnName).ConfigureAwait(false)
                .GetAwaiter().GetResult();
        if (countryRes.Count == 0) return new CountryDatabase();

        return countryRes[0];
    }

    public List<CountryDatabase> QueryAllCountries()
    {
        return _db.QueryAsync<CountryDatabase>("SELECT * FROM countries").GetAwaiter().GetResult();
    }

    private void InitTableData()
    {
        // 国家信息
        InitCountryDic();
        Log.Debug(TAG, "RST->initCountryDic!");
        InitCountryData();
        Log.Debug(TAG, "RST->initInitCountryData!");
        InitCallsign();
        Log.Debug(TAG, "RST->initInitCallsign!");
    }

    private void InitCallsign()
    {
        var assetManager = ctx.Assets;
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
                    _db.RunInTransactionAsync((tran) =>
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
                            Log.Debug(TAG, (a++).ToString());
                        }
                    }).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
        }
        catch (Exception e)
        {
            Log.Warn(TAG, e.Message);
            //ignored
        }
    }

    private void InitCountryDic()
    {
        var assetManager = ctx.Assets;
        try
        {
            var inputStream = assetManager.Open("country_en2cn.dat");
            using (var reader = new StreamReader(inputStream))
            {
                var result = reader.ReadToEnd();
                var st = result.Split("\n");
                countries = new Dictionary<string, string>();
                for (var i = 0; i < st.Length; i++)
                {
                    if (!st[i].Contains(":")) continue;
                    var cc = st[i].Split(":");
                    countries[cc[0]] = cc[1];
                }
            }
        }
        catch (IOException e)
        {
        }
    }

    private void InitCountryData()
    {
        var assetManager = ctx.Assets;
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
                    cdb.CountryNameCN = searchENForCountryNameCN(cdb.CountryNameEn);
                    cdb.Id = i + 1;
                    _db.InsertAsync(cdb).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
        }
        catch (Exception e)
        {
            Log.Warn(TAG, e.Message);
            //ignored
        }
    }

    private string searchENForCountryNameCN(string country)
    {
        if (countries.TryGetValue(country, out var cnCountry)) return cnCountry;
        return null;
    }
}