using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.IO.Ports;
using TMPro;
using System;
using System.IO;


public class configUI_main : MonoBehaviour
{
    //button 
    [SerializeField] Button btn_Scan;
    [SerializeField] Button btn_Connect;
    [SerializeField] Button btn_Read;
    [SerializeField] Button btn_Write;
    [SerializeField] Button btn_Reboot;
    [SerializeField] Button btn_ChckVersion;


    [SerializeField] TMP_Dropdown dropdown_PortList;
    [SerializeField] TMP_Text txt_Status;

    [SerializeField] TMP_InputField input_ssid;
    [SerializeField] TMP_InputField input_password;
    [SerializeField] TMP_InputField input_targetIp;
    [SerializeField] TMP_InputField input_targetPort;
    [SerializeField] TMP_InputField input_deviceNumber;

    SerialPort serialPort;

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
                    default:
                        
                        break;
                }
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

                btn_Connect.GetComponentInChildren<TMP_Text>().text = "Disconnect";
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



    // Start is called before the first frame update
    void Start()
    {
        btn_Scan.onClick.AddListener(() =>
        {
            Debug.Log("Scan Button Clicked");

            List<TMP_Dropdown.OptionData> filteredPorts = new List<TMP_Dropdown.OptionData>(); // OptionData 리스트 생성

            //기존 내용 지우기
            dropdown_PortList.ClearOptions();

            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                if (port.Contains("tty."))
                {
                    // "tty."를 포함하는 포트만 Dropdown 옵션으로 추가
                    filteredPorts.Add(new TMP_Dropdown.OptionData(port));
                }
            }
            dropdown_PortList.AddOptions(filteredPorts); // 필터링된 포트 리스트를 Dropdown에 추가

        });

        btn_Connect.onClick.AddListener(() =>
        {
            Debug.Log("Connect Button Clicked");
            Debug.Log(dropdown_PortList.options[dropdown_PortList.value].text);

            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                txt_Status.text = "Disconnected";
                btn_Connect.GetComponentInChildren<TMP_Text>().text = "Connect";
                return;
            }
            else
            {
                ConnectToSerialPort(dropdown_PortList.options[dropdown_PortList.value].text);
            }
        });

        btn_Read.onClick.AddListener(() =>
        {
            Debug.Log("Read Button Clicked");

            serialPort.DiscardInBuffer(); // 버퍼를 비웁니다.

            // 콜백 함수 예시
            OnReceivedOk = (receivedData) =>
            {
                Debug.Log("Received 'OK'. Data until now: " + receivedData);
                // 여기에 'OK'를 받았을 때의 추가 처리 작업을 구현합니다.

                /*
mStrAp: 
mStrPassword: 
mTargetIp: 
mTargetPort: 0
mDeviceNumber: 0
mTriggerDelay: 150
mOffsets: 
offset0: 0
offset1: 0
offset2: 0
offset3: 0
offset4: 0
offset5: 0
OK
                */

                ParseData(receivedData);
            };

            OnTimeout = (receivedData) =>
            {
                Debug.Log("Timeout. Data until now: " + receivedData);
                // 여기에 타임아웃 시의 추가 처리 작업을 구현합니다.

            };

            OnError = (receivedData) =>
            {
                Debug.Log("Error. Data until now: " + receivedData);
                // 여기에 에러 발생 시의 추가 처리 작업을 구현합니다.
            };


            // "print"  전송
            SendCommandToSerialPort("print");
            // ReadFromSerialPort();

            // 수신해서 출력
            StartCoroutine(ReadSerialUntilOk());

        });

        btn_Write.onClick.AddListener(() =>
        {
            Debug.Log("Write Button Clicked");

            try {

                // "write" 전송
                SendCommandToSerialPort("wifi connect " + input_ssid.text + " " + input_password.text);
                SendCommandToSerialPort("config target " + input_targetIp.text + " " + input_targetPort.text);
                SendCommandToSerialPort("config devid " + input_deviceNumber.text);

                SendCommandToSerialPort("save");

            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);

            }

            
        });

        btn_ChckVersion.onClick.AddListener(() =>
        {
            Debug.Log("Check Version Button Clicked");

            // "version" 전송
            SendCommandToSerialPort("help");

            OnReceivedOk = (receivedData) =>
            {
                Debug.Log("Received 'OK'. Data until now: " + receivedData);
                // 여기에 'OK'를 받았을 때의 추가 처리 작업을 구현합니다.
                //receivedData 의 맨 처음 라인을 버전 정보로 출력
                // string[] lines = receivedData.Split('\n'); // 데이터를 줄 단위로 나눕니다.

                txt_Status.text = receivedData;
                
                // StartCoroutine(ReadSerialUntilIdle());
            };

            // 수신해서 출력
            StartCoroutine(ReadSerialUntilOk());
        });

        btn_Reboot.onClick.AddListener(() =>
        {
            Debug.Log("Reboot Button Clicked");
            // "reboot" 전송
            SendCommandToSerialPort("reboot");

            OnReceivedOk = (receivedData) =>
            {
                Debug.Log("Received 'OK'. Data until now: " + receivedData);
                txt_Status.text = receivedData;
            };

            // 수신해서 출력
            StartCoroutine(ReadSerialUntilOk());
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
