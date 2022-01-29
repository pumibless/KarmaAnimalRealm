using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MushroomSpawner : MonoBehaviour
{

    [Header("Mushroom Spawner")]
    public GameObject[] prefabToSpawn;
    public Transform spawn;
    public int objectCount;
    public float spawnRadius = 5;
    public float spawnCollisionCheckRadius;

    // Start is called before the first frame update
    void Start()
    {
        SpawnFolliage(); 

    }

    // Update is called once per frame
    void Update()
    {

    }

    void SpawnFolliage()
    {
        //every n days, spawn mushrooms in an area
        //better way: using Physics.raycast to detect collision and layermask of ground https://answers.unity.com/questions/1812540/how-i-could-make-it.html
        for (int i = 0; i < objectCount; i++)
        {
            Vector3 spawnPoint = new Vector3(Random.insideUnitSphere.x, 0, Random.insideUnitSphere.z) * spawnRadius + new Vector3(spawn.transform.position.x, 0, spawn.transform.position.z);

            //check for collision of other mushrooms [not working ;_;]
            if (Physics.CheckSphere(spawnPoint, spawnCollisionCheckRadius))
            {
                Instantiate(prefabToSpawn[Random.Range(0, prefabToSpawn.Length)], spawnPoint, Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)));

            }

        }

    }
}
