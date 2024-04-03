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


