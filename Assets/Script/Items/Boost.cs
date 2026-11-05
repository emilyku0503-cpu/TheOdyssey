
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class Boost : MonoBehaviour
{
    [Header("Jump Boost")]
    [SerializeField] private float jumpMultiplier = 1.8f;

    [Header("UI")]
    [SerializeField] private GameObject jumpBoostUI;
    [SerializeField] private GameObject tutorialPanel;

    [Header("Tutorial")]
    [SerializeField] private GameObject tutorialBooster;

    [Header("Leg Appearance")]
    [SerializeField] private Renderer legRenderer;
    [SerializeField] private int materialIndex = 1;
    [SerializeField] private Color boostLegColor = Color.yellow;
    [SerializeField] private Color boostEmissionColor = Color.yellow;
    [SerializeField] private float emissionIntensity = 3f;

    private ThirdPersonController controller;
    private StarterAssetsInputs inputs;
    private PlayerInput playerInput;

    private Material legMaterial;
    private Color originalLegColor;
    private Color originalEmissionColor;
    private bool originalEmissionEnabled;

    private float originalJumpHeight;

    private bool hasBoost;
    private bool consuming;
    private bool tutorialOpen;
    private bool tutorialShown;

    private bool forcedTutorialJump;
    private float savedTimeScale;
    private bool savedControllerEnabled;
    private bool savedInputActive;

    private void Awake()
    {
        controller = GetComponent<ThirdPersonController>();
        inputs = GetComponent<StarterAssetsInputs>();
        playerInput = GetComponent<PlayerInput>();

        if (jumpBoostUI != null)
            jumpBoostUI.SetActive(false);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

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

        if (controller == null || inputs == null ||
            playerInput == null)
        {
            Debug.LogWarning("Missing player components!");
            return;
        }

        // Save original jump height only once
        if (!hasBoost)
            originalJumpHeight = controller.JumpHeight;

        // Activate boost
        controller.JumpHeight =
            originalJumpHeight * jumpMultiplier;

        hasBoost = true;

        if (jumpBoostUI != null)
            jumpBoostUI.SetActive(true);

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

        // Check whether this is the tutorial booster
        bool startTutorial =
            !tutorialShown &&
            tutorialBooster != null &&
            other.gameObject == tutorialBooster;

        Destroy(other.gameObject);

        if (startTutorial)
            StartTutorial();

        Debug.Log("Jump Boost Collected!");
    }

    private void StartTutorial()
    {
        tutorialShown = true;
        tutorialOpen = true;

        // Show tutorial UI
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        // Save current state
        savedTimeScale = Time.timeScale;
        savedControllerEnabled = controller.enabled;
        savedInputActive = playerInput.inputIsActive;

        // Stop movement and camera controls
        inputs.move = Vector2.zero;
        inputs.look = Vector2.zero;
        inputs.jump = false;
        inputs.sprint = false;

        playerInput.DeactivateInput();
        controller.enabled = false;

        // Pause game
        Time.timeScale = 0f;

        Debug.Log("Jump Tutorial Started!");
    }

    private void Update()
    {
        if (!hasBoost || consuming)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (tutorialOpen)
            {
                CompleteTutorial();
            }
            else
            {
                ConsumeBoost();
            }
        }
    }

    private void CompleteTutorial()
    {
        // Hide tutorial and resume controls
        ResumeTutorial();

        // Force the jump that was pressed during pause
        inputs.move = Vector2.zero;
        inputs.look = Vector2.zero;
        inputs.jump = true;

        forcedTutorialJump = true;

        // Consume after ThirdPersonController processes jump
        ConsumeBoost();

        Debug.Log("Jump Tutorial Completed!");
    }

    private void ResumeTutorial()
    {
        if (!tutorialOpen)
            return;

        tutorialOpen = false;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        Time.timeScale = savedTimeScale;

        controller.enabled = savedControllerEnabled;

        if (savedInputActive)
            playerInput.ActivateInput();
    }

    private void ConsumeBoost()
    {
        if (!hasBoost || consuming)
            return;

        consuming = true;
        StartCoroutine(FinishBoost());
    }

    private IEnumerator FinishBoost()
    {
        // Wait until the next frame's controller Update
        // has processed the boosted jump
        yield return null;

        RestoreBoost();

        if (forcedTutorialJump && inputs != null)
        {
            inputs.jump = false;
            forcedTutorialJump = false;
        }

        consuming = false;
    }

    private void RestoreBoost()
    {
        if (!hasBoost)
            return;

        hasBoost = false;

        // Restore jump height
        if (controller != null)
            controller.JumpHeight = originalJumpHeight;

        // Hide boost UI
        if (jumpBoostUI != null)
            jumpBoostUI.SetActive(false);

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

        Debug.Log("Jump Boost Used!");
    }

    private void OnDisable()
    {
        if (tutorialOpen)
            ResumeTutorial();

        if (hasBoost)
            RestoreBoost();

        if (forcedTutorialJump && inputs != null)
            inputs.jump = false;

        forcedTutorialJump = false;
    }
}
