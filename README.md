# imu 유니티 예제

## 예제 설명

**태져이건용**   
mp6050_tare : 기준점 재설정 예제  
mp6050_packet_monitor : 패킷 모니터링 예제  


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
```
textFireCount.text = packet.fire_count.ToString();

//탄수가 변하면 발사로 간주한다.
if (mPrevFireCount != packet.fire_count)
{
    mPrevFireCount = packet.fire_count;
    Debug.Log("fire count : " + packet.fire_count);
}

textNetWorkFps.text = (1.0f / deltaTime).ToString("F2"); //네트워크 fps
```

위의 예제에서 처럼 tare를 위해서 mDeltaRotation 을 사용한다. 
즉 mDeltaRotation은 센서의 원점을 지정하는데 사용된다.   

