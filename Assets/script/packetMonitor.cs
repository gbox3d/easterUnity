using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using System.Threading;
using System;

// TextMeshPro 네임스페이스 추가
using TMPro;

using UnityEngine.UI;
using Unity.VisualScripting;

public class packetMonitor : MonoBehaviour
{
    private UDPReceiver udpReceiver;
    private CancellationTokenSource cts;
    private float lastUpdateTime;

    [SerializeField] private TextMeshProUGUI textNetWorkFps;
    [SerializeField] private TextMeshProUGUI textQuaternionValues;

    [SerializeField] private TextMeshProUGUI textAccelValues;
    [SerializeField] private TextMeshProUGUI textGyroValues;
    [SerializeField] private TextMeshProUGUI textMagValues;
    [SerializeField] private TextMeshProUGUI textBattery;
    [SerializeField] private TextMeshProUGUI textFireCount;
    [SerializeField] private TextMeshProUGUI textDevId;
    [SerializeField] private TextMeshProUGUI textYawPitchRoll;

    [SerializeField] int localPort = 9250;

    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private TextMeshProUGUI receivedText;

    UdpClient udpClient;




    // Start is called before the first frame update
    void Start()
    {
        udpReceiver = new UDPReceiver(localPort); // Use the same port as in your Arduino code
        cts = new CancellationTokenSource();
        ReceivePacketsAsync(cts.Token);

        sendButton.onClick.AddListener(() =>
        {
            string message = inputField.text;
            udpReceiver.sendCmdPAcket(message);
            
        });
    }

    async void ReceivePacketsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Calculate the time delta
                float deltaTime = Time.time - lastUpdateTime;
                lastUpdateTime = Time.time;

                // Receive a packet
                S_RES_Packet _res_packet = await udpReceiver.ReceivePacketAsync();

                if (_res_packet.nResType == 0)
                {
                    S_Udp_IMU_RawData_Packet packet = _res_packet.imu_packet;
                    
                    switch (packet.header.MagicNumber)
                    {

                        case 20230903: //moai imu packet
                            // Debug.Log("Magic Number is 20230903");

                            textNetWorkFps.text = (1.0f / deltaTime).ToString("F2");

                            //소숫점 3자리까지만 표시
                            textQuaternionValues.text = string.Format("qW: {0:F3}, qX: {1:F3}, qY: {2:F3}, qZ: {3:F3}", packet.qW, packet.qX, packet.qY, packet.qZ);
                            textAccelValues.text = string.Format("AccelX: {0:F3}, AccelY: {1:F3}, AccelZ: {2:F3}", packet.aX, packet.aY, packet.aZ);
                            textGyroValues.text = string.Format("GyroX: {0:F3}, GyroY: {1:F3}, GyroZ: {2:F3}", packet.gX, packet.gY, packet.gZ);
                            textMagValues.text = string.Format("MagX: {0:F3}, MagY: {1:F3}, MagZ: {2:F3}", packet.mX, packet.mY, packet.mZ);
                            textBattery.text = string.Format("{0:F3}", packet.battery);
                            textFireCount.text = string.Format("{0}", packet.fire_count);
                            textDevId.text = string.Format("{0}", packet.dev_id);
                            textYawPitchRoll.text = string.Format("Yaw: {0:F3}, Pitch: {1:F3}, Roll: {2:F3}", packet.yaw, packet.pitch, packet.roll);

                            break;
                        default:
                            // Debug.Log("Magic Number is not 20230903");
                            Debug.Log("unknown Magic Number :" + packet.header.MagicNumber);
                            break;
                    }
                }
                else if (_res_packet.nResType == 1)
                {
                    receivedText.text = _res_packet.strSimpleString_packet;
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                break;
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Debug.LogError(ex);
            }
        }

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDestroy()
    {
        cts.Cancel();
    }

}
