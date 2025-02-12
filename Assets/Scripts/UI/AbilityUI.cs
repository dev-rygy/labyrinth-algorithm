/*
 * Created By:      Ryan Carpenter
 * Date Created:    02/12/2025
 * Last Modified:   02/12/2025 (Ryan)
 * Notes:           
*/
using System.Collections;
using System.Collections.Generic;
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

    // Background Images
    [SerializeField] private Image _primaryComboBGImage;
    [SerializeField] private Image _primaryPowerBGImage;
    [SerializeField] private Image _secondaryComboBGImage;
    [SerializeField] private Image _secondaryPowerBGImage;

    [Header("Text")]
    [SerializeField] private TMP_Text _primaryComboText;
    [SerializeField] private TMP_Text _primaryPowerText;
    [SerializeField] private TMP_Text _secondaryComboText;
    [SerializeField] private TMP_Text _secondaryPowerText;

    // Start is called before the first frame update
    private void Start()
    {
        _primaryComboBGImage.fillAmount = 0;
        _primaryPowerBGImage.fillAmount = 0;
        _secondaryComboBGImage.fillAmount = 0;
        _secondaryPowerBGImage.fillAmount = 0;

        _primaryComboText.enabled = false;      // Disable cooldown text if not in use
        _primaryPowerText.enabled = false;      // Disable cooldown text if not in use
        _secondaryComboText.enabled = false;      // Disable cooldown text if not in use
        _secondaryPowerText.enabled = false;      // Disable cooldown text if not in use
    }

    // TODO: Add ability image arguement
    public void AssignPrimaryComboAbility(Ability ability)
    {
        _primaryComboBGImage.fillAmount = 0;
        _primaryComboText.enabled = false;
        ability.OnAbilityCooldown += PrimaryComboCooldownUI;
    }

    public void AssignPrimaryPowerAbility(Ability ability)
    {
        _primaryPowerBGImage.fillAmount = 0;
        _primaryPowerText.enabled = false;
        ability.OnAbilityCooldown += PrimaryPowerCooldownUI;
    }

    public void AssignSecondaryComboAbility(Ability ability)
    {
        _secondaryComboBGImage.fillAmount = 0;
        _secondaryComboText.enabled = false;
        ability.OnAbilityCooldown += SecondaryComboCooldownUI;
    }

    public void AssignSecondaryPowerAbility(Ability ability)
    {
        _secondaryPowerBGImage.fillAmount = 0;
        _secondaryPowerText.enabled = false;
        ability.OnAbilityCooldown += SecondaryPowerCooldownUI;
    }

    private void PrimaryComboCooldownUI(float timeLeft, float cooldownTime)
    {
        // Ratio of time between [1,0]; can never be negative
        float normalizedCooldownRatio = Mathf.Max(timeLeft, 0) / cooldownTime;
        _primaryComboBGImage.fillAmount = normalizedCooldownRatio;

        if (normalizedCooldownRatio > 0)
        {
            _primaryComboImage.color = Color.gray;
            _primaryComboText.enabled = true;

            if (timeLeft > 1.0f)
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
}
