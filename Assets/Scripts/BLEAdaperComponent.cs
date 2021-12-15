using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.Android;


public class BLEAdaperComponent: AdapterBase
{
    public enum BKConnectionStatus
    {
        None = 0,
        Initialized,
        Scanning,
        DeviceDiscovered,
        DeviceConnected,
        ServiceScanning,
        ServiceConnected,
    }

    public const string MacAddrSZ = "BC:97:40:40:87:46";
    public const string MacAddrCD = "BC:97:40:40:87:CB";

    public ReactiveProperty<bool>  Connected { get;             private set; } = new ReactiveProperty<bool>(false);
    public ReactiveProperty<bool>  Scanning  { get;             private set; } = new ReactiveProperty<bool>(false);
    public ReactiveProperty<float> BleTimer  { get;             private set; } = new ReactiveProperty<float>(-1);
    public ReactiveProperty<BKConnectionStatus> BKStatus { get; private set; } =
        new ReactiveProperty<BKConnectionStatus>(BKConnectionStatus.None);

    private string ServiceUuid            = "69400001-b5a3-f393-e0a9-e50e24dcca99";
    private string DownCharacteristicUuid = "69400003-b5a3-f393-e0a9-e50e24dcca99";
    private string UpCharacteristicUuid   = "69400002-b5a3-f393-e0a9-e50e24dcca99";
    private string MacAddress;

    private float  _bleTimerStartedAt = 0;
    private bool _mtuRequested      = false;

    /// <summary>
    /// 扫描蓝牙并连接bk设备
    /// </summary>
    /// <param name="macAddress">连接对象的mac地址</param>
    /// <returns></returns>
    public async UniTask<Subject<int>> ScanToConnectBK(string macAddress)
    {
        //start timer
        _bleTimerStartedAt  = Time.fixedUnscaledTime;
        this.BleTimer.Value = 0;
        //connect type
        AdapterBase.ConnectedBy.Value = ConnectedType.BLUE_TOOTH;
        var s = new Subject<int>();            
        if(this.Scanning.Value || this.Connected.Value)
        {
            s.OnError(new Exception("连接中..."));
        }

        if(this.BKStatus.Value < BKConnectionStatus.Initialized)
        {
            var tsk = new UniTaskCompletionSource();
            BluetoothLEHardwareInterface.Initialize(
                true, false, () => { tsk.TrySetResult(); }, (e) =>
                {
                    var exception = new Exception(e);
                    s.OnError(exception);
                    tsk.TrySetException(exception);
                }
            );
            try
            {
                await tsk.Task;
                this.BKStatus.Value = BKConnectionStatus.Initialized;
            }
            catch(Exception e)
            {
                this.DisconnectBK();
                return s;
            }
        }

        this.Scanning.Value = false;
#if IOS
        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null,
            (s1, s2) => { },
            (addr, name, len, data) =>
            {
                if (data is { Length: 6 })
                {
                    if (BitConverter.ToString(data).Replace("-", ":") == macAddress)
                    {
                        this.BKStatus.Value = BKConnectionStatus.DeviceDiscovered;
                        BluetoothLEHardwareInterface.ConnectToPeripheral(addr, (theAddr) =>
                            {
                                this.Connected.Value = true;
                                this.MacAddress = theAddr;
                                this.BKStatus.Value = BKConnectionStatus.DeviceConnected;
                                this.BKStatus.Value = BKConnectionStatus.ServiceScanning;
                                Debug.Log($"设备连接:{theAddr}");
                            },
                            null,
                            (theAddr, theService, cUuid) =>
                            {
                                Debug.Log($"发现服务:{theService},发现特征:{cUuid}");
                                if (theService.ToLower().Equals(this.ServiceUuid) && cUuid.ToLower().Equals(this.DownCharacteristicUuid))
                                {
                                    this.BKStatus.Value = BKConnectionStatus.ServiceConnected;
                                    BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(addr, theService, cUuid,
                                        null,
                                        (deviceAddress, deviceCharacteristic, rawData) => { RecievedData(rawData); });
                                    BluetoothLEHardwareInterface.StopScan();
                                }
                            });
                    }
                }
            });
#else

        BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(
            null, (addr, deviceName) =>
            {
                this.Scanning.Value = true;
                this.BKStatus.Value = BKConnectionStatus.Scanning;
                if(addr.Equals(macAddress))
                {
                    this.BKStatus.Value = BKConnectionStatus.DeviceDiscovered;
                    BluetoothLEHardwareInterface.ConnectToPeripheral(
                        addr, (theAddr) =>
                        {
                            this.Connected.Value = true;
                            this.MacAddress      = theAddr;
                            this.BKStatus.Value  = BKConnectionStatus.DeviceConnected;
                            this.BKStatus.Value  = BKConnectionStatus.ServiceScanning;
                            Debug.Log($"设备连接:{theAddr}");
                        },
                        null,
                        (theAddr, theService, cUuid) =>
                        {
                            Debug.Log($"发现服务:{theService},发现特征:{cUuid}");
                            if(theService.Equals(this.ServiceUuid) && cUuid.Equals(this.DownCharacteristicUuid))
                            {
                                this.BKStatus.Value = BKConnectionStatus.ServiceConnected;
                                BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(
                                    addr, theService, cUuid,
                                    (p1, p2) =>
                                    {
                                        //stopped timer
                                        this.BleTimer.Value = Time.fixedUnscaledTime - _bleTimerStartedAt;
                                        if(!this._mtuRequested)
                                        {
                                            BluetoothLEHardwareInterface.RequestMtu(
                                                addr, 500, (str, num) => { Debug.Log($"request mtu callback {str} {num}"); }
                                            );
                                        }
                                        /*Debug.Log($"ble notification:${p1}:::${p2}");*/
                                    },
                                    (deviceAddress, deviceCharacteristic, rawData) =>
                                    {
                                        //Debug.Log($"ble onData:{deviceAddress}:::{deviceCharacteristic}");
                                        RecievedData(rawData);
                                    }
                                );
                                BluetoothLEHardwareInterface.StopScan();
                            }
                        }
                    );
                }
            }
        );
#endif
        return s;
    }

    /// <summary>
    /// 向bk发送数据
    /// </summary>
    /// <param name="rawData"></param>
    public override void SendToBk(sbyte[] rawData)
    {
        if(this.BKStatus.Value < BKConnectionStatus.ServiceConnected)
        {
            Debug.Log("无连接");
            return;
        }

        var sbytes = rawData.ToList();

        sbytes.Add((sbyte)BleProtocalsHelper.CalcCrc(rawData));

        //Debug.Log("开始发送: " + BitConverter.ToString((byte[]) (Array) sbytes.ToArray()));
        BluetoothLEHardwareInterface.WriteCharacteristic(
            this.MacAddress, this.ServiceUuid, this.UpCharacteristicUuid,
            sbytes.ToArray(), sbytes.Count, true, (s) =>
            {
                /*Debug.Log("发送成功");*/
            }
        );
    }

    public void DisconnectBK()
    {
        AdapterBase.ConnectedBy.Value = ConnectedType.NONE;
        BluetoothLEHardwareInterface.StopScan();
        BluetoothLEHardwareInterface.DisconnectPeripheral(this.MacAddress, mac => { });
        this.BKStatus.Value  = BKConnectionStatus.Initialized;
        this.Connected.Value = false;
        this.Scanning.Value  = false;
    }

    public override void RecievedData(byte[] bytes)
    {
        if(bytes.Length < 2)
        {
            return;
        }
        OnReceviedData(bytes);
    }
}