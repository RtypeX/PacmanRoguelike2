using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// MainMenuAnimations - Handles all main menu animations.
/// Attach to an empty GameObject in your MainMenu scene.
///
/// SETUP:
/// - Assign titleText
/// - Assign all buttons in order (Start, Upgrades, Settings, Quit)
/// - Buttons slide in from left on load with staggered delay
/// - Title pulses continuously
/// - Buttons scale up on hover
/// </summary>
public class MainMenuAnimations : MonoBehaviour
{
    [Header("Title")]
    public TextMeshProUGUI titleText;
    public float titlePulseMin = 0.95f;
    public float titlePulseMax = 1.05f;
    public float titlePulseSpeed = 1.5f;

    [Header("Buttons - assign in order")]
    public List<Button> menuButtons;
    public float slideInDistance = 600f;
    public float slideInDuration = 0.4f;
    public float slideInStagger = 0.1f;
    public float hoverScale = 1.08f;
    public float hoverDuration = 0.15f;

    private List<Vector2> originalPositions = new List<Vector2>();

    private void Start()
    {
        SetupButtonHovers();
        StartCoroutine(SlideButtonsIn());

        if (titleText != null)
            StartCoroutine(PulseTitle());
    }

    // ---- Title Pulse --------------------------------------------------------

    private IEnumerator PulseTitle()
    {
        while (true)
        {
            LeanTween.scale(titleText.gameObject, Vector3.one * titlePulseMax, titlePulseSpeed)
                .setEaseInOutSine();
            yield return new WaitForSeconds(titlePulseSpeed);
            LeanTween.scale(titleText.gameObject, Vector3.one * titlePulseMin, titlePulseSpeed)
                .setEaseInOutSine();
            yield return new WaitForSeconds(titlePulseSpeed);
        }
    }

    // ---- Button Slide In ----------------------------------------------------

    private IEnumerator SlideButtonsIn()
    {
        // Store original positions and move buttons off screen to the left
        foreach (var btn in menuButtons)
        {
            if (btn == null) continue;
            RectTransform rt = btn.GetComponent<RectTransform>();
            originalPositions.Add(rt.anchoredPosition);
            rt.anchoredPosition = new Vector2(-slideInDistance, rt.anchoredPosition.y);
            btn.gameObject.SetActive(true);
        }

        // Slide each button in with stagger
        for (int i = 0; i < menuButtons.Count; i++)
        {
            if (menuButtons[i] == null) continue;
            RectTransform rt = menuButtons[i].GetComponent<RectTransform>();
            Vector2 target = originalPositions[i];

            LeanTween.value(menuButtons[i].gameObject,
                (float val) => rt.anchoredPosition = new Vector2(val, rt.anchoredPosition.y),
                -slideInDistance, target.x, slideInDuration)
                .setEaseOutCubic()
                .setDelay(i * slideInStagger);

            yield return new WaitForSeconds(slideInStagger);
        }
    }

    // ---- Button Hover -------------------------------------------------------

    private void SetupButtonHovers()
    {
        foreach (var btn in menuButtons)
        {
            if (btn == null) continue;

            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            GameObject btnRef = btn.gameObject;
            Vector3 originalScale = btn.transform.localScale; // capture original scale

            // Hover enter - scale up from original
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) =>
            {
                LeanTween.cancel(btnRef);
                LeanTween.scale(btnRef, originalScale * hoverScale, hoverDuration).setEaseOutBack();
            });
            trigger.triggers.Add(enterEntry);

            // Hover exit - return to original
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) =>
            {
                LeanTween.cancel(btnRef);
                LeanTween.scale(btnRef, originalScale, hoverDuration).setEaseOutCubic();
            });
            trigger.triggers.Add(exitEntry);

            // Click - squish then return
            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) =>
            {
                LeanTween.cancel(btnRef);
                LeanTween.scale(btnRef, originalScale * 0.9f, 0.05f).setEaseInOutSine()
                    .setOnComplete(() => LeanTween.scale(btnRef, originalScale, 0.1f).setEaseOutBack());
            });
            trigger.triggers.Add(clickEntry);
        }
    }
}