﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pop_Up_Pillar : MonoBehaviour, ICollidable {
    GameObject warningSign;
    Vector3 spawnPosition;
    GameObject player;
    Vector3 offset;
    bool spawn = false;
    Vector3 finalRaiseAmount;
    Vector3 raiseAmount = Vector3.zero;
    Vector3 originalPosition;
    Player playerScript;
    Obstacle_Spawner obstacleSpawner;

    // Use this for initialization
    void Start () {
        //set scale
        playerScript = Info.getPlayer().GetComponent<Player>();
        float scale = Random.Range(1.0f, 3.0f);
        transform.localScale = new Vector3(scale, 1, 1);

        player = GameObject.FindGameObjectWithTag("Player");
        obstacleSpawner = Info.getPlayer().GetComponent<Obstacle_Spawner>();

        //offset = new Vector3(Random.Range(-1000, 1000), 0, Random.Range(-2000, -20000));


        if (playerScript.desertMode) {
            offset = new Vector3(Random.Range(-15000, 15000), 0, Random.Range(-15000, -70000) * obstacleSpawner.difficutlyModifier);
        } else {
            offset = new Vector3(Random.Range(-1600, 1600), 0, Random.Range(-10000, -20000) * obstacleSpawner.difficutlyModifier);
        }

        if (playerScript.desertMode) {
            spawnPosition = player.transform.position + offset;
        } else {
            spawnPosition = new Vector3(-2228, -60, player.transform.position.z) + offset;
        }
        

        warningSign = Instantiate(Resources.Load("warning_sign"), Vector3.zero, Quaternion.identity) as GameObject;

        finalRaiseAmount = Vector3.up * Random.Range(100, 350);

        Invoke("RaisePillar", Random.Range(0.2f, 0.6f));
        originalPosition = spawnPosition;
    }
	
	// Update is called once per frame
	void Update () {
        if (playerScript.desertMode) {
            spawnPosition = player.transform.position + offset;
        } else {
            spawnPosition = new Vector3(-2228, -60, player.transform.position.z) + offset;
        }

        warningSign.transform.position = spawnPosition;

        if (spawn) {
            transform.position = originalPosition + raiseAmount;
            raiseAmount = Vector3.Lerp(raiseAmount, finalRaiseAmount, Time.deltaTime / 0.1f);
        } else {
            originalPosition = spawnPosition + Vector3.down * 750;
        }

    }

    public static void Spawn() {
        GameObject pillar = Instantiate(Resources.Load("pop_up_pillar"), Vector3.zero, Quaternion.identity) as GameObject;
    }

    void RaisePillar() {
        transform.position = spawnPosition + Vector3.down * 750;
        spawn = true;
        warningSign.SetActive(false);
    }

    public void Collide() {
        Destroy(gameObject);
    }
}
