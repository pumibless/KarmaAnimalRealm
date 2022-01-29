using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Inventory")]
    public List<string> pickables = new List<string>();
    [Header("Pickable Checker")]
    public bool mushroomClicked;

    [Header("Day-Night Cycle")]
    public int dayNum;

    [Header("PickupSounds")]
    public AudioClip[] audioClips;
    AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        pickableSoundUpdate();
    }

    public void pickableSoundUpdate() 
    {
        if (mushroomClicked) 
        {
            audioSource.PlayOneShot(audioClips[0]);
            mushroomClicked = false;
        }

    }

}
