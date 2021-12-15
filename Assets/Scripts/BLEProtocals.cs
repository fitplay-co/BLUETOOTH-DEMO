using System;
using System.Collections.Generic;
using System.Reflection;

public enum ConverterType
{
    Default,
    IntConv,
    UIntConv,
    BoolConv,
    SteerConv,
    BrakeConv,
    BleSysStatusConv,
    BleOtaStatusConv,
    RtCadenceConv,
    ControllerKeyStatusConv,
    UsbStatConv,
}

public static class BleCmd
{
    public const sbyte Sid00ConfigCmd  = 0x00;
    public const sbyte Sid01QueryCmd   = 0x01;
    public const sbyte Sid02ControlCmd = 0x02;
    public const sbyte Sid03GameCmd    = 0x03;
    public const sbyte Sid04SsidCmd    = 0x04;
    public const sbyte Sid05WifiPwCmd  = 0x05;
    public const sbyte Sid06UsbStatCmd = 0x06;
    public const sbyte Sid07UpdateCmd  = 0x07;
}

public interface IBle2GameMessageBase
{
}

public class BleMessageItemAttribute: Attribute
{
    public int           StartAt { get; private set; }
    public int           Len           = 1;
    public ConverterType ConverterType = ConverterType.Default;

    public BleMessageItemAttribute(int startAt)
    {
        this.StartAt = startAt;
    }
}

#region MCU->APP数据包结构定义

/// <summary>
/// MCu->APP 配置参数应答数据 
/// </summary>
public struct BleSid00McuConfigMessage: IBle2GameMessageBase
{
    /// <summary>
    /// 霍尔踏频
    /// </summary>
    [BleMessageItem(3)]
    public uint CadenceHall;

    /// <summary>
    /// 角度传感器踏频
    /// </summary>
    [BleMessageItem(4)]
    public uint CandenceAngel;

    /// <summary>
    /// 电磁铁阻力档位，默认0x01
    /// </summary>
    [BleMessageItem(5)]
    public uint EmsGear;

    /// <summary>
    /// 电磁铁电流，单位mA
    /// </summary>
    [BleMessageItem(6, Len = 2)]
    public uint EmsCurrent;

    /// <summary>
    /// 电磁铁PWM占空比，0-100
    /// </summary>
    [BleMessageItem(8)]
    public uint EmsPwmDuty;

    /// <summary>
    /// 电磁铁温度，原始温度+40传输
    /// </summary>
    [BleMessageItem(9)]
    public uint EmsTemp;

    /// <summary>
    /// 电磁铁高斯值，单位：高斯
    /// </summary>
    [BleMessageItem(10, Len = 2)]
    public uint EmsGauss;

    /// <summary>
    /// 曲柄实时位置，0-360度范围
    /// </summary>
    [BleMessageItem(12, Len = 2)]
    public uint CrankAngel;

    [BleMessageItem(14)] public uint DutyAck;

    [BleMessageItem(15)] public uint NewtonAck;

    [BleMessageItem(16)] public uint ReserveAck1;

    [BleMessageItem(17)] public uint ReserveAck2;

    [BleMessageItem(18)] public uint ReserveAck3;

    [BleMessageItem(19)] public uint ReserveAck4;
}

/// <summary>
/// mcu->app 查询系统信息的返回数据
/// </summary>
public struct BleSid01McuStatusMessage: IBle2GameMessageBase
{
    public enum BleSysStatus
    {
        Unknown,
        Idle,
        Busy,
        Ready
    }

    public enum BleOtaStatus
    {
        WifiDisconnected = 10,
        WifiConnecting,
        WifiConnected,
        Updating,
        UpdateCompleted,
        UpdateFailed,
        NoNewVersion,
        DownloadFailed,
        NotEnouphSpace,
        UpdateCountLimited,
    }

    /// <summary>
    /// 中控硬件版本
    /// </summary>
    [BleMessageItem(3)]
    public uint HwVer;

    /// <summary>
    /// 中控软件版本
    /// </summary>
    [BleMessageItem(4, Len = 3)]
    public uint SwVer;

    /// <summary>
    /// 系统状态
    /// </summary>
    [BleMessageItem(8, ConverterType = ConverterType.BleSysStatusConv)]
    public BleSysStatus SysStatus;

    /// <summary>
    /// 升级状态
    /// </summary>
    [BleMessageItem(9, ConverterType = ConverterType.BleOtaStatusConv)]
    public BleOtaStatus OtaStatus;
}

/// <summary>
/// mcu->app 控制命令
/// </summary>
public struct BleSid02ControllerMessage: IBle2GameMessageBase
{
    public enum KeyStatus
    {
        Down,
        Up,
        Hold
    }

    /// <summary>
    /// 协议版本号
    /// </summary>
    [BleMessageItem(3)]
    public uint Ver;

    /// <summary>
    /// 心率 整数，次/分钟，范围：0~255
    /// </summary>
    [BleMessageItem(4)]
    public uint HeartRate;

    /// <summary>
    /// 转向角 浮点线性输入，范围：0~65535，中间值：32768
    /// </summary>
    [BleMessageItem(5, Len = 2, ConverterType = ConverterType.SteerConv)]
    public float Angle;

    /// <summary>
    /// 功率值，单位：瓦
    /// </summary>
    [BleMessageItem(7, Len = 2)]
    public uint Power;

    /// <summary>
    /// 实时踏频(上报间隔内的均值)，浮点数放大10倍(精度0.1)，单位：次/分钟
    /// </summary>
    [BleMessageItem(9, Len = 2, ConverterType = ConverterType.RtCadenceConv)]
    public float RealtimeCadence;

    /// <summary>
    /// 均值踏频(3秒平均值)，整数，次/分钟 (用不上)
    /// </summary>
    [BleMessageItem(11, Len = 2)]
    public uint AvgCadence;

    /// <summary>
    /// 整数，公里/小时 （用不上）
    /// </summary>

    //[BleMessageItem(13, Len = 2)]
    public uint Speed;

    /// <summary>
    /// 曲柄位置：0~360初始角度：45度（曲柄水平朝前为180度）
    /// </summary>
    [BleMessageItem(15, Len = 2)]
    public uint Crank;

    /// <summary>
    /// 前刹车 0~1放大65525倍
    /// </summary>
    [BleMessageItem(17, ConverterType = ConverterType.BrakeConv)]
    public float FBrake;

    /// <summary>
    /// 后刹车，浮点线性输入，0~1放大65525倍
    /// </summary>
    [BleMessageItem(19, ConverterType = ConverterType.BrakeConv)]
    public float RBrake;

    /// <summary>
    /// 摇杆ADC X轴值，范围：0~65535
    /// </summary>
    [BleMessageItem(21, ConverterType = ConverterType.SteerConv)]
    public float XJoyStick;

    /// <summary>
    /// 摇杆ADC Y轴值，范围：0~65535
    /// </summary>
    [BleMessageItem(23, ConverterType = ConverterType.SteerConv)]
    public float YJoyStick;

    [BleMessageItem(25, ConverterType = ConverterType.ControllerKeyStatusConv)]
    public KeyStatus AKey;

    [BleMessageItem(26, ConverterType = ConverterType.ControllerKeyStatusConv)]
    public KeyStatus BKey;

    [BleMessageItem(27, ConverterType = ConverterType.ControllerKeyStatusConv)]
    public KeyStatus PlusKey;

    [BleMessageItem(28, ConverterType = ConverterType.ControllerKeyStatusConv)]
    public KeyStatus MinusKey;

    [BleMessageItem(29, ConverterType = ConverterType.ControllerKeyStatusConv)]
    public KeyStatus MenuKey;
}

/// <summary>
/// mcu->app usb插拔状态
/// </summary>
public struct BleSid06UsbStatMessage: IBle2GameMessageBase
{
    public enum UsbStat
    {
        Pluged,
        Removed
    }

    [BleMessageItem(3, ConverterType = ConverterType.UsbStatConv)]
    public UsbStat Status;
}

#endregion

public static class BleProtocalsHelper
{
    private struct BleFieldInfo
    {
        public FieldInfo     F;
        public int           FieldIdx;
        public int           FieldLen;
        public ConverterType ConverterType;
    }

    private static Dictionary<string, Dictionary<string, BleFieldInfo>> _bleFieldInfoDic =
            new Dictionary<string, Dictionary<string, BleFieldInfo>>();

    public static T? ParseBle2GameMessage<T>(byte[] bytes) where T : struct, IBle2GameMessageBase
    {
        if(!CheckCrc(bytes))
        {
            return null;
        }

        var           msgLen     = (uint)bytes[2];
        var           ret        = new T();
        System.Object boxed      = ret;
        var           fieldInfos = GetFieldInfosOfT(ret);
        foreach(var entry in fieldInfos)
        {
            var idx = entry.Value.FieldIdx;
            var len = entry.Value.FieldLen;
            if(idx + len > msgLen)
            {
                return null;
            }

            switch(entry.Value.ConverterType)
            {
                case ConverterType.IntConv:
                    int intVal = 0;
                    for(var i = 0; i < len; i++)
                    {
                        if(i == len - 1)
                        {
                            intVal += ((sbyte)bytes[idx + i]) << (i * 8);
                        }
                        else
                        {
                            intVal += bytes[idx + i] << (i * 8);
                        }
                    }

                    entry.Value.F.SetValue(boxed, intVal);
                    break;
                case ConverterType.UIntConv:
                    uint uintVal = 0;
                    for(var i = 0; i < len; i++)
                    {
                        uintVal += ((uint)bytes[idx + i]) << (i * 8);
                    }

                    entry.Value.F.SetValue(boxed, uintVal);
                    break;
                case ConverterType.BoolConv:
                    entry.Value.F.SetValue(boxed, (uint)bytes[idx] > 0);
                    break;
                case ConverterType.SteerConv:
                    entry.Value.F.SetValue(boxed, (((int)((uint)bytes[idx + 1] << 8) + bytes[idx]) - 32768) / 32768f);
                    break;
                case ConverterType.RtCadenceConv:
                    entry.Value.F.SetValue(boxed, ((uint)(bytes[idx + 1] << 8) + bytes[idx]) / 10f);
                    break;
                case ConverterType.BrakeConv:
                    entry.Value.F.SetValue(boxed, ((uint)(bytes[idx + 1] << 8) + bytes[idx]) / 65535f);
                    break;
                case ConverterType.BleSysStatusConv:
                    entry.Value.F.SetValue(boxed, (BleSid01McuStatusMessage.BleSysStatus)bytes[idx]);
                    break;
                case ConverterType.BleOtaStatusConv:
                    entry.Value.F.SetValue(boxed, (BleSid01McuStatusMessage.BleOtaStatus)bytes[idx]);
                    break;
                case ConverterType.ControllerKeyStatusConv:
                    entry.Value.F.SetValue(boxed, (BleSid02ControllerMessage.KeyStatus)bytes[idx]);
                    break;
                case ConverterType.UsbStatConv:
                    entry.Value.F.SetValue(boxed, (BleSid06UsbStatMessage.UsbStat)bytes[idx]);
                    break;
            }
        }

        return (T)boxed;
    }

    /// <summary>
    /// app->mcu 配置参数消息
    /// </summary>
    /// <returns></returns>
    public static sbyte[] CreateSid00McuConfigMessage()
    {
        //0|0xAA|1|HEAD
        //1|0x00|1|CMD
        //2|0x0D|1|len
        //3|sbyte|1|配置标识项 duty_set
        //4|sbyte|1|PWM百分比 duty_value
        //5|sbyte|1|配置标识项 newton_set
        //6|sword|2|newton_value
        //8|sbyte|1|reserve
        //9|sbyte|1|reserve
        //10|sbyte|1|reserve
        //11|sbyte|1|reserve
        return new sbyte[] { };
    }

    /// <summary>
    /// app->mcu 查询ble系统信息的协议
    /// </summary>
    /// <returns></returns>
    public static sbyte[] CreateSid01QueryStatusMessage()
    {
        return new sbyte[] { unchecked((sbyte)0xAA), BleCmd.Sid01QueryCmd, 0x04 };
    }

    /// <summary>
    /// 下发软件车状态的协议
    /// </summary>
    /// <param name="gear1"></param>
    /// <param name="gear2"></param>
    /// <param name="resistance"></param>
    /// <param name="vibsel"></param>
    /// <param name="cadence"></param>
    /// <param name="ver"></param>
    /// <returns></returns>
    public static sbyte[] CreateSid03GameParamsMessage(int gear1, int gear2, int resistance, uint vibsel, uint cadence, uint ver = 0x01)
    {
        //0|0xAA|1|HEAD
        //1|0x03|1|CMD
        //2|0x0C|1|LEN
        //3|0x01|1|ver
        //4|sword|2|cadence
        //6|sbyte|1|gear1
        //7|sbyte|1|gear2
        //8|sword|2|resistance
        //10|byte|1|unused
        return new sbyte[]
        {
            unchecked((sbyte)0xAA), BleCmd.Sid03GameCmd, 0x0C, (sbyte)ver, (sbyte)cadence, (sbyte)((cadence & 0xFF00) >> 8), (sbyte)gear1,
            (sbyte)gear2, (sbyte)(resistance), (sbyte)((resistance & 0xFF00) >> 8), (sbyte)vibsel
        };
    }

    /// <summary>
    /// 设置wifi ssid的协议
    /// </summary>
    /// <param name="ssid"></param>
    /// <returns></returns>
    public static sbyte[] CreateSid04SsidMessage(string ssid)
    {
        //0|0xAA|1|HEAD
        //1|0x04|1|CMD
        //2|0x44|1|LEN
        //3|sbytes|64|ssid
        var bytes = System.Text.Encoding.UTF8.GetBytes(ssid);
        if(bytes.Length > 64)
        {
            throw new InconsistParamException("ssid不能超过64个字符");
        }

        var ret = new sbyte[67];
        ret[0] = unchecked((sbyte)0xAA);
        ret[1] = BleCmd.Sid04SsidCmd;
        ret[2] = 0x44;
        Buffer.BlockCopy(bytes, 0, ret, 3, bytes.Length);
        return ret;
    }

    /// <summary>
    /// 设置wifi密码的协议
    /// </summary>
    /// <param name="wifiPassword"></param>
    /// <returns></returns>
    /// <exception cref="InconsistParamException"></exception>
    public static sbyte[] CreateSid05WifiPwMessage(string wifiPassword)
    {
        //0|0xAA|1|HEAD
        //1|0x04|1|CMD
        //2|0x44|1|LEN
        //3|sbytes|64|pw
        if(System.Text.Encoding.UTF8.GetByteCount(wifiPassword) != wifiPassword.Length)
        {
            throw new InconsistParamException("不支持非英文和数字的wifi 密码");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(wifiPassword);

        if(bytes.Length > 64)
        {
            throw new InconsistParamException("密码不能超过64个字符");
        }

        var ret = new sbyte[67];
        ret[0] = unchecked((sbyte)0xAA);
        ret[1] = BleCmd.Sid05WifiPwCmd;
        ret[2] = 0x44;
        Buffer.BlockCopy(bytes, 0, ret, 3, bytes.Length);
        return ret;
    }

    public static sbyte[] CreateReqUpdateMessage()
    {
        return new sbyte[] { unchecked((sbyte)0xAA), BleCmd.Sid07UpdateCmd, 0x04 };
    }

    private static Dictionary<string, BleFieldInfo> GetFieldInfosOfT<T>(T obj) where T : struct, IBle2GameMessageBase
    {
        var typeName = typeof(T).FullName;
        if(!_bleFieldInfoDic.ContainsKey(typeName))
        {
            var fieldInfos = new Dictionary<string, BleFieldInfo>();
            var fields     = typeof(T).GetFields();
            foreach(FieldInfo f in fields)
            {
                foreach(Attribute a in f.GetCustomAttributes())
                {
                    if(a is BleMessageItemAttribute ba)
                    {
                        var convType = ba.ConverterType;
                        if(convType == ConverterType.Default)
                        {
                            if(f.FieldType == typeof(int))
                            {
                                convType = ConverterType.IntConv;
                            }
                            else if(f.FieldType == typeof(uint))
                            {
                                convType = ConverterType.UIntConv;
                            }
                            else if(f.FieldType == typeof(bool))
                            {
                                convType = ConverterType.BoolConv;
                            }
                        }

                        fieldInfos[f.Name] = new BleFieldInfo() { F = f, FieldIdx = ba.StartAt, FieldLen = ba.Len, ConverterType = convType };
                    }
                }
            }

            _bleFieldInfoDic[typeName] = fieldInfos;
        }

        return _bleFieldInfoDic[typeName];
    }

    public static byte CalcCrc(sbyte[] rawData)
    {
        byte b = 0;
        for(int i = 0; i < rawData[2] - 1; i++)
        {
            b += (byte)rawData[i];
        }

        return b;
    }

    public static bool CheckCrc(byte[] rawData)
    {
        byte b = 0;
        for(int i = 0; i < rawData[2] - 1; i++)
        {
            b += rawData[i];
        }

        return b == rawData[rawData[2] - 1];
    }
}