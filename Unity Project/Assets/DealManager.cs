using UnityEngine;
using UnityEngine.UI;
using TMPro;    

[RequireComponent(typeof(CharacterManager))]
[RequireComponent(typeof(ContractManager))]
public class PitchObject : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Component References (auto-fetched or wired in Inspector)
    // ------------------------------------------------------------------ //
    [Header("Sub-components (auto-resolved if left empty)")]
    [SerializeField] private CharacterManager characterManager;
    [SerializeField] private ContractManager  contractManager;

    // The data asset currently loaded into this pitch
    public PitchData CurrentData { get; private set; }

    // ------------------------------------------------------------------ //
    //  Unity Lifecycle
    // ------------------------------------------------------------------ //
    private void Awake()
    {
        // Fall back to GetComponent if Inspector references are not set
        if (characterManager == null)
            characterManager = GetComponent<CharacterManager>();
        if (contractManager == null)
            contractManager  = GetComponent<ContractManager>();
    }

    // ------------------------------------------------------------------ //
    //  Public Lifecycle API  (called by RoundController)
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Load a PitchData asset into both sub-components without showing anything yet.
    /// Call this before transitioning into the pitch scene so data is ready instantly.
    /// </summary>
    public void Load(PitchData data)
    {
        if (data == null)
        {
            Debug.LogError("[PitchObject] Load called with null PitchData.");
            return;
        }

        CurrentData = data;
        characterManager.populate(data);
        contractManager.populate(data);

        Debug.Log($"[PitchObject] Loaded pitch: {data.characterName} — {data.contractTitle}");
    }

}
