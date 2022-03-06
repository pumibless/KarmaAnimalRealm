using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public enum StatName {
    Health, Hunger, Stamina, Thirst
}

public class StatsManager : MonoBehaviour
{
    // -- CONSTS --
    const int NUM_STATS = 4;

    // unit per 1/50th of a second
    const float STAMINA_GAIN_RATE = 0.25f;
    const float STAMINA_LOSS_RATE = -STAMINA_GAIN_RATE * 2;
    float BAR_WIDTH; // used as a const

    // -- PUBLIC VARIABLES --
    public Color[] barColors = new Color[NUM_STATS];
    public GameObject barPrefab;

    // -- PRIVATE VARIABLES --
    GameObject overlayMenu;
    
    // arrays, not lists, are used bc i think arrays are faster, and no need to add or remove
    float[] stats = new float[NUM_STATS];
    RectTransform[] barRt = new RectTransform[NUM_STATS];

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

        BAR_WIDTH = barRt[0].rect.width;
    }

    void Update() {
        if (Input.GetKey("tab")) overlayMenu.SetActive(true);
        else                     overlayMenu.SetActive(false);
    }

    void FixedUpdate() {
        if (Input.GetAxisRaw("Vertical") != 0 ||  Input.GetAxisRaw("Horizontal") != 0)
            ChangeStat(StatName.Stamina, STAMINA_LOSS_RATE);
        else
            ChangeStat(StatName.Stamina, STAMINA_GAIN_RATE);
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