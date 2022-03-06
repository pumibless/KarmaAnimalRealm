using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public enum StatName {
    Health, Hunger, Stamina, Thirst
}

public class StatsManager : MonoBehaviour
{
    // ------
    // CONSTS
    // ------
    const int NUM_STATS = 4;

    float BAR_WIDTH; // used as a const

    // ----------------
    // PUBLIC VARIABLES
    // ----------------
    public Color[] barColors = new Color[NUM_STATS];
    public GameObject barPrefab;

    // the rate of loss is X_LOSS_AMOUNT/X_LOSS_INTERVAL units per second
    public float hungerLossAmount = -1.0f;
    public float hungerLossInterval = 3.0f;
    public float thirstLossAmount = -1.0f;
    public float thirstLossInterval = 1.0f;

    // unit per 1/50th of a second
    public float staminaGainRate = 0.1f;
    public float staminaLossRate = -0.2f;

    // -----------------
    // PRIVATE VARIABLES
    // -----------------
    GameObject overlayMenu;
    
    // arrays, not lists, are used bc i think arrays are faster, and no need to add or remove
    float[] stats = new float[NUM_STATS];
    RectTransform[] barRt = new RectTransform[NUM_STATS];

    float hungerTimer;
    float thirstTimer;


    void Start() {
        overlayMenu = GameObject.Find("Canvas/Overlay Menu");
        
        for (int i = 0; i < stats.Length; i++)
            stats[i] = 100.0f;

        for (int i = 0; i < barRt.Length; i++) {
            GameObject bar = Instantiate(barPrefab, overlayMenu.transform);
            bar.GetComponent<Image>().color = barColors[i];
            barRt[i] = bar.GetComponent<RectTransform>();
            barRt[i].localPosition = new Vector2(barRt[i].localPosition[0], -i*100+100); // TODO: y-component is placeholder, arbitrary math
        }

        hungerTimer = Time.time;
        thirstTimer = Time.time;

        BAR_WIDTH = barRt[0].rect.width;
    }

    void Update() {
        if (Input.GetKey("tab")) overlayMenu.SetActive(true);
        else                     overlayMenu.SetActive(false);
    }

    void FixedUpdate() {
        if (Input.GetAxisRaw("Vertical") != 0 ||  Input.GetAxisRaw("Horizontal") != 0)
            ChangeStat(StatName.Stamina, staminaLossRate);
        else
            ChangeStat(StatName.Stamina, staminaGainRate);
        if (Time.time - hungerTimer > hungerLossInterval) {
            ChangeStat(StatName.Hunger, hungerLossAmount);
            hungerTimer = Time.time;
        }
        if (Time.time - thirstTimer > thirstLossInterval) {
            ChangeStat(StatName.Thirst, thirstLossAmount);
            thirstTimer = Time.time;
        }
    }

    protected void ChangeStat(StatName stat, float num) {
        int s = (int)stat;

        if (num > 0) {
            if (stats[s] < 100) stats[s] += num;
            else stats[s] = 100;
        }
        else {
            if (stats[s] > 0) stats[s] += num;
            else stats[s] = 0;
        }
        
        barRt[s].sizeDelta = new Vector2(BAR_WIDTH * stats[s] / 100.0f, 100);

        // the overlay menu width is 720 => half that is 360. changing width affects both ends, so have to move pos with it.
        barRt[s].localPosition = new Vector2(barRt[s].rect.width/2-360, barRt[s].localPosition[1]);
    }
}