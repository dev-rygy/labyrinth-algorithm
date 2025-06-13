/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/04/2025
 * Last Modified:   06/12/2025 (Ryan)
 * Notes:           Controls the user interface of the debug console
*/
using UnityEngine;

using RyansLibrary.Input;

public class ConsoleUI : UIBehaviour
{
    private void Start()
    {
        InputHandler.OnConsoleOpen += OpenConsole;
        InputHandler.OnConsoleClose += CloseConsole;
    }

    private void OpenConsole()
    {
        Show();
    }

    private void CloseConsole()
    {
        Hide();
    }
}
