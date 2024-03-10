﻿using System;
using Photon.Pun.UtilityScripts;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FightUI : MonoBehaviour
{
    // public Image StaminaBar; // 确保在Unity编辑器中已经设置了这个引用
    public static FightUI Instance { get; private set; }
    //private GameManager gameManager;
    private Text countdownText;
    private Transform tutorialPanel;
    private Image StaminaBar;

    private float previousTime;
    private bool iscount;
    //public static float countdownTimer = 180f;
    private void Start()
    {
        iscount = true;
        countdownText = transform.Find("CountdownText").GetComponent<Text>();
        tutorialPanel = transform.Find("TutorialPanel");
        Transform hpTransform = transform.Find("hp");
        if (hpTransform != null && hpTransform.childCount > 0) {
            // 假设hp下只有一个子对象，直接获取第一个子对象
            Transform firstChild = hpTransform.GetChild(0);
            Image image = firstChild.GetComponent<Image>();
            if (image != null) {
                // 成功找到了Image组件
                StaminaBar = image;
            }
        }

        StartCoroutine(BeginStartSequence());
        //--------------------------
        // top left placeholder components
        // transform.Find("hp/fill").GetComponent<Image>().fillAmount =
        // transform.Find("hp/Text").GetComponent<Text>().text =
        //--------------------------

    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    
    public void UpdateStaminaBar(float fillAmount)
    {
        if (StaminaBar != null)
        {
            StaminaBar.fillAmount = fillAmount;
        }
    }
    IEnumerator BeginStartSequence()
    {
        yield return new WaitForSeconds(2); // 首先等待2秒
        StartCoroutine(ShowTutorialPanel()); // 然后显示教程面板
    }
    
    IEnumerator ShowTutorialPanel()
    {
        // tutorialPanel.gameObject.SetActive(true); // 显示教程面板
        // yield return new WaitForSeconds(10); // 等待5秒
        // tutorialPanel.gameObject.SetActive(false); // 隐藏教程面板
        // 遍历tutorialPanel下的所有子对象
        for (int i = 0; i < tutorialPanel.childCount; i++)
        {
            // 激活当前子对象
            Transform currentChild = tutorialPanel.GetChild(i);
            currentChild.gameObject.SetActive(true);

            // 等待5秒
            yield return new WaitForSeconds(5);

            // 禁用当前子对象
            currentChild.gameObject.SetActive(false);
        }
    }

    //public AudioClip countSound;
    //public AudioClip timesupSound;
    //// Update is called once per frame
    void Update()
    {

    }

    public void SetCountdownTimer(float countdownTimer) 
    {
        // 获取 GameManager 中的倒计时时间
        //float countdownTime = gameManager.GetCountdownTime();

        // 将倒计时时间格式化为分钟:秒钟的形式
        string formattedTime = string.Format("{0:0}:{1:00}", Mathf.Floor(countdownTimer / 60), Mathf.Floor(countdownTimer % 60));



        // 更新 TextMeshProUGUI 文本内容
        if (countdownText != null && iscount)
        {
            // 判断是否小于等于10秒，如果是，将颜色设置为红色
            if (Mathf.Floor(countdownTimer) <= 10 && Mathf.Floor(countdownTimer) > 0f)
            {
                countdownText.color = Color.red;
                // 在10秒之后的每一秒播放倒计时音效
                //if(Mathf.Floor(countdownTime) != previousTime)
                //{
                //    this.GetComponent<AudioSource>().PlayOneShot(countSound);
                //    //Debug.Log("countdownTime:" + countdownTime);

                //}

                //this.GetComponent<AudioSource>().PlayOneShot(countSound);
            }
            else if (Mathf.Floor(countdownTimer) == 0f)
            {
                //this.GetComponent<AudioSource>().PlayOneShot(timesupSound);
                iscount = false;
            }
            else
            {
                // 如果不是，将颜色还原为之前的颜色
                countdownText.color = Color.black;
            }
            countdownText.text = "Time: " + formattedTime;
            // 更新上一次的整数部分时间
            previousTime = Mathf.Floor(countdownTimer);
        }
    }
}
