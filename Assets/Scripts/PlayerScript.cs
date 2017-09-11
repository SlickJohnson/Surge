﻿using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;

public class PlayerScript : MonoBehaviour
{

    public float playerVelocity;
    private Vector3 playerPosition;
    private Vector3 touchPos;
    public float boundary;
    private int playerPoints;
    private int highestScore;
    private int currentLevelHighScore;

    public AudioClip breakSound;
    public AudioClip lifeSound;

    private Vector3 touchOrgPos;
    private float maintainedDist;
    private float lastBreakTime;

    public LevelManager levelManager;
    private int currentLevel;

    private GameObject hud;
    private GameObject playerLives;
    private Text hudScore;
    private GameObject levelDetailsCanvas;
    private GameObject pauseMenu;
    private Text levelDetailsText;
    private float timeToDisplayDetails = 20;
    private float detailsLoadedTime = 0;
    private int blocksDestroyed;
    private int numberOfBlocks;
    private bool isScoreWobbling;
    private float currentWobbleTimer;
    private float wobbleAmount = 1.4f;
    private Vector3 hudScoreScale;
    private float wobbleTimer = .1f;
    private Quaternion hudScoreOriginialRot;
    private int scoreRotationSide = 1;
    private float lastPoint;
    private float scoreMultiplier = 1;
    private Text scoreMultiplierText;
    private int savedDisplayedScore;
    private float pointAnimTimer;
    private float boostScore;

    // Use this for initialization

    void Awake()
    {
        hud = GameObject.Find("UI");
        pauseMenu = GameObject.Find("PauseMenu");
        playerLives = GameObject.Find("Lives");
        hudScore = hud.transform.Find("Score").GetComponent<Text>();
        scoreMultiplierText = hud.transform.Find("Multiplier").GetComponent<Text>();

        levelDetailsCanvas = GameObject.Find("LevelDetails");
        levelDetailsText = GameObject.Find("Details").GetComponent<Text>();

        numberOfBlocks = (GameObject.FindGameObjectsWithTag("Block")).Length;
    }
    void Start()
    {
        currentLevel = SceneManager.GetActiveScene().buildIndex;

        // get the initial position of the game object
        playerPosition = gameObject.transform.position;

        playerPoints = 0;
        highestScore = PlayerPrefs.GetInt("HighestScore", 0);
        currentLevelHighScore = PlayerPrefs.GetInt("HighScoreLevel" + (currentLevel - LevelManager.numberOfMenuScenes + 1), 0);

        levelManager.ChangeColor();

        ShowDetails();

        hudScoreScale = hudScore.transform.localScale;
        hudScoreOriginialRot = hudScore.transform.rotation;
    }

    private void ShowDetails()
    {
        levelDetailsText.text = "Level:\n" + (currentLevel - LevelManager.numberOfMenuScenes + 1) + "\n" + "Song:\n" + levelManager.songName + "\n" + "High Score:\n" + currentLevelHighScore;
        detailsLoadedTime = Time.time;
    }

    void Update()
    {
        if (!pauseMenu.activeInHierarchy)
        {
            if (timeToDisplayDetails > 0 && levelDetailsCanvas.activeInHierarchy == true)
            {
                levelDetailsCanvas.GetComponent<CanvasGroup>().alpha -= .001f;
                timeToDisplayDetails -= Time.deltaTime;
            }
            else
            {
                levelDetailsCanvas.SetActive(false);
            }

            // Movement on pc
            playerPosition.x += Input.GetAxis("Horizontal") * playerVelocity;

            // Get first touch position of finger
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                touchOrgPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 5));
                maintainedDist = touchOrgPos.x - playerPosition.x;
            }

            // Move player based on first touch position
            if (Input.touchCount == 1)
            {
                if (levelDetailsCanvas.activeInHierarchy == true)
                {
                    levelDetailsCanvas.SetActive(false);
                    timeToDisplayDetails = 2;

                    GameObject.Find("Ball").SendMessage("PlayerReady");
                }
                touchPos = Camera.main.ScreenToWorldPoint(new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 5));
                playerPosition = new Vector3(touchPos.x - maintainedDist, playerPosition.y, playerPosition.z);
            }
            if (Input.touchCount == 2 || Input.GetKeyDown(KeyCode.K))
            {
                if (Time.timeScale == 1)
                {
                    Time.timeScale = .2f;
                    Time.fixedDeltaTime = Time.timeScale * .02f;
                }
                else
                {
                    Time.timeScale = 1;
                    Time.fixedDeltaTime = Time.timeScale * .02f;
                }
            }

            // Switch level with 2 finger gesture
            if ((Input.touchCount == 3 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetKeyDown("n"))
            {
                if (currentLevel == SceneManager.sceneCountInBuildSettings - 1) SceneManager.LoadScene(LevelManager.numberOfMenuScenes);
                else SceneManager.LoadScene(currentLevel + 1);
            }

            // Quit
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameObject.Find("ButtonManager").GetComponent<ButtonManager>().PauseGame();
            }

            // Player boundaries
            if (playerPosition.x < -boundary)
            {
                playerPosition = new Vector3(-boundary, playerPosition.y, playerPosition.z);
            }
            if (playerPosition.x > boundary)
            {
                playerPosition = new Vector3(boundary, playerPosition.y, playerPosition.z);
            }

            // update the game object transform
            transform.position = playerPosition;

            // Wobble playerscore
            if (currentWobbleTimer > 0 && Time.timeScale > 0)
            {
                var wobblePos = UnityEngine.Random.insideUnitCircle * (wobbleAmount * Mathf.Clamp(scoreMultiplier / 2, 1, 2f));

                hudScore.transform.localScale = new Vector3(hudScoreScale.x + Math.Abs(wobblePos.x), hudScoreScale.y + Math.Abs(wobblePos.x), hudScoreScale.z);

                currentWobbleTimer -= Time.deltaTime;
            }
            else if (isScoreWobbling || hudScore.transform.localScale != hudScoreScale)
            {

                hudScore.transform.localScale = hudScoreScale;

                isScoreWobbling = false;
            }

            if (Time.time - lastPoint > 1f)
            {
                scoreMultiplier = 1f;
            }

            // update hud
            pointAnimTimer += Time.deltaTime;
            float prcComplete = pointAnimTimer / (wobbleTimer * 2.1f);
            hudScore.text = Math.Ceiling(Mathf.Lerp(savedDisplayedScore, playerPoints, prcComplete)).ToString();

            scoreMultiplierText.text = "x" + (scoreMultiplier + boostScore).ToString();

            // Check game state
            WinLose();
        }
    }

    void AddPoints(int points)
    {
        savedDisplayedScore = Int32.Parse(hudScore.text);
        playerPoints += (int)(points * (scoreMultiplier + boostScore));
        pointAnimTimer = 0f;

        scoreRotationSide *= -1;
        // hudScore.transform.Rotate(Vector3.forward, 45 * scoreRotationSide);

        if (Time.time - lastPoint < 1f)
        {
            scoreMultiplier += 0.5f;
        }
        else
        {
            scoreMultiplier = 1f;
        }

        lastPoint = Time.time;
        WobbleScore();
    }

    void Boost(int amount)
    {
        boostScore += amount;
    }

    void IncrementBlocksDestroyed()
    {
        blocksDestroyed++;

        if (Time.time - lastBreakTime > .01f)
            GetComponent<AudioSource>().PlayOneShot(breakSound);

        if (Time.time - lastBreakTime < .7f)
        {
            GetComponent<AudioSource>().pitch += .15f;
            lastBreakTime = Time.time;
        }
        else
        {
            GetComponent<AudioSource>().pitch = 1f;
            lastBreakTime = Time.time;
        }
    }

    void TakeLife()
    {
        Destroy(playerLives.transform.GetChild(playerLives.transform.childCount - 1).gameObject, .1f);
        GetComponent<AudioSource>().pitch = 1f;
        GetComponent<AudioSource>().PlayOneShot(lifeSound);

        boostScore = 0;
        scoreMultiplier = 1;

        scoreMultiplierText.text = "x" + (scoreMultiplier + boostScore).ToString();
    }

    void WinLose()
    {
        // restart the game
        if (playerLives.transform.childCount == 0)
        {
            ResetLevel();

            if (playerPoints > highestScore)
            {
                PlayerPrefs.SetInt("HighestScore", playerPoints);
            }

            if (playerPoints > currentLevelHighScore)
            {
                PlayerPrefs.SetInt("HighScoreLevel" + (currentLevel - LevelManager.numberOfMenuScenes + 1), playerPoints);
            }
        }

        // chech if all blocks are destroyed
        if (numberOfBlocks == blocksDestroyed)
        {
            // next level or quit
            if (currentLevel > SceneManager.sceneCountInBuildSettings) Application.Quit();
            else SceneManager.LoadScene(currentLevel + 1);
        }
    }

    void WobbleScore()
    {
        isScoreWobbling = true;
        currentWobbleTimer = wobbleTimer;
    }

    public void ResetLevel()
    {
        SceneManager.LoadScene(currentLevel);
        Time.timeScale = 1;
    }
}
