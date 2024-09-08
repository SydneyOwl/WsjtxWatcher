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
public class QSLRecord
{
    private static string TAG = "QSLRecord";
    private readonly long bandFreq; //发射的波段
    private readonly string bandLength = "";
    private string comment;
    public string errorMSG = ""; //如果解析出错，错误的消息
    public long id = -1;

    public bool isInvalid = false; //是否解析出错
    public bool isLotW_import = false; //是否是从外部数据导入的，此项需要在数据库中比对才能设定
    public bool isLotW_QSL = false; //是否是lotw确认的
    public bool isQSL = false; //手工确认

    private readonly string mode = "FT8";
    //private long endTime;//结束时间

    private string myCallsign; //我的呼号

    private string myMaidenGrid; //我的网格

    //private long startTime;//起始时间
    private string qso_date;
    private string qso_date_off;
    private int receivedReport; //我收到对方的报告（也就是SNR）

    public bool saved = false; //是否被保存到数据库中
    private int sendReport; //对方收到我的报告（也就是我发送的信号强度）
    private string time_off;
    private string time_on;
    private readonly string toCallsign; //对方的呼号
    private string toMaidenGrid; //对方的网格
    private readonly int wavFrequency; //发射的频率

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
    public QSLRecord()
    {
    }

    public QSLRecord(long startTime, long endTime, string myCallsign, string myMaidenGrid
        , string toCallsign, string toMaidenGrid, int sendReport, int receivedReport
        , string mode, long bandFreq, int wavFrequency)
    {
        //this.startTime = startTime;
        qso_date = UTCTimer.GetYYYYMMDD(startTime);
        time_on = UTCTimer.GetTimeHHMMSS(startTime);
        qso_date_off = UTCTimer.GetYYYYMMDD(endTime);
        time_off = UTCTimer.GetTimeHHMMSS(endTime);
        this.myCallsign = myCallsign;
        this.myMaidenGrid = myMaidenGrid;
        this.toCallsign = toCallsign;
        this.toMaidenGrid = toMaidenGrid;
        this.sendReport = sendReport;
        this.receivedReport = receivedReport;
        this.mode = mode;
        bandLength = BaseRigOperation.GetMeterFromFreq(bandFreq); //获取波长
        this.bandFreq = bandFreq;
        this.wavFrequency = wavFrequency;
        var distance = "";
        if (!myMaidenGrid.Equals("") && !toMaidenGrid.Equals(""))
            distance = MaidenheadGrid.GetDistStrEN(myMaidenGrid, toMaidenGrid);
    }

    public void update(QSLRecord record)
    {
        qso_date_off = record.qso_date_off;
        time_off = record.time_off;
        toMaidenGrid = record.toMaidenGrid;
        sendReport = record.sendReport;
        receivedReport = record.receivedReport;
    }


    public string toString()
    {
        return "QSLRecord{" +
               "id=" + id +
               ", qso_date='" + qso_date + '\'' +
               ", time_on='" + time_on + '\'' +
               ", qso_date_off='" + qso_date_off + '\'' +
               ", time_off='" + time_off + '\'' +
               ", myCallsign='" + myCallsign + '\'' +
               ", myMaidenGrid='" + myMaidenGrid + '\'' +
               ", toCallsign='" + toCallsign + '\'' +
               ", toMaidenGrid='" + toMaidenGrid + '\'' +
               ", sendReport=" + sendReport +
               ", receivedReport=" + receivedReport +
               ", mode='" + mode + '\'' +
               ", bandLength='" + bandLength + '\'' +
               ", bandFreq=" + bandFreq +
               ", wavFrequency=" + wavFrequency +
               ", isQSL=" + isQSL +
               ", isLotW_import=" + isLotW_import +
               ", isLotW_QSL=" + isLotW_QSL +
               ", saved=" + saved +
               ", comment='" + comment + '\'' +
               '}';
    }

    public string toHtmlString()
    {
        var ss = saved ? "<font color=red>, saved=true</font>" : ", saved=false";
        return "QSLRecord{" +
               "id=" + id +
               ", qso_date='" + qso_date + '\'' +
               ", time_on='" + time_on + '\'' +
               ", qso_date_off='" + qso_date_off + '\'' +
               ", time_off='" + time_off + '\'' +
               ", myCallsign='" + myCallsign + '\'' +
               ", myMaidenGrid='" + myMaidenGrid + '\'' +
               ", toCallsign='" + toCallsign + '\'' +
               ", toMaidenGrid='" + toMaidenGrid + '\'' +
               ", sendReport=" + sendReport +
               ", receivedReport=" + receivedReport +
               ", mode='" + mode + '\'' +
               ", bandLength='" + bandLength + '\'' +
               ", bandFreq=" + bandFreq +
               ", wavFrequency=" + wavFrequency +
               ", isQSL=" + isQSL +
               ", isLotW_import=" + isLotW_import +
               ", isLotW_QSL=" + isLotW_QSL +
               ss +
               ", comment='" + comment + '\'' +
               '}';
    }

    public string getBandLength()
    {
        return bandLength;
    }

    public string getToCallsign()
    {
        return toCallsign;
    }

    public string getToMaidenGrid()
    {
        return toMaidenGrid;
    }

    public string getMode()
    {
        return mode;
    }

    public long getBandFreq()
    {
        return bandFreq;
    }

    public int getWavFrequency()
    {
        return wavFrequency;
    }


    public string getMyCallsign()
    {
        return myCallsign;
    }

    public void setMyCallsign(string val)
    {
        myCallsign = val;
    }

    public string getMyMaidenGrid()
    {
        return myMaidenGrid;
    }

    public void setMyMaidenGrid(string myMaidenGrid)
    {
        this.myMaidenGrid = myMaidenGrid;
    }

    public int getSendReport()
    {
        return sendReport;
    }

    public int getReceivedReport()
    {
        return receivedReport;
    }

    public string getQso_date()
    {
        return qso_date;
    }

    public string getTime_on()
    {
        return time_on;
    }

    public string getQso_date_off()
    {
        return qso_date_off;
    }

    public string getTime_off()
    {
        return time_off;
    }

    public string getStartTime()
    {
        return qso_date + "-" + time_on;
    }

    public string getEndTime()
    {
        return qso_date_off + "-" + time_off;
    }

    public string getComment()
    {
        return comment;
    }


    public void setToMaidenGrid(string toMaidenGrid)
    {
        this.toMaidenGrid = toMaidenGrid;
    }

    public void setSendReport(int sendReport)
    {
        this.sendReport = sendReport;
    }

    public void setReceivedReport(int receivedReport)
    {
        this.receivedReport = receivedReport;
    }

    public void setQso_date(string qso_date)
    {
        this.qso_date = qso_date;
    }

    public void setTime_on(string time_on)
    {
        this.time_on = time_on;
    }
}