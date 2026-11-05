
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class Boost : MonoBehaviour
{
    [Header("Jump Boost")]
    [SerializeField] private float jumpMultiplier = 1.8f;

    [Header("UI")]
    [SerializeField] private GameObject jumpBoostUI;

    [SerializeField] private int materialIndex = 1;

    [Header("Leg Color")]
    [SerializeField] private Renderer legRenderer;
    [SerializeField] private Color boostLegColor = Color.yellow;


    [Header("Leg Emission")]
    [SerializeField] private Color boostEmissionColor = Color.yellow;
    [SerializeField] private float emissionIntensity = 2f;

    private Color originalEmissionColor;
    private bool originalEmissionEnabled;


    private Material legMaterial;
    private Color originalLegColor;


    private ThirdPersonController controller;
    private float originalJumpHeight;

    private bool hasBoost = false;
    private bool useBoost = false;

    private void Awake()
    {
        controller = GetComponent<ThirdPersonController>();

        if (jumpBoostUI != null)
            jumpBoostUI.SetActive(false);


        if (legRenderer != null)
        {
            if (legRenderer != null)
            {
                Material[] materials = legRenderer.materials;

                if (materialIndex >= 0 && materialIndex < materials.Length)
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
            originalLegColor = legMaterial.color;
        }


        if (legMaterial != null && legMaterial.HasProperty("_EmissionColor"))
        {
            originalEmissionColor = legMaterial.GetColor("_EmissionColor");
            originalEmissionEnabled = legMaterial.IsKeywordEnabled("_EMISSION");
        }


    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("JumpBooster"))
            return;

        // Save original height
        if (!hasBoost)
            originalJumpHeight = controller.JumpHeight;

        // Activate jump boost
        controller.JumpHeight = originalJumpHeight * jumpMultiplier;
        hasBoost = true;

        // Show UI
        if (jumpBoostUI != null)
            jumpBoostUI.SetActive(true);

        // Change leg color
        if (legMaterial != null)
            legMaterial.color = boostLegColor;


        if (legMaterial != null && legMaterial.HasProperty("_EmissionColor"))
        {
            legMaterial.EnableKeyword("_EMISSION");

            legMaterial.SetColor(
                "_EmissionColor",
                boostEmissionColor * emissionIntensity
            );
        }



        // Remove collected item
        Destroy(other.gameObject);

        Debug.Log("Jump Boost Activated!");
    }

    private void Update()
    {
        if (!hasBoost)
            return;

        // Detect Spacebar input
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            useBoost = true;
        }
    }

    private void LateUpdate()
    {
        if (!useBoost)
            return;

        // Restore jump height
        controller.JumpHeight = originalJumpHeight;

        // Hide UI
        if (jumpBoostUI != null)
            jumpBoostUI.SetActive(false);

        RestoreLegAppearance();

        // Restore original leg color
        if (legMaterial != null)
            legMaterial.color = originalLegColor;

        hasBoost = false;
        useBoost = false;

        Debug.Log("Jump Boost Used!");
    }

    private void OnDisable()
    {
        if (hasBoost && controller != null)
            controller.JumpHeight = originalJumpHeight;

        if (jumpBoostUI != null)
            jumpBoostUI.SetActive(false);

        hasBoost = false;
        useBoost = false;


        if (legMaterial != null)
            legMaterial.color = originalLegColor;

    }


    private void RestoreLegAppearance()
    {
        if (legMaterial == null)
            return;

        // Restore original color
        legMaterial.color = originalLegColor;

        // Restore original emission
        if (legMaterial.HasProperty("_EmissionColor"))
        {
            legMaterial.SetColor("_EmissionColor", originalEmissionColor);

            if (originalEmissionEnabled)
                legMaterial.EnableKeyword("_EMISSION");
            else
                legMaterial.DisableKeyword("_EMISSION");
        }
    }

}
