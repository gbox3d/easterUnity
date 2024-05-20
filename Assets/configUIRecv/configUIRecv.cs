using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// using System.IO.Ports;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Threading;


public class configUIRecv : MonoBehaviour
{
    SerialPortManager serialPortManager = new();

    [SerializeField] Button btnScan;
    [SerializeField] Button btnConnect;

    [SerializeField] Button btnSend;

    [SerializeField] TMP_Dropdown dropdown_PortList;
    [SerializeField] TMP_Text txt_Log;
    [SerializeField] TMP_Text txt_Info;
    [SerializeField] TMP_InputField inputField_Command;

    // CancellationTokenSource cancellationTokenSource = null;

    private SynchronizationContext unityContext;


    // Start is called before the first frame update
    void Start()
    {
        unityContext = SynchronizationContext.Current;

        btnScan.onClick.AddListener( async () =>
        {
            btnScan.interactable = false;

            // cancellationTokenSource = new CancellationTokenSource();

            string[] ports = SerialPortManager.GetPortNames(); // 사용 가능한 포트 리스트 가져오기

            txt_Log.text = "";

            List<string> portList = new List<string>();

            foreach (string port in ports)
            {
                if (port.Contains("tty."))
                {
                    //Bluetooth 포트만 필터링
                    if (port.Contains("Bluetooth"))
                    {
                        continue;
                    }
                    // "tty."를 포함하는 포트만 Dropdown 옵션으로 추가
                    portList.Add(port);
                }
                else if (port.Contains("COM"))
                {
                    portList.Add(port);
                }
            }



            List<TMP_Dropdown.OptionData> filteredPorts = new List<TMP_Dropdown.OptionData>(); // OptionData 리스트 생성
            dropdown_PortList.options.Clear();

            foreach (string port in portList)
            {
                Debug.Log("check Port: " + port);
                txt_Log.text += "Port : " + port + " opeining... \n";
                await Task.Run( () =>
                {
                    if(serialPortManager.checkPort_moai_Recriver(port,115200))
                    {
                        filteredPorts.Add(new TMP_Dropdown.OptionData(port));
                        
                        unityContext.Post((state) =>
                        {
                            txt_Log.text += "found moai_Recriver. " + port + "\n";
                        }, null);


                        // await Task.Delay(1000);
                    }
                });

                txt_Log.text += "port closed. " + port + "\n";
                
            }

            dropdown_PortList.AddOptions(filteredPorts); // 필터링된 포트 리스트를 Dropdown에 추가

            btnScan.interactable = true;
            Debug.Log("Scan completed.");



            // Dropdown에 포트 리스트 추가
            // foreach (string port in portList)
            // {
            //     if (cancellationTokenSource.Token.IsCancellationRequested)
            //     {
            //         break;
            //     }

            //     await Task.Delay(100);

            //     string strData = "";
            //     bool taskCompleted = false;

            //     int nFsm = 0;

            //     Debug.Log("Port: " + port);

            //     txt_Log.text += "Port : " + port + " opeining... \n";

            //     using (var serialPortManager = new SerialPortManager())
            //     {
            //         serialPortManager.OnReceivedData = (string data) =>
            //         {
            //             // strData += data;
            //             // Debug.Log("Data: " + data);
            //             switch (nFsm)
            //             {
            //                 case 0:
            //                     if (data.StartsWith('%'))
            //                     {
            //                         nFsm = 1;
            //                         strData = data + "\n";
            //                     }
            //                     break;
            //                 case 1:
            //                     if (data.Contains("OK"))
            //                     {
            //                         taskCompleted = true;

            //                         if (strData.Contains("moai reciver"))
            //                         {
            //                             dropdown_PortList.options.Add(new TMP_Dropdown.OptionData(port));
            //                         }
            //                     }
            //                     else
            //                     {
            //                         strData += data + "\n";
            //                     }
            //                     break;
            //             }
            //         };
            //         serialPortManager.OnError = (string error) =>
            //         {
            //             strData = "err:" + error;
            //             taskCompleted = true;
            //         };

            //         serialPortManager.OnTimeout = (string timeout) =>
            //         {
            //             strData = "timeout : " + timeout;
            //             taskCompleted = true;
            //         };

            //         await Task.Delay(1000);

            //         serialPortManager.OpenSerialPort(port, 115200, 5000);

            //         if (!serialPortManager.IsOpen())
            //         {
            //             txt_Log.text += "port open failed. " + port + "\n";
            //             continue;
            //         }

            //         serialPortManager.WriteSerialPort("about\r\n");

            //         await Task.Run(async () =>
            //         {
            //             while (!taskCompleted)
            //             {
            //                 await Task.Delay(10); // 짧은 지연을 주어 계속 대기
            //             }
            //         });

            //         serialPortManager.CloseSerialPort();

            //         txt_Log.text += "port closed. " + port + "\n";
            //     }

            //     Debug.Log(strData);
            //     Debug.Log("-------------------- : " + port + "---------------------");

            //     txt_Log.text += "\n";
            //     txt_Log.text += strData + "  :  " + port + "\n";
            // }

            // cancellationTokenSource = null;

            // btnScan.interactable = true;

            // txt_Log.text += "Scan completed.\n";
        });
    
        btnConnect.onClick.AddListener( () =>
        {
            if (serialPortManager.IsOpen())
            {
                txt_Log.text += "Already connected.\n";
                serialPortManager.CloseSerialPort();
                btnConnect.GetComponentInChildren<TMP_Text>().text = "Connect";

                // cancellationTokenSource.Cancel();

                return;
            }

            txt_Log.text = "";
            if (dropdown_PortList.options.Count == 0)
            {
                txt_Log.text += "No port selected.\n";
                return;
            }

            string portName = dropdown_PortList.options[dropdown_PortList.value].text;

            txt_Log.text += "Connecting to " + portName + "\n";

            serialPortManager.OpenSerialPort(portName, 115200);

            if (!serialPortManager.IsOpen())
            {
                txt_Log.text += "Failed to connect to " + portName + "\n";
                return;
            }

            txt_Log.text += "Connected to " + portName + "\n";

            serialPortManager.OnReceivedData = (string data) =>
            {
                // \r 문자 제거
                data = data.Replace("\r", "");

                if(data.StartsWith("%"))
                {
                    txt_Log.text += data + "\n";
                }
                else if(data.Contains("#"))
                {
                    txt_Log.text += data + "\n";
                }
                else if(data.Contains("$"))
                {
                    serialPortManager.ParseAndMoaiReceiverFormat(data[1..]);

                    txt_Info.text = "fire_count: " + serialPortManager.fire_count + ", ";
                    txt_Info.text += "mode_switch: " + serialPortManager.mode_switch + ", ";
                    txt_Info.text += "battery: " + serialPortManager.battery + ", ";
                    txt_Info.text += "gun_status: " + serialPortManager.gun_status;
                    txt_Info.text += "\n";
                }
                
            };

            serialPortManager.OnError = (string error) =>
            {
                txt_Log.text += "Error: " + error + "\n";
                // unityContext.Post((state) =>
                // {
                //     txt_Log.text += "Error: " + error + "\n";
                // }, null);
            };

            serialPortManager.OnTimeout = (string timeout) =>
            {
                txt_Log.text += "Timeout: " + timeout + "\n";
                // unityContext.Post((state) =>
                // {
                //     txt_Log.text += "Timeout: " + timeout + "\n";
                // }, null);
            };

            //'disconnect text' button
            btnConnect.GetComponentInChildren<TMP_Text>().text = "Disconnect";


            //task runnning
            // await Task.Run(async () =>
            // {
            //     while (true)
            //     {
            //         if (cancellationTokenSource.Token.IsCancellationRequested)
            //         {
            //             break;
            //         }

            //         await Task.Delay(10);
            //     }
            // });

            // Debug.Log("Task completed.");

        });

        btnSend.onClick.AddListener( () =>
        {
            if (!serialPortManager.IsOpen())
            {
                txt_Log.text += "Not connected.\n";
                return;
            }

            string command = inputField_Command.text;

            if (string.IsNullOrEmpty(command))
            {
                txt_Log.text += "No command entered.\n";
                return;
            }

            txt_Log.text = "Sending command: " + command + "\n";

            serialPortManager.WriteSerialPort(command + "\r\n");
        });
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDestroy()
    {
        serialPortManager.Dispose();

        // if (cancellationTokenSource != null)
        // {
        //     cancellationTokenSource.Cancel();
        // }
    }
}
