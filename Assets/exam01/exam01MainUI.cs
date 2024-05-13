using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// using System.IO.Ports;
using TMPro;
using System;


public class exam01MainUI : MonoBehaviour
{
    // SerialPort serialPort;
    SerialPortManager serialPortManager;

    [SerializeField] Button btnScan;
    [SerializeField] Button btnConnect;
    [SerializeField] Button btnWrite;
    [SerializeField] TMP_Dropdown dropdown_PortList;
    [SerializeField] TMP_Text txt_Log;
    [SerializeField] TMP_Text txt_Info;
    [SerializeField] TMP_InputField inputField_Command;

    
    public Action<string> OnReceivedData;


    public void ParseAndFormat(string data)
    {

        // data = data.Trim();
        // 데이터를 공백으로 분리
        string[] parts = data.Trim().Split(' ');

        if (parts.Length != 7)
        {
            // Console.WriteLine("입력 데이터가 형식에 맞지 않습니다.");
            Debug.Log("입력 데이터가 형식에 맞지 않습니다. " + parts.Length + "개의 데이터가 들어왔습니다.");
            return;
        }

        try
        {
            // 각 부분을 적절한 타입으로 변환
            int fire_count = int.Parse(parts[0]);
            int mode_switch = int.Parse(parts[1]);
            float battery = float.Parse(parts[2]);
            float quat0 = float.Parse(parts[3]);
            float quat1 = float.Parse(parts[4]);
            float quat2 = float.Parse(parts[5]);
            float quat3 = float.Parse(parts[6]);

            txt_Info.text = $"Fire Count: {fire_count}\nMode Switch: {mode_switch}\nBattery: {battery:F6}"; 



            // 형식에 맞게 출력
            // Console.WriteLine($"\n$ {fire_count} {mode_switch} {battery:F6} {quat0:F6} {quat1:F6} {quat2:F6} {quat3:F6}\n");
        }
        catch (FormatException)
        {
            Debug.Log("데이터 파싱 중 오류가 발생했습니다. 데이터 형식을 확인해주세요.");
            // Console.WriteLine("데이터 파싱 중 오류가 발생했습니다. 데이터 형식을 확인해주세요.");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        dropdown_PortList.ClearOptions(); // Dropdown의 옵션 초기화

        // port scan 버튼 클릭 이벤트
        btnScan.onClick.AddListener(() =>
        {
            Debug.Log("Scan Button Clicked");

            List<TMP_Dropdown.OptionData> filteredPorts = new List<TMP_Dropdown.OptionData>(); // OptionData 리스트 생성

            //기존 내용 지우기
            dropdown_PortList.ClearOptions();

            string[] ports = SerialPortManager.GetPortNames(); // 사용 가능한 포트 리스트 가져오기

            // string[] ports = Directory.GetFiles("/dev/", "tty.*");

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
                    filteredPorts.Add(new TMP_Dropdown.OptionData(port));
                }
            }
            dropdown_PortList.AddOptions(filteredPorts); // 필터링된 포트 리스트를 Dropdown에 추가


        });

        // port 접속 버튼 클릭 이벤트
        btnConnect.onClick.AddListener(() =>
        {
            if (serialPortManager != null) 
            {
                serialPortManager.CloseSerialPort();
                btnConnect.GetComponentInChildren<TMP_Text>().text = "Connect";
            }
            else
            {
                string portName = dropdown_PortList.options[dropdown_PortList.value].text;

                Debug.Log("select port : " + portName);

                OnReceivedData = (string data) =>
                {
                    //#으로 시작하는 데이터만 로그에 출력
                    if (data.StartsWith("#") || data.StartsWith("%") )
                    {
                        Debug.Log("수신된 데이터: " + data);
                        //data 에서 맨 처음에 오는 '#' 문자 제거
                        txt_Log.text += "> " + data[1..] + "\n";
                    }
                    else if(data.StartsWith("$"))
                    {
                        // Debug.Log(data[1..]);

                        ParseAndFormat(data[1..]);
                        
                    }
                    
                };

                serialPortManager = new SerialPortManager(portName, 115200, OnReceivedData);
                btnConnect.GetComponentInChildren<TMP_Text>().text = "Disconnect";
            }
        });

        btnWrite.onClick.AddListener(() =>
        {
            try {
                string command = inputField_Command.text;
                serialPortManager?.WriteSerialPort(command);
            }
            catch (NullReferenceException)
            {
                Debug.Log("시리얼 포트가 연결되어 있지 않습니다.");
            }
            
        });


    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDestroy()
    {
        serialPortManager?.CloseSerialPort();
    }
}
