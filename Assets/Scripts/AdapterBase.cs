using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;


public enum ConnectedType
{
    NONE,
    BLUE_TOOTH,
    USB
}

public abstract class AdapterBase
{
    private List<Action<BleSid02ControllerMessage?>> _onGameCmdActions   = new List<Action<BleSid02ControllerMessage?>>();
    private List<Action<BleSid01McuStatusMessage?>>  _onQueryCmdActions  = new List<Action<BleSid01McuStatusMessage?>>();
    private List<Action<BleSid00McuConfigMessage?>>  _onMcuConfigActions = new List<Action<BleSid00McuConfigMessage?>>();
    private List<Action<BleSid06UsbStatMessage?>>    _onUsbStatActions   = new List<Action<BleSid06UsbStatMessage?>>();

    public static ReactiveProperty<ConnectedType> ConnectedBy { get; private set; } = new ReactiveProperty<ConnectedType>(ConnectedType.BLUE_TOOTH);

    //发送数据
    public abstract void SendToBk(sbyte[] dataStr); 
    public abstract void RecievedData(byte[] dataStr);
    
    /// <summary>
    /// 对收到的数据的处理
    /// </summary>
    /// <param name="rawData"></param>
    protected void OnReceviedData(byte[] rawData)
    {
        //LogHelper.LogDebug($"bk data: {BitConverter.ToString(rawData)}");
        switch((sbyte)rawData[1])
        {
            case BleCmd.Sid02ControlCmd:
                foreach(var onGameCmdAction in this._onGameCmdActions)
                {
                    onGameCmdAction(BleProtocalsHelper.ParseBle2GameMessage<BleSid02ControllerMessage>(rawData));
                }

                break;
            case BleCmd.Sid01QueryCmd:
                foreach(var onQueryCmdAction in this._onQueryCmdActions)
                {
                    onQueryCmdAction(BleProtocalsHelper.ParseBle2GameMessage<BleSid01McuStatusMessage>(rawData));
                }

                break;
            case BleCmd.Sid00ConfigCmd:
                foreach(var onMcuConfigAction in this._onMcuConfigActions)
                {
                    onMcuConfigAction(BleProtocalsHelper.ParseBle2GameMessage<BleSid00McuConfigMessage>(rawData));
                }

                break;
            case BleCmd.Sid06UsbStatCmd:
                foreach(var onUsbStatAction in this._onUsbStatActions)
                {
                    onUsbStatAction(BleProtocalsHelper.ParseBle2GameMessage<BleSid06UsbStatMessage>(rawData));
                }

                break;
            case BleCmd.Sid03GameCmd:
            case BleCmd.Sid07UpdateCmd:
            case BleCmd.Sid04SsidCmd:
            case BleCmd.Sid05WifiPwCmd:
                //LogHelper.LogDebug($"ble data: {BitConverter.ToString(rawData)}");
                break;
        }
    }

    public void AddOnGameCmdHandler(Action<BleSid02ControllerMessage?> handler)
    {
        this._onGameCmdActions.Add(handler);
    }

    public void RemoveOnGameCmdHandler(Action<BleSid02ControllerMessage?> handler)
    {
        this._onGameCmdActions.Remove(handler);
    }

    public void AddOnQueryCmdHandler(Action<BleSid01McuStatusMessage?> handler)
    {
        this._onQueryCmdActions.Add(handler);
    }

    public void RemoveOnQueryCmdHandler(Action<BleSid01McuStatusMessage?> handler)
    {
        this._onQueryCmdActions.Remove(handler);
    }

    public void RemoveAllOnGameCmdHandler()
    {
        this._onGameCmdActions.Clear();
    }

    public void RemoveAllOnQueryCmdHandler()
    {
        this._onQueryCmdActions.Clear();
    }

    public void AddOnMcuConfigHandler(Action<BleSid00McuConfigMessage?> handler)
    {
        this._onMcuConfigActions.Add(handler);
    }

    public void RemoveOnMcuConfigHandler(Action<BleSid00McuConfigMessage?> handler)
    {
        this._onMcuConfigActions.Remove(handler);
    }

    public void AddOnUsbStatHandler(Action<BleSid06UsbStatMessage?> handler)
    {
        this._onUsbStatActions.Add(handler);
    }

    public void RemoveOnUsbStatHandler(Action<BleSid06UsbStatMessage?> handler)
    {
        this._onUsbStatActions.Remove(handler);
    }

    public void RemoveAllHandler()
    {
        this._onGameCmdActions.Clear();
        this._onQueryCmdActions.Clear();
        this._onMcuConfigActions.Clear();
        this._onUsbStatActions.Clear();
    }
}