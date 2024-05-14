using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// This script have actions of Start and Exit Buttons of Room scene
/// </summary>
public class BounceButton : MonoBehaviour
{
    private float bounceScale = 1.5f;
    private float bounceDuration = 1f;
    private Vector3 originalScale;

    private Button startButton;

    private void Start()
    {
    }

    private void Update()
    {

    }

    public void Bounce()
    {
        startButton.gameObject.SetActive(true);
        originalScale = startButton.transform.localScale;
        StartCoroutine(BounceCoroutine());
    }

    public IEnumerator BounceCoroutine()
    {
        float timer = 0f;

        while (timer < bounceDuration)
        {
            float scaleFactor = Mathf.Lerp(1f, bounceScale, timer / bounceDuration);
            startButton.transform.localScale = originalScale * scaleFactor;

            timer += Time.deltaTime;
            yield return null;
        }

        startButton.transform.localScale = originalScale * bounceScale;

        yield return new WaitForSeconds(0.5f); // Wait for a short time at the peak

        timer = 0f;

        while (timer < bounceDuration)
        {
            float scaleFactor = Mathf.Lerp(bounceScale, 1f, timer / bounceDuration);
            startButton.transform.localScale = originalScale * scaleFactor;

            timer += Time.deltaTime;
            yield return null;
        }

        startButton.transform.localScale = originalScale;
    }

    public void StartGame()
    {
        //startButton.gameObject.SetActive(false);
        HostManager HostManager = GameObject.FindObjectOfType<HostManager>();
        HostManager.StartGame();
    }

    public void ExitGame()
    {
        HandleMainSceneAlert mainSceneAlert = GameObject.FindObjectOfType<HandleMainSceneAlert>();
        mainSceneAlert.ExitConfirmation();
    }

    public void ExitGameConfirmed()
    {
        RoomManager RoomManager = GameObject.FindObjectOfType<RoomManager>();
        RoomManager.ExitGame();
    }
}
