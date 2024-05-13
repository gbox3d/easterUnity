using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System;
using JetBrains.Annotations;

public class SerialPortManager
{
    SerialPort serialPort;
    CancellationTokenSource cancellationTokenSource;

    public Action<string> OnReceivedData;


    public SerialPortManager(string portName, int baudRate, Action<string> onReceivedData)
    {
        OnReceivedData = onReceivedData;
        OpenSerialPort(portName, baudRate);
    }

    ~SerialPortManager()
    {
        CloseSerialPort();
    }

    public async void OpenSerialPort(string portName, int baudRate)
    {
        serialPort = new SerialPort(portName, baudRate);

        if (!serialPort.IsOpen)
        {
            cancellationTokenSource = new CancellationTokenSource();

            serialPort.Open();
            Debug.Log("시리얼 포트가 연결되었습니다.");

            try
            {
                await ReadSerialPortDataAsync(cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                Debug.Log("시리얼 포트 읽기가 취소되었습니다.");
            }
            finally
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                Debug.Log("시리얼 포트가 닫혔습니다.");
            }

            Debug.Log("시리얼 포트가 닫혔습니다.");
        }
        else {

            Debug.Log("시리얼 포트가 이미 연결되어 있습니다.");

        }

    }

    public void WriteSerialPort(string data)
    {
        if (serialPort.IsOpen)
        {
            serialPort.WriteLine(data);
            Debug.Log("데이터 전송: " + data);
        }
        else
        {
            Debug.LogError("시리얼 포트가 연결되어 있지 않습니다.");
        }
    }

    // 시리얼 포트 닫기
    public void CloseSerialPort()
    {
        if (cancellationTokenSource != null)
        {
            Debug.Log("시리얼 포트 닫기");
            cancellationTokenSource.Cancel();
            serialPort.Close();
        }
    }


    async Task ReadSerialPortDataAsync(CancellationToken cancellationToken)
    {
        while (serialPort.IsOpen && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                string data = await Task.Run(() => serialPort.ReadLine(), cancellationToken);
                // Debug.Log("수신된 데이터: " + data);
                OnReceivedData?.Invoke(data);

            }
            catch (System.TimeoutException)
            {
                Debug.Log("읽기 타임아웃: 데이터를 수신하지 못했습니다.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("IO 예외 발생: " + ex.Message);
                break;
            }
        }
    }

    static public string [] GetPortNames()
    {
        return SerialPort.GetPortNames();
    }

}
