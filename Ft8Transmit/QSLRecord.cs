using WsjtxWatcher.Utils.Maidenhead;
using WsjtxWatcher.Utils.UTCTimer;

namespace WsjtxWatcher.Ft8Transmit;

/**
 * 用于记录通联成功信息的类。通联成功是指FT8完成6条消息的通联。并不是互认。
 * isLotW_import是指是否是外部的数据导入，因为用户可能使用了JTDX等软件通联，这样可以把通联的结果导入到FT8CN
 * isLotW_QSL是指是否被平台确认。
 * isQSL是指是否被手工确认
 * 
 * @author BGY70Z
 * @date 2023-03-20
 * ----代码来自 FT8CN----
 */
public class QslRecord
{
    private static string _tag = "QSLRecord";
    private readonly long _bandFreq; //发射的波段
    private readonly string _bandLength = "";

    private readonly string _mode = "FT8";
    private readonly string _toCallsign; //对方的呼号
    private readonly int _wavFrequency; //发射的频率
    private string _comment;
    public string ErrorMsg = ""; //如果解析出错，错误的消息
    public long Id = -1;

    public bool IsInvalid = false; //是否解析出错
    public bool IsLotWImport = false; //是否是从外部数据导入的，此项需要在数据库中比对才能设定
    public bool IsLotWQsl = false; //是否是lotw确认的

    public bool IsQsl = false; //手工确认
    //private long endTime;//结束时间

    private string _myCallsign; //我的呼号

    private string _myMaidenGrid; //我的网格

    //private long startTime;//起始时间
    private string _qsoDate;
    private string _qsoDateOff;
    private int _receivedReport; //我收到对方的报告（也就是SNR）

    public bool Saved = false; //是否被保存到数据库中
    private int _sendReport; //对方收到我的报告（也就是我发送的信号强度）
    private string _timeOff;
    private string _timeOn;
    private string _toMaidenGrid; //对方的网格

    /**
     * 构建通联成功的对象
     * 
     * @param startTime      起始时间
     * @param endTime        结束时间
     * @param myCallsign     我的呼号
     * @param myMaidenGrid   我的网格
     * @param toCallsign     对方呼号
     * @param toMaidenGrid   对方网格
     * @param sendReport     发送的报告
     * @param receivedReport 接收的报告
     * @param mode           模式 默认FT8
     * @param bandFreq       载波频率
     * @param wavFrequency   声音频率
     */
    public QslRecord()
    {
    }

    public QslRecord(long startTime, long endTime, string myCallsign, string myMaidenGrid
        , string toCallsign, string toMaidenGrid, int sendReport, int receivedReport
        , string mode, long bandFreq, int wavFrequency)
    {
        //this.startTime = startTime;
        _qsoDate = UtcTimer.GetYyyymmdd(startTime);
        _timeOn = UtcTimer.GetTimeHhmmss(startTime);
        _qsoDateOff = UtcTimer.GetYyyymmdd(endTime);
        _timeOff = UtcTimer.GetTimeHhmmss(endTime);
        this._myCallsign = myCallsign;
        this._myMaidenGrid = myMaidenGrid;
        this._toCallsign = toCallsign;
        this._toMaidenGrid = toMaidenGrid;
        this._sendReport = sendReport;
        this._receivedReport = receivedReport;
        this._mode = mode;
        _bandLength = BaseRigOperation.GetMeterFromFreq(bandFreq); //获取波长
        this._bandFreq = bandFreq;
        this._wavFrequency = wavFrequency;
        var distance = "";
        if (!myMaidenGrid.Equals("") && !toMaidenGrid.Equals(""))
            distance = MaidenheadGrid.GetDistStrEn(myMaidenGrid, toMaidenGrid);
    }

    public void Update(QslRecord record)
    {
        _qsoDateOff = record._qsoDateOff;
        _timeOff = record._timeOff;
        _toMaidenGrid = record._toMaidenGrid;
        _sendReport = record._sendReport;
        _receivedReport = record._receivedReport;
    }


    public string ToString()
    {
        return "QSLRecord{" +
               "id=" + Id +
               ", qso_date='" + _qsoDate + '\'' +
               ", time_on='" + _timeOn + '\'' +
               ", qso_date_off='" + _qsoDateOff + '\'' +
               ", time_off='" + _timeOff + '\'' +
               ", myCallsign='" + _myCallsign + '\'' +
               ", myMaidenGrid='" + _myMaidenGrid + '\'' +
               ", toCallsign='" + _toCallsign + '\'' +
               ", toMaidenGrid='" + _toMaidenGrid + '\'' +
               ", sendReport=" + _sendReport +
               ", receivedReport=" + _receivedReport +
               ", mode='" + _mode + '\'' +
               ", bandLength='" + _bandLength + '\'' +
               ", bandFreq=" + _bandFreq +
               ", wavFrequency=" + _wavFrequency +
               ", isQSL=" + IsQsl +
               ", isLotW_import=" + IsLotWImport +
               ", isLotW_QSL=" + IsLotWQsl +
               ", saved=" + Saved +
               ", comment='" + _comment + '\'' +
               '}';
    }

    public string ToHtmlString()
    {
        var ss = Saved ? "<font color=red>, saved=true</font>" : ", saved=false";
        return "QSLRecord{" +
               "id=" + Id +
               ", qso_date='" + _qsoDate + '\'' +
               ", time_on='" + _timeOn + '\'' +
               ", qso_date_off='" + _qsoDateOff + '\'' +
               ", time_off='" + _timeOff + '\'' +
               ", myCallsign='" + _myCallsign + '\'' +
               ", myMaidenGrid='" + _myMaidenGrid + '\'' +
               ", toCallsign='" + _toCallsign + '\'' +
               ", toMaidenGrid='" + _toMaidenGrid + '\'' +
               ", sendReport=" + _sendReport +
               ", receivedReport=" + _receivedReport +
               ", mode='" + _mode + '\'' +
               ", bandLength='" + _bandLength + '\'' +
               ", bandFreq=" + _bandFreq +
               ", wavFrequency=" + _wavFrequency +
               ", isQSL=" + IsQsl +
               ", isLotW_import=" + IsLotWImport +
               ", isLotW_QSL=" + IsLotWQsl +
               ss +
               ", comment='" + _comment + '\'' +
               '}';
    }

    public string GetBandLength()
    {
        return _bandLength;
    }

    public string GetToCallsign()
    {
        return _toCallsign;
    }

    public string GetToMaidenGrid()
    {
        return _toMaidenGrid;
    }

    public string GetMode()
    {
        return _mode;
    }

    public long GetBandFreq()
    {
        return _bandFreq;
    }

    public int GetWavFrequency()
    {
        return _wavFrequency;
    }


    public string GetMyCallsign()
    {
        return _myCallsign;
    }

    public void SetMyCallsign(string val)
    {
        _myCallsign = val;
    }

    public string GetMyMaidenGrid()
    {
        return _myMaidenGrid;
    }

    public void SetMyMaidenGrid(string myMaidenGrid)
    {
        this._myMaidenGrid = myMaidenGrid;
    }

    public int GetSendReport()
    {
        return _sendReport;
    }

    public int GetReceivedReport()
    {
        return _receivedReport;
    }

    public string getQso_date()
    {
        return _qsoDate;
    }

    public string getTime_on()
    {
        return _timeOn;
    }

    public string getQso_date_off()
    {
        return _qsoDateOff;
    }

    public string getTime_off()
    {
        return _timeOff;
    }

    public string GetStartTime()
    {
        return _qsoDate + "-" + _timeOn;
    }

    public string GetEndTime()
    {
        return _qsoDateOff + "-" + _timeOff;
    }

    public string GetComment()
    {
        return _comment;
    }


    public void SetToMaidenGrid(string toMaidenGrid)
    {
        this._toMaidenGrid = toMaidenGrid;
    }

    public void SetSendReport(int sendReport)
    {
        this._sendReport = sendReport;
    }

    public void SetReceivedReport(int receivedReport)
    {
        this._receivedReport = receivedReport;
    }

    public void setQso_date(string qsoDate)
    {
        this._qsoDate = qsoDate;
    }

    public void setTime_on(string timeOn)
    {
        this._timeOn = timeOn;
    }
}