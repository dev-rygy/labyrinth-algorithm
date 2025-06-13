/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/04/2025
 * Last Modified:   06/12/2025 (Ryan)
 * Notes:           Controls common behaviors between
 *                  all UI's
*/
using UnityEngine;

public class UIBehaviour : MonoBehaviour
{
    // Event
    public System.Action<bool> OnVisibilityChanged;

    [Header("UI Behaviour Settings")]
    [SerializeField] protected GameObject UIContainer;
    
    private bool _isShowing;

    private void OnEnable()
    {
        _isShowing = UIContainer.activeSelf;
    }

    public void Show()
    {
        if (_isShowing)     // Already showing? Yes, no need to show again
            return;

        // Enable UI container
        _isShowing = true;
        UIContainer.SetActive(true);
    }

    public void Hide()
    {
        if (!_isShowing)    // Already hiding? Yes, no need to hide again
            return;

        // Disable UI container
        _isShowing = false;
        UIContainer.SetActive(false);
    }

    [ContextMenu("Toggle UI Container")]
    public virtual void ToggleShow()
    {
        if (_isShowing)
            Hide();
        else
            Show();
    }
}
