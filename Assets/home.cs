using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class home : MonoBehaviour
{
    [SerializeField] Button btnGotoTare6050;
    [SerializeField] Button btnGotoTareBNO8X;
    [SerializeField] Button btnGotoConfig;
    [SerializeField] Button btnGotoPacketMonitor;
    // Start is called before the first frame update
    void Start()
    {
        btnGotoTare6050.onClick.AddListener(() => {
            UnityEngine.SceneManagement.SceneManager.LoadScene("mp6050_tare");
        });

        btnGotoConfig.onClick.AddListener(() => {
            UnityEngine.SceneManagement.SceneManager.LoadScene("configUI");
        });

        btnGotoTareBNO8X.onClick.AddListener(() => {
            UnityEngine.SceneManagement.SceneManager.LoadScene("bno08x_tare");
        });

        btnGotoPacketMonitor.onClick.AddListener(() => {
            UnityEngine.SceneManagement.SceneManager.LoadScene("mp6050_packet_monitor");
        });

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
