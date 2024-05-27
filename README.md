# imu 유니티 예제

## 예제 설명

**태져이건용**   
mp6050_tare : 기준점 재설정 예제  
mp6050_packet_monitor : 패킷 모니터링 예제  
configUIRecv : Receiver 설정 및 모니터링 예제


### Tare (기준점 재설정) 

**종류**  
1. imu 자체에서 직접 재설정.  
2. 게임엔진 등에서 재설정.  


**필요성**  
하드웨어 설계상 센서의 위치가 항상 바뀌기 때문에 센서의 원점을 지정해야하는 필요성이 있다.

**unity 에제**  
```c#
// y,z 축이 서로 바뀌어 있고 x,y축의 방향이 역으로 되어있다.
float qW = packet.qW;
float qX = -packet.qX;
float qY = -packet.qZ; // imu qY = unity qZ
float qZ = packet.qY; // imu qZ = unity qY

//쿼터니온을 만든다.
mImuRotation = new Quaternion(qW, qX, qY, qZ); //imu sensor rotation
imuObject_Ypr.transform.rotation = mDeltaRotation * mImuRotation; //Tare rotation

//발사 카운트
textFireCount.text = packet.fire_count.ToString();

//탄수가 변하면 발사로 간주한다.(발사 시점 잡기 위함)
if (mPrevFireCount != packet.fire_count)
{
    mPrevFireCount = packet.fire_count;
    Debug.Log("fire count : " + packet.fire_count);
}

textNetWorkFps.text = (1.0f / deltaTime).ToString("F2"); //네트워크 fps
```

위의 예제에서 처럼 tare를 위해서 mDeltaRotation 을 사용한다. 
즉 mDeltaRotation은 센서의 원점을 지정하는데 사용된다.   


```c#
btnReset.onClick.AddListener(() =>
{
    //틀어진 값을 보정하기 위해 현재 imu의 rotation과 target rotation의 차이를 구한다.
    Quaternion target = imuObject_Corrected.transform.rotation;
    // target - current
    mDeltaRotation = target * Quaternion.Inverse(mImuRotation); //target - mImuRotation
    mDeltaRotation.Normalize();
});
```
mDeltaRotation은 위와 같이 target - current 로 구한다.  
target은 목표값이고 current는 현재 imu의 rotation값이다. 그래서 mImuRotation이 current 이다.  
따라서 target 과 같은 회전상태를 만든후 tare를 하면 imu의 원점이 target과 같은 위치로 재설정된다.  



### receiver 설정 및 모니터링

receiver 씨리얼 통신(rs232) 패킷  

#### 명령어   
gun [enable | disable]  :  총 발사 여부 정하기  
activate [지속시간 ms]  :  리시버의 활성화 시간 설정  
about  :  리시버 정보 확인  
apinfo :  리시버 AP 정보 확인  
reboot :  리시버 재부팅  

#### 응답 패킷
씨리얼 명령어에 대한 응답은 % 이 앞에 붙는다.  
원격 기기측에서 받은 패킷에는 $ 이 앞에 붙는다.  
시스템 정보메씨지는 # 이 앞에 붙는다.   

```c++
Serial.printf("\n$ %d %d %d %f %f %f %f %f\n", fire_count , mode_switch,gun_status ,battery ,quat[0], quat[1], quat[2], quat[3]);
```
위의 코드처럼 $ 이 앞에붙는 패킷은 발사 카운트, 모드 스위치, 총 상태, 배터리, 쿼터니온 값을 보낸다.    
[fire_count, mode_switch, gun_status, battery, quat[0], quat[1], quat[2], quat[3]]   


#### unity uart 통신 예제   

##### port scan 
유니티용 씨리얼 포트 통신 예제를 참고 하려면 다음 파일을 참고하십시오.  
Assets/script/SerialPortManager.cs   
위 파일은 씨리얼 포트 통신을 위한 라이브러리 예제입니다.  

아래 코드는 SerialPortManager 의 일부분으로 씨리얼 포트를 하나씩 열어서 about 명령어를 주고 응답으로 "%moai receiver" 라는 문자열을 받으면 moai receiver를 찾은 것으로 판단합니다.  

```c#
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
```

#### packet parsing  


씨리얼 포트로 받은 데이터를 파싱하는 함수는 다음과 같이 구현되어 있습니다.  

```c#
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
```


