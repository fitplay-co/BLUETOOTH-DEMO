/// <summary>
/// ble发给单车的数据包
/// </summary>
public struct Ble2GameMessage
{
    /// <summary>
    /// 0xAA
    /// </summary>
    public byte head;

    /// <summary>
    /// 0x02
    /// </summary>
    public byte cmd;

    /// <summary>
    /// 0x12
    /// </summary>
    public byte len;

    /// <summary>
    /// 转向角
    /// </summary>
    public float steer;

    /// <summary>
    /// 功率值，单位：瓦
    /// </summary>
    public uint power;

    /// <summary>
    /// 前刹车
    /// </summary>
    public float fBrake;

    /// <summary>
    /// 后刹车，浮点线性输入，0~1放大100倍
    /// </summary>
    public float rBrake;

    /// <summary>
    /// 重置 0或1， 按下／弹开
    /// </summary>
    public bool reset;

    /// <summary>
    /// 踏频 整数，次/分钟
    /// </summary>
    public uint cadence;

    /// <summary>
    /// 整数，公里/小时
    /// </summary>
    public uint speed;

    /// <summary>
    /// 档位1，正表示加档，负数表示减档
    /// </summary>
    public int gear1;

    /// <summary>
    /// 档位2，正表示加档，负数表示减档
    /// </summary>
    public int gear2;

    /// <summary>
    /// 循环检校
    /// </summary>
    public int check;
}

public struct Game2BleMessage
{
    /// <summary>
    /// 0xAA
    /// </summary>
    public byte head;

    /// <summary>
    /// 0x02
    /// </summary>
    public byte cmd;

    /// <summary>
    /// 0x12
    /// </summary>
    public byte len;

    /// <summary>
    /// 档位1，正表示加档，负数表示减档
    /// </summary>
    public sbyte gear1;

    /// <summary>
    /// 档位2，正表示加档，负数表示减档
    /// </summary>
    public sbyte gear2;

    /// <summary>
    /// 外部阻力，单位：牛
    /// </summary>
    public short resistance;
}

public static class BLEProtocalsHelper
{
    public static sbyte[] CreateGame2BleMessage(int gear1, int gear2, int resistance, uint vibsel)
    {
        return new sbyte[]
        {
            unchecked ((sbyte) 0xAA), 
            0x03, 
            0x09, 
            (sbyte) gear1, 
            (sbyte) gear2, 
            (sbyte) ((resistance & 0xFF00) >> 8),
            (sbyte) (resistance), 
            (sbyte) vibsel
        };
    }

    public static Ble2GameMessage? ParseBleMessage(byte[] bytes)
    {
        if (bytes.Length < 15)
        {
            return null;
        }

        return new Ble2GameMessage()
        {
            steer = (((int) ((uint) bytes[3] << 8) + bytes[4]) - 500) / 500f,
            power = (((uint) bytes[5] << 8) + bytes[6]),
            fBrake = bytes[7] / 100f,
            rBrake = bytes[8] / 100f,
            reset = (uint) bytes[9] > 0,
            cadence = (((uint) bytes[10] << 8) + bytes[11]),
            speed = (((uint) bytes[12] << 8) + bytes[13]),
            gear1 = (sbyte) bytes[14],
            gear2 = (sbyte) bytes[15]
        };
    }
}