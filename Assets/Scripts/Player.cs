﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

//Player class
public class Player : MonoBehaviour {
    const float minimumSpeed = -1500;
    const float maxHorizontalSpeed = 5000;

    Rigidbody rb;

    //Put all this horizontal speed in its own class.
    float currentHorizontalSpeed = 0;
    //float maxHorizontalSpeed = 5000;
    float bonusHorizontalSpeed = 0;
    float boostHorizontalSpeed = 0;
	float accelData = 0;

    public Text hSpeed;
    public Text tSpeed;
    public Text tScore;
    public Text tPortalDistance;
    public Text tPoliceSpeed;

    DoubleTap aDoubleTap;
    DoubleTap bDoubleTap;
    Shield shield;
    BlurEffect blur;

	public GameObject sendRef;
	UDPSend udpSendRef;

    GameObject[] wheelSmoke;

    Speedometer speedometer;

    bool dropDown = false;

    public int score = 0;

    public bool desertMode = false;

    public GameObject desertPlane = null;

    PortalDesertSpawner portalDesertSpawner;

    float deathStart;
    bool readyToDie = false;
    float deathTresholdCity = 3700;
    float deathTresholdDesert = 6000;
    float deathModifier = 1;

    // Use this for initialization
    void Start () {
        shield = new Shield();
        blur = new BlurEffect();
        rb = GetComponent<Rigidbody>();
        speedometer = new Speedometer();

		udpSendRef = sendRef.GetComponent<UDPSend> ();

        aDoubleTap = new DoubleTap(KeyCode.A, 0.2f, DashLeft);
        bDoubleTap = new DoubleTap(KeyCode.D, 0.2f, DashRight);

        wheelSmoke = GameObject.FindGameObjectsWithTag("WheelSmoke");
        portalDesertSpawner = gameObject.GetComponent<PortalDesertSpawner>();
        Invoke("ReadyToDie", 5.0f);
    }
	
	// Update is called once per frame
    //Split update into smaller methods later.
	void Update () {
        if (Input.GetKey(KeyCode.Escape)) {
            SceneManager.LoadScene("MainMenu");
        }

        deathModifier += Time.deltaTime / 100;
        aDoubleTap.Update();
        bDoubleTap.Update();
        shield.Update();
        blur.Update();
        speedometer.Update();

        if (dropDown) {
            DropDown();
            return;
        }

        //death
        if (Mathf.Abs(rb.velocity.z) < deathTresholdCity * deathModifier && desertMode == false) {

        } else if (Mathf.Abs(rb.velocity.z) < deathTresholdDesert * deathModifier && desertMode == true)  {
            
        } else {
            deathStart = Time.time;
        }

        if (deathStart + 2 < Time.time && readyToDie) {
            GameObject.Find("FadeOut").GetComponent<FadeOut>().StartFadeOut();
        }

        //If behind portal
        if (desertMode && portalDesertSpawner.exitPortal != null) {
            if (transform.position.z < portalDesertSpawner.newEntracePortalPosition.z - 1000) {
                Destroy(gameObject);
            }
        }

        score += (int)(Mathf.Abs(rb.velocity.z) * Time.deltaTime);

        //Get data from UDPRecieve script for X value of acceleromter on app
        float.TryParse (UDPReceive.lastReceivedUDPPacket, out accelData);
		//Debug.Log(accelData);

        //Accelerate the car initally.
        if(rb.velocity.z > -4000) {
            rb.AddRelativeForce(Vector3.forward * -1000 * Time.deltaTime * 50);
        }

        //Accelerate car continuously;
        rb.AddRelativeForce(Vector3.forward * -100 * Time.deltaTime * 50);

		//Calculate bonus horizontal speed.
		bonusHorizontalSpeed = CalculateBonusHorizontalSpeed();

        //Lerp back the boost horizontal speed to 0.
        boostHorizontalSpeed = Mathf.Lerp(boostHorizontalSpeed, 0, Time.deltaTime / 0.1f);

        //Input keyboard code.
		if (accelData > 0.19) {
			currentHorizontalSpeed = Mathf.Lerp(currentHorizontalSpeed, (accelData*10000) * -1 + -bonusHorizontalSpeed + -boostHorizontalSpeed, Time.deltaTime / 0.2f);
        }

		if (accelData < - 0.19) {
			currentHorizontalSpeed = Mathf.Lerp(currentHorizontalSpeed, (accelData*10000) * -1 + bonusHorizontalSpeed + boostHorizontalSpeed, Time.deltaTime / 0.2f);
        }

		if (Input.GetKey(KeyCode.D)) {
			currentHorizontalSpeed = Mathf.Lerp(currentHorizontalSpeed, -maxHorizontalSpeed + -bonusHorizontalSpeed + -boostHorizontalSpeed, Time.deltaTime / 0.2f);
		}

		if (Input.GetKey(KeyCode.A)) {
			currentHorizontalSpeed = Mathf.Lerp(currentHorizontalSpeed, maxHorizontalSpeed + bonusHorizontalSpeed + boostHorizontalSpeed, Time.deltaTime / 0.2f);
		}

		//If none of the buttons are pressed, lerp to 0 on horizontal speed.
        if (!Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A)) {
            currentHorizontalSpeed = Mathf.Lerp(currentHorizontalSpeed, 0, Time.deltaTime / 0.1f);
        }


		//Check lane boundaries
        if (transform.position.x < -3800 && !desertMode) {
            transform.position = new Vector3(-3800, transform.position.y, transform.position.z);
            currentHorizontalSpeed = 0;
        }

        if (transform.position.x > -630 && !desertMode) {
            transform.position = new Vector3(-630, transform.position.y, transform.position.z);
            currentHorizontalSpeed = 0;
        }

        //tilt
        //Mb turn more the more ur speed is
        Vector3 rotation = transform.localRotation.eulerAngles;
        float maxYTilt = Helper.Remap(rb.velocity.z, minimumSpeed, -16000, 20, 40);
        maxYTilt = Mathf.Clamp(maxYTilt, -40, 40);
        rotation.y = Helper.Remap(currentHorizontalSpeed, (-maxHorizontalSpeed + -bonusHorizontalSpeed + -boostHorizontalSpeed) * -1, -maxHorizontalSpeed + -bonusHorizontalSpeed + -boostHorizontalSpeed, -maxYTilt, maxYTilt);
        rotation.z = Helper.Remap(currentHorizontalSpeed, (-maxHorizontalSpeed + -bonusHorizontalSpeed + -boostHorizontalSpeed) * -1, -maxHorizontalSpeed + -bonusHorizontalSpeed + -boostHorizontalSpeed, -7, 7);
        transform.localRotation = Quaternion.Euler(rotation);

        rb.velocity = new Vector3(currentHorizontalSpeed, rb.velocity.y, rb.velocity.z);

		//Set speed to minimum if less than minimum.
        if (rb.velocity.z > minimumSpeed) { //Remember, the minimum speed is negative.
            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, minimumSpeed);
        }
        //Debug.Log(currentHorizontalSpeed);
        //Wheel smoke
        if (Mathf.Abs(currentHorizontalSpeed) > 1000) {
            foreach (GameObject wheel in wheelSmoke) {
                wheel.GetComponent<ParticleSystem>().Play();
            }
        } else {
            foreach (GameObject wheel in wheelSmoke) {
                wheel.GetComponent<ParticleSystem>().Stop();
            }
        }

		_UpdateText();
    }

	float CalculateBonusHorizontalSpeed() { //Change this to take in 2 ranges and a multiplier later.
		float tVel = Mathf.Abs(rb.velocity.z);
        tVel = Mathf.Clamp(tVel, 8000, 20000);
		return Helper.Remap(tVel, 8000, 15000, -800, 1800);
	}

	void _UpdateText() {
		if (hSpeed != null) {
			hSpeed.text = "hSpeed: " + currentHorizontalSpeed.ToString();
		}

		if (tSpeed != null) {
			tSpeed.text = Math.Round(-1*(rb.velocity.z / 500.0f), 0, MidpointRounding.AwayFromZero) .ToString() + "m/s";
		}

        if (tScore != null) {
            tScore.text = "Score: " + (score / 500).ToString() + "m";
        }

        if (tPoliceSpeed != null) {
            if (desertMode) {
                tPoliceSpeed.text = "Police Speed: " + Math.Round((deathTresholdDesert / 500.0f * deathModifier), 0, MidpointRounding.AwayFromZero).ToString() + "m/s";
            } else {
                tPoliceSpeed.text = "Police Speed: " + Math.Round((deathTresholdCity / 500.0f * deathModifier), 0, MidpointRounding.AwayFromZero).ToString() + "m/s";
            }
        }
	}

    void OnTriggerEnter(Collider other) {
        //Debug.Log("Triggered with " + other.tag);

		

        if ((other.tag == "Pillar" || other.tag == "Rocket") && !shield.onOff) {
            Vector3 velocityChange = Vector3.forward * 2000;
            velocityChange.z += Mathf.Abs((rb.velocity.z / 10));

            rb.AddRelativeForce(velocityChange, ForceMode.VelocityChange);
            Info.getCameraShake().AddShake(40, 0.2f);
			////////////////////////////////////////
            udpSendRef.sendString("CrashedWall");

            ICollidable collidable = other.GetComponent<ICollidable>();
            if (collidable != null) {
                collidable.Collide();
            }

			Info.getDistortImageEffects().Quake();
            ObstacleExplosion.Explode(other.transform.position);
            GameObject.Find("CrashSound").GetComponent<AudioSource>().Play();
        }

        if (other.tag == "SpeedUpRing") {
            rb.AddRelativeForce(Vector3.forward * -2000, ForceMode.VelocityChange);
            //Info.getCameraShake().AddShake(40, 0.2f);
            //Info.getDistortImageEffects().Quake();
            blur.Quake();
            GameObject.Find("SpeedupSound").GetComponent<AudioSource>().Play();
			/////////////////////////////////////
			udpSendRef.sendString ("SpeedUpRing");
        }

        if (other.tag == "Portal") {
            if (!desertMode) {
                Info.getFollowPlayer().GoStraight(rb.velocity.z);

                Vector3 teleportPosition = other.GetComponent<Portal>().connectionPortal.transform.position;
                teleportPosition.y = transform.position.y;
                transform.position = teleportPosition;
                desertMode = true;

                GetComponent<PortalDesertSpawner>().EnterDesert();
            } else {
                Info.getFollowPlayer().GoStraight(rb.velocity.z);

                Vector3 teleportPosition = other.GetComponent<Portal>().connectionPortal.transform.position;
                teleportPosition.y = transform.position.y;
                transform.position = teleportPosition;
                desertMode = false;

                GetComponent<PortalDesertSpawner>().EnterCity();
            }

            if (rb.velocity.z > -6500 * deathModifier) {
                rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, -6500 * deathModifier);
            }

            GameObject.Find("PortalSound").GetComponent<AudioSource>().Play();
        }

        if (other.tag == "CityCrack") {
            StartDropDown();
            GameObject.Find("FadeOut").GetComponent<FadeOut>().StartFadeOut();
        }

        if (other.tag == "DesertPlane") {
            desertPlane = other.gameObject;
        }
    }

    void DashRight() {
        boostHorizontalSpeed = 15000;
    }

    void DashLeft() {
        boostHorizontalSpeed = 15000;
    }

    void DieExplode() {
        Destroy(gameObject);
    }

    void DropDown() {
        rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.deltaTime / 0.15f);
        rb.transform.localPosition += Vector3.down * 1000 * Time.deltaTime;
    }

    void StartDropDown() {
        dropDown = true;
        Info.getFollowPlayer().birdsEyeView = true;

        Invoke("DieExplode", 2.0f);
    }

    void ReadyToDie() {
        readyToDie = true;
    }
}
