
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using StarterAssets;
using System.Collections;

public class Boost : MonoBehaviour
{
    [Header("Jump Boost")]
    [SerializeField] private float jumpMultiplier = 1.8f;
    [SerializeField] private float boostDuration = 3f;

    [Header("UI")]
    [SerializeField] private GameObject jumpBoostUI;
    [SerializeField] private TMP_Text boostTimerText;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject okayPanel;
    [SerializeField] private float okayDuration = 1.5f;

    [Header("Tutorial")]
    [SerializeField] private GameObject tutorialBooster;

    [Header("Leg Appearance")]
    [SerializeField] private Renderer legRenderer;
    [SerializeField] private int materialIndex = 1;
    [SerializeField] private Color boostLegColor = Color.yellow;
    [SerializeField] private Color boostEmissionColor = Color.yellow;
    [SerializeField] private float emissionIntensity = 3f;

    private ThirdPersonController controller;

    private Material legMaterial;
    private Color originalLegColor;
    private Color originalEmissionColor;
    private bool originalEmissionEnabled;

    private float originalJumpHeight;
    private float remainingBoostTime;

    private bool hasBoost;
    private bool tutorialShown;
    private bool waitingForSpace;

    private Coroutine okayTimer;

    private void Awake()
    {
        controller = GetComponent<ThirdPersonController>();

        if (jumpBoostUI != null)
            jumpBoostUI.SetActive(false);

        if (boostTimerText != null)
            boostTimerText.gameObject.SetActive(false);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (okayPanel != null)
            okayPanel.SetActive(false);

        if (legRenderer != null)
        {
            Material[] materials = legRenderer.materials;

            if (materialIndex >= 0 &&
                materialIndex < materials.Length)
            {
                legMaterial = materials[materialIndex];

                originalLegColor = legMaterial.color;

                if (legMaterial.HasProperty("_EmissionColor"))
                {
                    originalEmissionColor =
                        legMaterial.GetColor("_EmissionColor");

                    originalEmissionEnabled =
                        legMaterial.IsKeywordEnabled("_EMISSION");
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("JumpBooster"))
            return;

        if (controller == null)
        {
            Debug.LogWarning("Missing ThirdPersonController!");
            return;
        }

        // Save original jump height once
        if (!hasBoost)
            originalJumpHeight = controller.JumpHeight;

        // Activate boost
        controller.JumpHeight =
            originalJumpHeight * jumpMultiplier;

        hasBoost = true;

        // Reset duration on every pickup
        remainingBoostTime = Mathf.Max(0.01f, boostDuration);

        // Show UI
        if (jumpBoostUI != null)
            jumpBoostUI.SetActive(true);

        if (boostTimerText != null)
            boostTimerText.gameObject.SetActive(true);

        UpdateTimerUI();

        // Change leg appearance
        if (legMaterial != null)
        {
            legMaterial.color = boostLegColor;

            if (legMaterial.HasProperty("_EmissionColor"))
            {
                legMaterial.EnableKeyword("_EMISSION");

                legMaterial.SetColor(
                    "_EmissionColor",
                    boostEmissionColor * emissionIntensity
                );
            }
        }

        // Tutorial only once
        bool startTutorial =
            !tutorialShown &&
            (tutorialBooster == null ||
             other.gameObject == tutorialBooster);

        Destroy(other.gameObject);

        if (startTutorial)
            ShowTutorial();

        Debug.Log("Jump Boost Collected!");
    }

    private void Update()
    {
        // Boost countdown
        if (hasBoost)
        {
            remainingBoostTime -= Time.deltaTime;

            if (remainingBoostTime <= 0f)
            {
                remainingBoostTime = 0f;
                RestoreBoost();
            }
            else
            {
                UpdateTimerUI();
            }
        }

        // Tutorial Spacebar detection
        if (waitingForSpace &&
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CompleteTutorial();
        }
    }

    // NEW: Update countdown UI
    private void UpdateTimerUI()
    {
        if (boostTimerText != null)
        {
            boostTimerText.text =
                 $"{Mathf.CeilToInt(remainingBoostTime)}";
        }
    }

    private void ShowTutorial()
    {
        tutorialShown = true;
        waitingForSpace = true;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        Debug.Log("Jump Tutorial Started!");
    }

    private void CompleteTutorial()
    {
        waitingForSpace = false;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (okayPanel != null)
        {
            okayPanel.SetActive(true);

            if (okayTimer != null)
                StopCoroutine(okayTimer);

            okayTimer = StartCoroutine(HideOkayPanel());
        }

        Debug.Log("Jump Tutorial Completed!");
    }

    private IEnumerator HideOkayPanel()
    {
        yield return new WaitForSeconds(
            Mathf.Max(0f, okayDuration)
        );

        if (okayPanel != null)
            okayPanel.SetActive(false);

        okayTimer = null;
    }

    private void RestoreBoost()
    {
        if (!hasBoost)
            return;

        hasBoost = false;
        remainingBoostTime = 0f;

        // Restore jump height
        if (controller != null)
            controller.JumpHeight = originalJumpHeight;

        // Hide boost UI
        if (jumpBoostUI != null)
            jumpBoostUI.SetActive(false);

        // Hide countdown UI
        if (boostTimerText != null)
            boostTimerText.gameObject.SetActive(false);

        // Restore leg appearance
        if (legMaterial != null)
        {
            legMaterial.color = originalLegColor;

            if (legMaterial.HasProperty("_EmissionColor"))
            {
                legMaterial.SetColor(
                    "_EmissionColor",
                    originalEmissionColor
                );

                if (originalEmissionEnabled)
                    legMaterial.EnableKeyword("_EMISSION");
                else
                    legMaterial.DisableKeyword("_EMISSION");
            }
        }

        Debug.Log("Jump Boost Expired!");
    }

    private void OnDisable()
    {
        if (okayTimer != null)
        {
            StopCoroutine(okayTimer);
            okayTimer = null;
        }

        if (hasBoost)
            RestoreBoost();

        waitingForSpace = false;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (okayPanel != null)
            okayPanel.SetActive(false);
    }
}
