/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/12/2025
 * Last Modified:   02/19/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Abilities;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityUI : MonoBehaviour
{
    [Header("Images")]
    // Foreground Images
    [SerializeField] private Image _primaryComboImage;
    [SerializeField] private Image _primaryPowerImage;
    [SerializeField] private Image _secondaryComboImage;
    [SerializeField] private Image _secondaryPowerImage;
    [SerializeField] private Image _dashImage;

    // Background Images
    [SerializeField] private Image _primaryComboBGImage;
    [SerializeField] private Image _primaryPowerBGImage;
    [SerializeField] private Image _secondaryComboBGImage;
    [SerializeField] private Image _secondaryPowerBGImage;
    [SerializeField] private Image _dashBGImage;

    [Header("Text")]
    [SerializeField] private TMP_Text _primaryComboText;
    [SerializeField] private TMP_Text _primaryPowerText;
    [SerializeField] private TMP_Text _secondaryComboText;
    [SerializeField] private TMP_Text _secondaryPowerText;
    [SerializeField] private TMP_Text _dashText;

    private void OnEnable()
    {
        _primaryComboBGImage.fillAmount = 0;        // Disable cooldown gradient on start
        _primaryPowerBGImage.fillAmount = 0;
        _secondaryComboBGImage.fillAmount = 0;
        _secondaryPowerBGImage.fillAmount = 0;
        _dashBGImage.fillAmount = 0;

        _primaryComboText.enabled = false;      // Disable cooldown text on start
        _primaryPowerText.enabled = false;
        _secondaryComboText.enabled = false;
        _secondaryPowerText.enabled = false;
        _dashText.enabled = false;

        if (PlayerStateMachine.Instance == null)
        {
            Debug.LogError("Ability UI Error: PlayerStateMachine instance is null. Unable to subscribe to OnAbilityChanged.");
            this.enabled = false;
            return;
        }

        PlayerStateMachine.Instance.OnAbilityChanged += AssignAbility;  // Receive callbacks for equipped abilities
    }

    private void OnDisable()
    {
        if (PlayerStateMachine.Instance == null)
            return;

        PlayerStateMachine.Instance.OnAbilityChanged -= AssignAbility;  // Receive callbacks for equipped abilities
    }

    // Ability Assignment
    public void AssignAbility(Ability ability)
    {
        switch (ability.Type)
        {
            case AbilityType.ComboAttackPrimary:        // Assign Primary Combo Ability
                _primaryComboImage.sprite = ability.abilityIcon;
                _primaryComboBGImage.sprite = ability.abilityIcon;
                _primaryComboBGImage.fillAmount = 0;
                _primaryComboText.enabled = false;
                ability.OnAbilityCooldown += PrimaryComboCooldownUI;
                return;
            case AbilityType.PowerAttackPrimary:        // Assign Primary Power Ability
                _primaryPowerImage.sprite = ability.abilityIcon;
                _primaryPowerBGImage.sprite = ability.abilityIcon;
                _primaryPowerBGImage.fillAmount = 0;
                _primaryPowerText.enabled = false;
                ability.OnAbilityCooldown += PrimaryPowerCooldownUI;
                return;
            case AbilityType.ComboAttackSecondary:      // Assign Secondary Combo Ability
                _secondaryComboImage.sprite = ability.abilityIcon;
                _secondaryComboBGImage.sprite = ability.abilityIcon;
                _secondaryComboBGImage.fillAmount = 0;
                _secondaryComboText.enabled = false;
                ability.OnAbilityCooldown += SecondaryComboCooldownUI;
                return;
            case AbilityType.PowerAttackSecondary:      // Assign Secondary Power Ability
                _secondaryPowerImage.sprite = ability.abilityIcon;
                _secondaryPowerBGImage.sprite = ability.abilityIcon;
                _secondaryPowerBGImage.fillAmount = 0;
                _secondaryPowerText.enabled = false;
                ability.OnAbilityCooldown += SecondaryPowerCooldownUI;
                return;
            case AbilityType.Dash:
                _dashImage.sprite = ability.abilityIcon;
                _dashBGImage.sprite = ability.abilityIcon;
                _dashBGImage.fillAmount = 0;
                _dashText.enabled = false;
                ability.OnAbilityCooldown += DashCooldownUI;
                return;
            default:
                Debug.LogError("Ability UI Error: Ability type undefined.");
                return;
        }
    }

    // Ability Removal
    public void RemoveAbility(Ability ability)
    {
        switch (ability.Type)
        {
            case AbilityType.ComboAttackPrimary:        // Assign Primary Combo Ability
                _primaryComboImage.sprite = null;
                _primaryComboBGImage.sprite = null;
                _primaryComboBGImage.fillAmount = 0;
                _primaryComboText.enabled = false;
                ability.OnAbilityCooldown -= PrimaryComboCooldownUI;
                return;
            case AbilityType.PowerAttackPrimary:        // Assign Primary Power Ability
                _primaryPowerImage.sprite = null;
                _primaryPowerBGImage.sprite = null;
                _primaryPowerBGImage.fillAmount = 0;
                _primaryPowerText.enabled = false;
                ability.OnAbilityCooldown -= PrimaryPowerCooldownUI;
                return;
            case AbilityType.ComboAttackSecondary:      // Assign Secondary Combo Ability
                _secondaryComboImage.sprite = null;
                _secondaryComboBGImage.sprite = null;
                _secondaryComboBGImage.fillAmount = 0;
                _secondaryComboText.enabled = false;
                ability.OnAbilityCooldown -= SecondaryComboCooldownUI;
                return;
            case AbilityType.PowerAttackSecondary:      // Assign Secondary Power Ability
                _secondaryPowerImage.sprite = null;
                _secondaryPowerBGImage.sprite = null;
                _secondaryPowerBGImage.fillAmount = 0;
                _secondaryPowerText.enabled = false;
                ability.OnAbilityCooldown -= SecondaryPowerCooldownUI;
                return;
            case AbilityType.Dash:      // Assign Secondary Power Ability
                _dashImage.sprite = null;
                _dashBGImage.sprite = null;
                _dashBGImage.fillAmount = 0;
                _dashText.enabled = false;
                ability.OnAbilityCooldown -= DashCooldownUI;
                return;
            default:
                Debug.LogError("Ability UI Error: Ability type undefined.");
                return;
        }
    }

    // Cooldown UI
    private void PrimaryComboCooldownUI(float timeLeft, float cooldownTime)
    {
        // Ratio of time between [1,0]; can never be negative
        float normalizedCooldownRatio = Mathf.Max(timeLeft, 0) / cooldownTime;
        _primaryComboBGImage.fillAmount = normalizedCooldownRatio;

        if (normalizedCooldownRatio > 0)        // If ability is on cooldown
        {
            _primaryComboImage.color = Color.gray;
            _primaryComboText.enabled = true;

            if (timeLeft > 1.0f)        // If the time left for the cooldown is < 1.0 then show decimal values in UI
            {
                _primaryComboText.text = timeLeft.ToString("F0");
            }
            else
            {
                _primaryComboText.text = timeLeft.ToString("F1");
            }
        }
        else
        {
            _primaryComboImage.color = Color.white;
            _primaryComboText.enabled = false;
        }
    }

    private void PrimaryPowerCooldownUI(float timeLeft, float cooldownTime)
    {
        // Ratio of time between [1,0]; can never be negative
        float normalizedCooldownRatio = Mathf.Max(timeLeft, 0) / cooldownTime;
        _primaryPowerBGImage.fillAmount = normalizedCooldownRatio;

        if (normalizedCooldownRatio > 0)
        {
            _primaryPowerImage.color = Color.gray;
            _primaryPowerText.enabled = true;

            if (timeLeft > 1.0f)
            {
                _primaryPowerText.text = timeLeft.ToString("F0");
            }
            else
            {
                _primaryPowerText.text = timeLeft.ToString("F1");
            }
        }
        else
        {
            _primaryPowerImage.color = Color.white;
            _primaryPowerText.enabled = false;
        }
    }

    private void SecondaryComboCooldownUI(float timeLeft, float cooldownTime)
    {
        // Ratio of time between [1,0]; can never be negative
        float normalizedCooldownRatio = Mathf.Max(timeLeft, 0) / cooldownTime;
        _secondaryComboBGImage.fillAmount = normalizedCooldownRatio;

        if (normalizedCooldownRatio > 0)
        {
            _secondaryComboImage.color = Color.gray;
            _secondaryComboText.enabled = true;

            if (timeLeft > 1.0f)
            {
                _secondaryComboText.text = timeLeft.ToString("F0");
            }
            else
            {
                _secondaryComboText.text = timeLeft.ToString("F1");
            }
        }
        else
        {
            _secondaryComboImage.color = Color.white;
            _secondaryComboText.enabled = false;
        }
    }

    private void SecondaryPowerCooldownUI(float timeLeft, float cooldownTime)
    {
        // Ratio of time between [1,0]; can never be negative
        float normalizedCooldownRatio = Mathf.Max(timeLeft, 0) / cooldownTime;
        _secondaryPowerBGImage.fillAmount = normalizedCooldownRatio;

        if (normalizedCooldownRatio > 0)
        {
            _secondaryPowerImage.color = Color.grey;
            _secondaryPowerText.enabled = true;

            if (timeLeft > 1.0f)
            {
                _secondaryPowerText.text = timeLeft.ToString("F0");
            }
            else
            {
                _secondaryPowerText.text = timeLeft.ToString("F1");
            }
        }
        else
        {
            _secondaryPowerImage.color = Color.white;
            _secondaryPowerText.enabled = false;
        }
    }

    private void DashCooldownUI(float timeLeft, float cooldownTime)
    {
        // Ratio of time between [1,0]; can never be negative
        float normalizedCooldownRatio = Mathf.Max(timeLeft, 0) / cooldownTime;
        _dashBGImage.fillAmount = normalizedCooldownRatio;

        if (normalizedCooldownRatio > 0)
        {
            _dashImage.color = Color.grey;
            _dashText.enabled = true;

            if (timeLeft > 1.0f)
            {
                _dashText.text = timeLeft.ToString("F0");
            }
            else
            {
                _dashText.text = timeLeft.ToString("F1");
            }
        }
        else
        {
            _dashImage.color = Color.white;
            _dashText.enabled = false;
        }
    }
}

