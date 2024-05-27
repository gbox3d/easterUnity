using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.IO.Ports;
using TMPro;
using System;
using System.IO;
using System.Threading.Tasks;


public class configUI_main : MonoBehaviour
{
    //button 
    [SerializeField] Button btn_Scan;
    [SerializeField] Button btnConnect;
    [SerializeField] Button btn_Read;
    [SerializeField] Button btn_Write;
    [SerializeField] Button btn_Reboot;
    [SerializeField] Button btn_ChckVersion;


    [SerializeField] TMP_Dropdown dropdown_PortList;
    [SerializeField] TMP_Text txt_Status;
    [SerializeField] TMP_Text txt_firmwareVersion;

    [SerializeField] TMP_InputField input_ssid;
    [SerializeField] TMP_InputField input_password;
    [SerializeField] TMP_InputField input_targetIp;
    [SerializeField] TMP_InputField input_targetPort;
    [SerializeField] TMP_InputField input_deviceNumber;
    [SerializeField] TMP_InputField input_triggerDelay;
    [SerializeField] Toggle toggle_IsUseImu;

    SerialPort serialPort;

    SerialPortManager serialPortManager = new();

    public Action<string> OnReceivedOk; // "OK" 수신 시 호출될 콜백
    public Action<string> OnTimeout; // 타임아웃 시 호출될 콜백
    public Action<string> OnError; // 에러 발생 시 호출될 콜백


    public void ParseData(string data)
    {
        string[] lines = data.Split('\n'); // 데이터를 줄 단위로 나눕니다.

        foreach (var line in lines)
        {
            if (line.Contains(":"))
            {
                string[] parts = line.Split(new string[] { ": " }, StringSplitOptions.None);
                string key = parts[0].Trim();
                string value = parts[1].Trim();

                switch (key)
                {
                    case "mStrAp":
                        input_ssid.text = value;
                        break;
                    case "mStrPassword":
                        input_password.text = value;
                        break;
                    case "mTargetIp":
                        input_targetIp.text = value;
                        break;
                    case "mTargetPort":
                        input_targetPort.text = value;
                        break;
                    case "mDeviceNumber":
                        input_deviceNumber.text = value;
                        break;
                    case "mTriggerDelay":
                        input_triggerDelay.text = value;
                        break;
                    case "mIsUseImu" :
                        toggle_IsUseImu.isOn = value == "1" ? true : false;
                        break;
                    default:
                        
                        break;
                }
            }
            else if(data.Contains("revision") ) {

                //revision 문자열 부터 끝까지 자르기
                int startIndex = data.IndexOf("revision");
                string revision = data.Substring(startIndex);

                // revision 11  revisio 다음 공백후 숫자 얻기
                string[] parts = revision.Split(' ');
                int revisionNumber = int.Parse(parts[1]);

                txt_firmwareVersion.text = "Firmware Version: " + revisionNumber.ToString();

                // txt_Status.text += parts[1] + "\n";

                // txt_Status.text += line + "\n";
            }
        }
    }

    IEnumerator ReadSerialUntilIdle()
    {
        while (true)
        {
            if (serialPort.IsOpen)
            {
                try
                {
                    string data = serialPort.ReadLine(); // 한 줄씩 읽어옵니다.
                    txt_Status.text += ("\n" + data); // 수신된 데이터를 누적합니다.
                }
                catch (TimeoutException)
                {
                    Debug.Log("Read Timeout: No data received.");
                    yield break; // 타임아웃 발생 시 루프 종료
                }
                catch (IOException ex)
                {
                    Debug.LogError("IO Exception: " + ex.Message);
                    yield break; // IO 예외 발생 시 루프 종료
                }
            }

            yield return null; // 다음 프레임까지 기다립니다.
        }

    }

    void ConnectToSerialPort(string portName)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close(); // 포트를 닫아줍니다.
        }

        try {

            serialPort = new SerialPort(portName, 115200); // 시리얼 포트 객체 생성
            serialPort.Open(); // 포트 열기
            serialPort.ReadTimeout = 500; // 1초 동안 데이터를 읽지 못하면 타임아웃
            // 성공여부 판단
            if (serialPort.IsOpen)
            {
                txt_Status.text = "Connected to " + portName;

                btnConnect.GetComponentInChildren<TMP_Text>().text = "Disconnect";
                StartCoroutine(ReadSerialUntilIdle());
            }
            else
            {
                txt_Status.text = "Failed to connect to " + portName;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            txt_Status.text = "Failed to connect to " + portName;
        }
    }

    void SendCommandToSerialPort(string command)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.WriteLine(command); // 명령어 전송
        }
    }

    void ReadFromSerialPort()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            string message = serialPort.ReadLine(); // 데이터 읽기
            Debug.Log(message); // 읽은 데이터 로그 출력
        }
    }

    IEnumerator ReadSerialUntilOk()
    {
        string receivedData = ""; // 수신된 데이터를 저장할 변수를 초기화합니다.

        while (true)
        {
            if (serialPort.IsOpen)
            {
                try
                {
                    string data = serialPort.ReadLine(); // 한 줄씩 읽어옵니다.
                    receivedData += data + "\n"; // 수신된 데이터를 누적합니다.

                    if (data.Contains("OK"))
                    {
                        // Debug.Log("Received OK, stopping read.");
                        // Debug.Log("All Received Data: " + receivedData); // "OK"를 받으면 지금까지 누적된 데이터를 출력합니다.
                        OnReceivedOk?.Invoke(receivedData); // "OK" 콜백 호출
                        yield break; // 루프를 종료합니다.
                    }
                }
                catch (TimeoutException)
                {
                    Debug.Log("Read Timeout: No data received.");
                    OnTimeout?.Invoke(receivedData); // 타임아웃 콜백 호출
                    yield break; // 타임아웃 발생 시 루프 종료
                }
                catch (IOException ex)
                {
                    Debug.LogError("IO Exception: " + ex.Message);

                    OnError?.Invoke(receivedData); // IO 예외 발생 시 콜백 호출

                    yield break; // IO 예외 발생 시 루프 종료
                }
            }

            yield return null; // 다음 프레임까지 기다립니다.
        }
    }


    void clearInputFields()
    {
        input_ssid.text = "";
        input_password.text = "";
        input_targetIp.text = "";
        input_targetPort.text = "";
        input_deviceNumber.text = "";
        input_triggerDelay.text = "";
        toggle_IsUseImu.isOn = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        clearInputFields();
        
        btn_Scan.onClick.AddListener(async () =>
        {
            Debug.Log("Scan Button Clicked");

            btn_Scan.interactable = false;

            string[] ports = SerialPort.GetPortNames();

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

            //기존 내용 지우기
            dropdown_PortList.ClearOptions();

            
            foreach (string port in ports)
            {
                txt_Status.text = "Scanning... " + port + "\n";

                await Task.Run( () =>
                {
                    if(serialPortManager.checkPort_moai_DMP(port,115200))
                    {
                        filteredPorts.Add(new TMP_Dropdown.OptionData(port));
                        
                        // await Task.Delay(1000);
                    }
                });
            }
            dropdown_PortList.AddOptions(filteredPorts); // 필터링된 포트 리스트를 Dropdown에 추가

            btn_Scan.interactable = true;

            Debug.Log("Scan Button Finished");

        });

        btnConnect.onClick.AddListener(() =>
        {

            clearInputFields();
            

            if (serialPortManager.IsOpen())
            {
                serialPortManager.CloseSerialPort();
                btnConnect.GetComponentInChildren<TMP_Text>().text = "Connect";

                return;
            }

            if (dropdown_PortList.options.Count == 0)
            {
                // txt_Log.text += "No port selected.\n";
                return;
            }

            string portName = dropdown_PortList.options[dropdown_PortList.value].text;

            txt_Status.text += "Connecting to " + portName + "\n";

            serialPortManager.OpenSerialPort(portName, 115200);

            if (!serialPortManager.IsOpen())
            {
                txt_Status.text += "Failed to connect to " + portName + "\n";
                return;
            }

            txt_Status.text += "Connected to " + portName + "\n";

            serialPortManager.OnReceivedData = (string data) =>
            {
                // \r 을 제거하고 출력
                data = data.Replace("\r", "");

                txt_Status.text += data + "\n";
            };

            serialPortManager.OnError = (string error) =>
            {
                txt_Status.text += "Error: " + error + "\n";
                
            };

            serialPortManager.OnTimeout = (string timeout) =>
            {
                txt_Status.text += "Timeout: " + timeout + "\n";
                
            };

            //'disconnect text' button
            btnConnect.GetComponentInChildren<TMP_Text>().text = "Disconnect";


            // Debug.Log("Connect Button Clicked");
            // Debug.Log(dropdown_PortList.options[dropdown_PortList.value].text);

            // if (serialPort != null && serialPort.IsOpen)
            // {
            //     serialPort.Close();
            //     txt_Status.text = "Disconnected";
            //     btn_Connect.GetComponentInChildren<TMP_Text>().text = "Connect";
            //     return;
            // }
            // else
            // {
            //     ConnectToSerialPort(dropdown_PortList.options[dropdown_PortList.value].text);
            // }
        });

        btn_Read.onClick.AddListener( () =>
        {
            Debug.Log("Read Button Clicked");

// mStrAp: 
// mStrPassword: 
// mTargetIp: 
// mTargetPort: 0
// mDeviceNumber: 0
// mTriggerDelay: 150
// mOffsets: 
// offset0: 0
// offset1: 0
// offset2: 0
// offset3: 0
// offset4: 0
// offset5: 0
// OK

            txt_Status.text = "";

            serialPortManager.OnReceivedData = (string data) =>
            {
                ParseData(data);

                if(data.Contains("OK"))
                {
                    txt_Status.text = "read done\n";
                    serialPortManager.OnReceivedData = null; // 콜백 해제
                }
            };

            serialPortManager.WriteSerialPort("print\r\n");


        });

        btn_Write.onClick.AddListener(async () =>
        {
            Debug.Log("Write Button Clicked");

            try {

                btn_Write.interactable = false;

                txt_Status.text = "";
                serialPortManager.OnReceivedData = (string data) =>
                {
                    txt_Status.text += data + "\n";
                };
                
                // "write" 전송
                await Task.Delay(100);
                serialPortManager.WriteSerialPort("wifi connect " + input_ssid.text + " " + input_password.text);

                await Task.Delay(100);
                serialPortManager.WriteSerialPort("config target " + input_targetIp.text + " " + input_targetPort.text);

                await Task.Delay(100);
                serialPortManager.WriteSerialPort("config devid " + input_deviceNumber.text);

                await Task.Delay(100);
                serialPortManager.WriteSerialPort("config triggerdelay " + input_triggerDelay.text);

                await Task.Delay(100);
                serialPortManager.WriteSerialPort("imu " + (toggle_IsUseImu.isOn ? "use" : "notuse"));

                serialPortManager.OnReceivedData = (string data) =>
                {
                    if(data.Contains("OK"))
                    {
                        txt_Status.text = "write done\n";
                        serialPortManager.OnReceivedData = null; // 콜백 해제
                    }
                };
                serialPortManager.OnTimeout = (string timeout) =>
                {
                    txt_Status.text = "write timeout\n";
                    serialPortManager.OnTimeout = null; // 콜백 해제
                };
                serialPortManager.OnError = (string error) =>
                {
                    txt_Status.text = "write error\n";
                    serialPortManager.OnError = null; // 콜백 해제
                };

                await Task.Delay(100);
                serialPortManager.WriteSerialPort("save");



                // SendCommandToSerialPort("wifi connect " + input_ssid.text + " " + input_password.text);
                // SendCommandToSerialPort("config target " + input_targetIp.text + " " + input_targetPort.text);
                // SendCommandToSerialPort("config devid " + input_deviceNumber.text);

                // SendCommandToSerialPort("save");

            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);

            }

            btn_Write.interactable = true;

            
        });

        btn_ChckVersion.onClick.AddListener(() =>
        {
            Debug.Log("Check Version Button Clicked");

            // txt_Status.text = "";

            serialPortManager.OnReceivedData = (string data) =>
            {
                Debug.Log(data);
                ParseData(data);

                if(data.Contains("OK"))
                {
                    txt_Status.text = "version check done\n";
                    serialPortManager.OnReceivedData = null; // 콜백 해제
                }
            };

            serialPortManager.DiscardInBuffer(); // 버퍼를 비웁니다.

            serialPortManager.WriteSerialPort("help\r\n");
        });

        btn_Reboot.onClick.AddListener(() =>
        {
            Debug.Log("Reboot Button Clicked");

            serialPortManager.WriteSerialPort("reboot\r\n");

        });        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close(); // 포트를 닫아줍니다.
        }
    }


}
