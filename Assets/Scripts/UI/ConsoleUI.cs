/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/04/2025
 * Last Modified:   00/14/2025 (Ryan)
 * Notes:           Controls the user interface of the debug console
*/
using RyansLibrary.Debugging;
using RyansLibrary.Input;
using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Console = RyansLibrary.Debugging.Console;

public class ConsoleUI : UIBehaviour
{
    public static event Action OnConsoleOpened;
    public static event Action OnConsoleClosed;

    [Header("Console Settings")]
    [SerializeField] private bool _toggleInputMemory;       // Toggle input mem on and off
    [SerializeField] private int _inputMemoryCapacity;      // Holds prev commands
    [SerializeField] private bool _enableDevCommands;       // Toggle dev commands on and off

    [Header("Autocomplete")]
    [SerializeField] private TMP_Text _suggestionText;      // A small text field under the input field
    [SerializeField] private int _maxSuggestions = 5;

    [Header("Tags")]
    [SerializeField] private string _errorTextTag;
    [SerializeField] private string _assertTextTag;
    [SerializeField] private string _warningTextTag;
    [SerializeField] private string _logTextTag;
    [SerializeField] private string _exceptionTextTag;

    [Header("Colors")]
    [SerializeField] private Color _errorTextColor;
    [SerializeField] private Color _assertTextColor;
    [SerializeField] private Color _warningTextColor;
    [SerializeField] private Color _logTextColor;
    [SerializeField] private Color _exceptionTextColor;
    [SerializeField] private Color _generalTextColor;

    [Header("Component References")]
    [SerializeField] private TMP_Text _outputText;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private ScrollRect _scrollRect;

    private Console _console;

    private bool _suppressSuggestionUpdate = false;
    private string _currentSuggestion;

    public void InitConsole()
    {
        _console = new Console(_inputMemoryCapacity, _maxSuggestions, _enableDevCommands)
        {
            // Set log appearance
            ErrorTextColor = _errorTextColor,
            AssertTextColor = _assertTextColor,
            WarningTextColor = _warningTextColor,
            LogTextColor = _logTextColor,
            ExceptionTextColor = _exceptionTextColor,
            GeneralTextColor = _generalTextColor,

            // Log Type Tags
            ErrorTextTag = _errorTextTag,
            AssertTextTag = _assertTextTag,
            WarningTextTag = _warningTextTag,
            LogTextTag = _logTextTag,
            ExceptionTextTag = _exceptionTextTag
        };

        // Subscribe to input events
        InputHandler.OnConsoleOpen += OpenConsole;
        InputHandler.OnConsoleClose += CloseConsole;
        InputHandler.OnSubmit += SubmitTicket;
        InputHandler.OnNext += GetNextInput;
        InputHandler.OnPrevious += GetPrevInput;
        InputHandler.OnAutoComplete += AutoCompleteInput;

        _inputField.onValueChanged.AddListener(OnInputChanged);

        _console.OnConsoleOutput += OutputToConsole;

        AddBasicConsoleCommandsToRegistry();
    }

    private void OnDestroy()
    {
        // Unsubscribe from input events
        InputHandler.OnConsoleOpen -= OpenConsole;
        InputHandler.OnConsoleClose -= CloseConsole;
        InputHandler.OnSubmit -= SubmitTicket;
        InputHandler.OnNext -= GetNextInput;
        InputHandler.OnPrevious -= GetPrevInput;
        InputHandler.OnAutoComplete -= AutoCompleteInput;

        _inputField.onValueChanged.RemoveListener(OnInputChanged);

        _console.OnConsoleOutput -= OutputToConsole;
    }

    private void OpenConsole()
    {
        Show();

        // Focus cursor on the input field
        _inputField.Select();

        OnConsoleOpened?.Invoke();
    }

    private void CloseConsole()
    {
        Hide();

        OnConsoleClosed?.Invoke();
    }

    private void SubmitTicket()
    {
        string input = _inputField.text;

        if (input == "")
            return;

        // Display input on console output
        _console.CreateLogOutput(input);

        _console.SubmitTicket(input);

        // Reset input field
        _scrollRect.verticalNormalizedPosition = 0;
        _inputField.text = "";
        _inputField.ActivateInputField();
        _inputField.Select();
    }

    private void OnInputChanged(string text)
    {
        if (_suppressSuggestionUpdate)
            return;

        string suggestedCommand = _console.SuggestCommand(text);

        _currentSuggestion = suggestedCommand;

        UpdateSuggestionDisplay(_currentSuggestion);
    }

    private void UpdateSuggestionDisplay(string suggestedCommand)
    {
        if (_suggestionText == null)
            return;

        _suggestionText.text = suggestedCommand;
    }

    private void AutoCompleteInput()
    {
        if (_currentSuggestion == string.Empty)
            return;

        // Auto-complete the input field with the current suggestion
        _suppressSuggestionUpdate = true;      // Don't let this text change regenerate the suggestion list
        _inputField.text = _currentSuggestion + " ";
        _suppressSuggestionUpdate = false;

        // Move caret to the end of the input field
        _inputField.caretPosition = _inputField.text.Length;
        _inputField.stringPosition = _inputField.text.Length;

        UpdateSuggestionDisplay(string.Empty);      // Clear suggestion display
        _inputField.ActivateInputField();      // Keep focus in the field
    }

    private void GetNextInput()
    {
        _inputField.text = _console.GetNextInputInMemory();
    }

    private void GetPrevInput()
    {
        _inputField.text = _console.GetPrevInputInMemory();
    }

    private void PrintInputMemory()
    {
        string[] inputMemory = _console.GetInputMemory();

        OutputToConsole($"Input Memory: ({inputMemory.Length})");
        for (int i = 0; i < inputMemory.Length; i++)
        {
            OutputToConsole($"{i}. \"{inputMemory[i]}\"");
        }
    }

    private void OutputToConsole(string output)
    {
        _outputText.text += output + "\n";
    }

    private void ClearConsole()
    {
        // Clear output string
        _outputText.text = string.Empty;
    }

    private void AddBasicConsoleCommandsToRegistry()
    {
        // Help command - Output all registered commands to console
        Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "help",
            "Lists all available commands.",
            args =>
            {
                StringBuilder sb = new StringBuilder();

                sb.Append($"*** Console Commands ({Console.CommandRegistry.GetCommandCount()}) ***\n\n");

                foreach (var cmd in Console.CommandRegistry.GetAllCommands())
                {
                    sb.Append($"\t{cmd.CommandId} - \t{cmd.CommandDescription}\n\n");
                }

                OutputToConsole(sb.ToString());
            }));

        // Clear command - clear text from console interface
        Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "clear",
            "Clears the console output (if using a log window).",
            args =>
            {
                ClearConsole();
            }));

        // Print input memory command - Print memory of console
        Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "printinputmem",
            "Prints the input memory of the console.",
            args =>
            {
                PrintInputMemory();
                Debug.Log("Input Memory printed to console.");
            }, true));

        // Makes dev commands available to the console
        Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "devmode",
            "Enables developer commands in the console.",
            args =>
            {
                if (args.Length < 1)
                {
                    Debug.LogWarning("No argument given, please enter a passcode.");
                    return;
                }
                else if (args[0] is not string)
                {
                    Debug.LogWarning("Invalid argument given, please enter a valid string input.");
                    return;
                }

                string passcode = args[0].ToString();

                bool result = Console.CommandRegistry.ToggleDevCommands(passcode);
                if (result)
                    Debug.Log("Developer commands enabled.");
                else
                    Debug.Log("Developer commands disabled.");
            }));
    }
}
