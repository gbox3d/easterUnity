using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System;
using JetBrains.Annotations;
using System.IO;
using System.Text;
using Unity.VisualScripting;

//
public class SerialPortManager : IDisposable
{
    SerialPort serialPort;
    CancellationTokenSource cancellationTokenSource;

    public Action<string> OnReceivedData { get; set; }
    public Action<string> OnError { get; set; }
    public Action<string> OnTimeout { get; set; }

    public int fire_count;
    public int mode_switch;
    public float battery;
    public int gun_status;
    public float quat0;
    public float quat1;
    public float quat2;
    public float quat3;

    public SerialPortManager()
    {
    }

    public SerialPortManager(string portName, int baudRate, Action<string> onReceivedData, int timeout = 0)
    {
        OnReceivedData = onReceivedData;

        OpenSerialPort(portName, baudRate, timeout);
    }

    ~SerialPortManager()
    {
        CloseSerialPort();
    }

    public async void OpenSerialPort(string portName, int baudRate, int timeout = 0)
    {
        if (timeout > 0)
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = timeout;
        }
        else
            serialPort = new SerialPort(portName, baudRate);

        if (!serialPort.IsOpen)
        {
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                serialPort.Open();
                Debug.Log("시리얼 포트가 연결되었습니다. : " + portName);
                // await ReadSerialPortDataAsync(cancellationTokenSource.Token);
                await ReadSerialPortDataAsync_NonBlock(cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                Debug.Log("시리얼 포트 읽기가 취소되었습니다.");

            }
            catch (System.Exception ex)
            {
                Debug.LogError("시리얼 포트 연결 오류: " + ex.Message);
                OnError?.Invoke(ex.Message);
            }
            finally
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                // Debug.Log("시리얼 포트가 닫혔습니다.");
            }

            Debug.Log("port closed.");
        }
        else
        {
            Debug.Log("port is already opened.");
        }

    }

    public bool checkPort_moai_Recriver(string portName, int baudRate)
    {
        if (serialPort != null)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        try
        {
            serialPort = new SerialPort(portName, baudRate)
            {
                ReadTimeout = 1000
            };

            serialPort.Open();

            Debug.Log("시리얼 포트가 연결되었습니다. : " + portName);

            //delay
            Thread.Sleep(500);

            serialPort.WriteLine("about");


            while (true)
            {
                string data = serialPort.ReadLine();

                if (data.StartsWith('%'))
                {
                    //moai reciver
                    if (data.Contains("moai receiver"))
                    {
                        Debug.Log("moai_Recriver found.");
                        Debug.Log("data: " + data);
                        return true;
                    }
                }
            }
        }
        catch (TimeoutException)
        {
            Debug.Log("timeout exception occured. ");
            // return false;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            // return false;
        }
        finally
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }


        return false;
    }

    public bool checkPort_moai_DMP(string portName, int baudRate)
    {
        if (serialPort != null)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }

        try
        {
            serialPort = new SerialPort(portName, baudRate)
            {
                ReadTimeout = 1000
            };

            serialPort.Open();

            Debug.Log("시리얼 포트가 연결되었습니다. : " + portName);

            //delay
            Thread.Sleep(3000);

            serialPort.WriteLine("help");


            while (true)
            {
                string data = serialPort.ReadLine();

                Debug.Log("data: " + data);

                if(data.Contains(" MOAI-C3 (DMP)"))
                {
                    Debug.Log("DMP found.");
                    return true;
                }
            }
        }
        catch (TimeoutException)
        {
            Debug.Log("timeout exception occured. ");
            // return false;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            // return false;
        }
        finally
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }


        return false;
    }

    public void ParseAndMoaiReceiverFormat(string data)
    {

        // data = data.Trim();
        // 데이터를 공백으로 분리
        string[] parts = data.Trim().Split(' ');

        if (parts.Length != 8)
        {
            // Console.WriteLine("입력 데이터가 형식에 맞지 않습니다.");
            Debug.Log("입력 데이터가 형식에 맞지 않습니다. " + parts.Length + "개의 데이터가 들어왔습니다.");
            return;
        }

        try
        {
            // 각 부분을 적절한 타입으로 변환
            fire_count = int.Parse(parts[0]);
            mode_switch = int.Parse(parts[1]);
            gun_status = int.Parse(parts[2]);
            battery = float.Parse(parts[3]);
            
            quat0 = float.Parse(parts[4]);
            quat1 = float.Parse(parts[5]);
            quat2 = float.Parse(parts[6]);
            quat3 = float.Parse(parts[7]);

        }
        catch (FormatException)
        {
            Debug.Log("데이터 파싱 중 오류가 발생했습니다. 데이터 형식을 확인해주세요.");
            // Console.WriteLine("데이터 파싱 중 오류가 발생했습니다. 데이터 형식을 확인해주세요.");
        }
    }

    public void DiscardInBuffer()
    {
        serialPort.DiscardInBuffer();
    }

    public void WriteSerialPort(string data)
    {
        if (serialPort.IsOpen)
        {
            serialPort.WriteLine(data);
            // Debug.Log("데이터 전송: " + data);
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
            // Debug.Log("시리얼 포트 닫기");
            cancellationTokenSource.Cancel();
            serialPort.Close();

            Debug.Log("CloseSerialPort called.");
        }
    }

    async Task ReadSerialPortDataAsync_NonBlock(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var stringBuilder = new StringBuilder();

        while (serialPort.IsOpen && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (serialPort.BytesToRead == 0)
                {
                    await Task.Delay(100);
                    continue;
                }

                int bytesRead = await serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead > 0)
                {
                    string data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    stringBuilder.Append(data);
                    if (data.Contains("\n"))
                    {
                        // \n 이 여러개 있으면 잘라서 처리 후 여러번 호출
                        string completeData = stringBuilder.ToString();
                        stringBuilder.Clear();

                        //split by \n
                        string[] datas = completeData.Split('\n');
                        foreach (var d in datas)
                        {
                            if (d.Length > 0)
                            {
                                OnReceivedData?.Invoke(d);
                            }
                        }


                        //OnReceivedData?.Invoke(completeData);
                    }
                }
            }
            catch (TimeoutException)
            {
                // Debug.Log("읽기 타임아웃: 데이터를 수신하지 못했습니다.");
                OnTimeout?.Invoke("timeout");
                // break;
            }
            catch (Exception ex)
            {
                Debug.LogError("IO Exception : " + ex.Message);
                OnError?.Invoke(ex.Message);
                break;
            }
        }

        serialPort.Close();

        Debug.Log("ReadSerialPortDataAsync_NonBlock finished.");
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
                OnTimeout?.Invoke(" timeout exception occured. ");
                // break;

            }
            catch (System.Exception ex)
            {
                Debug.LogError("IO 예외 발생: " + ex.Message);
                OnError?.Invoke(ex.Message);
                // break;
            }
        }
    }

    static public string[] GetPortNames()
    {
        return SerialPort.GetPortNames();
    }

    public bool IsOpen()
    {
        return serialPort.IsOpen;
    }

    public void Dispose()
    {
        CloseSerialPort();
    }


}
