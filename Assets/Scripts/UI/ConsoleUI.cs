/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/04/2025
 * Last Modified:   07/17/2026 (Ryan)
 * Notes:           Controls the user interface of the debug console
*/
using RyansLibrary.Console;
using RyansLibrary.Input;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleUI : UIBehaviour
{
    public static ConsoleCommandRegistry CommandRegistry { get; private set; }        // Console Command Registry

    public static Action OnConsoleOpened;
    public static Action OnConsoleClosed;
    public static Action<string, LogType> OnNewConsoleOutput;        // Output to UI
    public static Action OnClearConsole;                             // Clear UI

    [Header("Console Settings")]
    [SerializeField] private bool _toggleInputMemory;       // Toggle input mem on and off
    [SerializeField] private int _inputMemoryCapacity;      // Holds prev commands

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

    [Header("Component References")]
    [SerializeField] private TMP_Text _outputText;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private ScrollRect _scrollRect;

    private string[] _inputMemory;
    private int _inputMemoryIndex = 0;
    private int _currentInputMemCapacity = 0;

    private List<string> _currentSuggestions = new();
    private int _suggestionIndex = 0;
    private bool _suppressSuggestionUpdate = false;

    private void OnEnable()
    {
        CommandRegistry = new ConsoleCommandRegistry();          // Init. registry
        _inputMemory = new string[_inputMemoryCapacity];        // Init. command memory        

        if (_inputMemoryCapacity <= 0)      // Incorrect input mem setting
            _toggleInputMemory = false;

        InputHandler.OnConsoleOpen += OpenConsole;
        InputHandler.OnConsoleClose += CloseConsole;
        InputHandler.OnSubmit += SubmitTicket;
        InputHandler.OnNext += GetNextInput;
        InputHandler.OnPrevious += GetPrevInput;
        InputHandler.OnAutoComplete += AutoCompleteInput;

        _inputField.onValueChanged.AddListener(OnInputChanged);

        OnNewConsoleOutput += OutputToConsole;
        OnClearConsole += ClearConsole;

        Application.logMessageReceived += HandleUnityLog;

        AddBasicConsoleCommandsToRegistry();
    }

    private void OnDisable()
    {
        InputHandler.OnConsoleOpen -= OpenConsole;
        InputHandler.OnConsoleClose -= CloseConsole;
        InputHandler.OnSubmit -= SubmitTicket;
        InputHandler.OnNext -= GetNextInput;
        InputHandler.OnPrevious -= GetPrevInput;
        InputHandler.OnAutoComplete -= AutoCompleteInput;

        _inputField.onValueChanged.RemoveListener(OnInputChanged);

        Application.logMessageReceived -= HandleUnityLog;

        OnNewConsoleOutput -= OutputToConsole;
        OnClearConsole -= ClearConsole;
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
        OutputToConsole(input, LogType.Log);

        // Add input to memory
        AddInputToMemory(input);
        _inputMemoryIndex = 0;

        bool success = CommandRegistry.TryExecuteCommand(input);

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

        _suggestionIndex = 0;

        string commandPart = text.Split(' ')[0];
        _currentSuggestions = string.IsNullOrWhiteSpace(commandPart) ? new List<string>()
            : CommandRegistry.GetSuggestions(commandPart, _maxSuggestions);

        UpdateSuggestionDisplay();
    }

    private void UpdateSuggestionDisplay()
    {
        if (_suggestionText == null)
            return;

        if (_currentSuggestions.Count == 0)
        {
            _suggestionText.text = "";
            return;
        }

        StringBuilder sb = new StringBuilder();

        // TODO: Make suggestions into a drop down and use arrow keys to switch between them
        sb.Append(_currentSuggestions[0]);

        _suggestionText.text = sb.ToString();
    }

    private void AutoCompleteInput()
    {
        if (_currentSuggestions.Count == 0)
            return;

        string chosen = _currentSuggestions[_suggestionIndex];

        _suppressSuggestionUpdate = true;      // Don't let this text change regenerate the suggestion list
        _inputField.text = chosen + " ";
        _suppressSuggestionUpdate = false;

        _inputField.caretPosition = _inputField.text.Length;
        _inputField.stringPosition = _inputField.text.Length;

        _suggestionIndex = _currentSuggestions.Count > 1 ? (_suggestionIndex + 1) % _currentSuggestions.Count : 0;

        UpdateSuggestionDisplay();
        _inputField.ActivateInputField();      // Keep focus in the field
    }

    private void AddInputToMemory(string input)
    {
        if (!_toggleInputMemory)
            return;

        // Shift all memory cells forward by 1
        for (int i = _inputMemoryCapacity - 1; i > 0; i--)
        {
            // Input of right elem is equal to left elem
            _inputMemory[i] = _inputMemory[i - 1];
        }

        // add new input to front of array
        _inputMemory[0] = input;

        if (_currentInputMemCapacity < _inputMemoryCapacity)
            _currentInputMemCapacity++;
    }

    private void GetNextInput()
    {
        _inputField.text = _inputMemory[_inputMemoryIndex];

        _inputMemoryIndex--;
        if (_inputMemoryIndex < 0)
            _inputMemoryIndex = 0;
    }

    private void GetPrevInput()
    {
        _inputField.text = _inputMemory[_inputMemoryIndex];

        _inputMemoryIndex++;
        if (_inputMemoryIndex >= _currentInputMemCapacity)
            _inputMemoryIndex -= 1;
    }

    private void PrintInputMemory()
    {
        OutputToConsole($"Input Memory: ({_currentInputMemCapacity})");
        for (int i = 0; i < _inputMemoryCapacity; i++)
        {
            OutputToConsole($"{i}. \"{_inputMemory[i]}\"");
        }
    }

    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        OutputToConsole(logString, type);
    }

    private void OutputToConsole(string output, LogType logType = LogType.Log)
    {
        StringBuilder sb = new StringBuilder();

        var typeColor = logType switch
        {
            LogType.Error => _errorTextColor,
            LogType.Assert => _assertTextColor,
            LogType.Warning => _warningTextColor,
            LogType.Exception => _exceptionTextColor,
            _ => _logTextColor
        };

        var typeTag = logType switch
        {
            LogType.Error => _errorTextTag,
            LogType.Assert => _assertTextTag,
            LogType.Warning => _warningTextTag,
            LogType.Exception => _exceptionTextTag,
            _ => _logTextTag
        };

        sb.Append("<color=#")
          .Append(ColorUtility.ToHtmlStringRGB(typeColor))
          .Append(">[")
          .Append(typeTag)
          .Append("]</color>");

        sb.Append(" ");

        sb.Append("<color=#")
          .Append(ColorUtility.ToHtmlStringRGB(_logTextColor))
          .Append(">")
          .Append(output)
          .Append("</color>");

        sb.Append("\n");

        _outputText.text += sb.ToString();
    }

    private void ClearConsole()
    {
        // Clear output string
        _outputText.text = String.Empty;
    }

    private void AddBasicConsoleCommandsToRegistry()
    {
        // Help command - Output all registered commands to console
        CommandRegistry.RegisterCommand(new ConsoleCommand(
            "help",
            "Lists all available commands.",
            args =>
            {
                StringBuilder sb = new StringBuilder();

                sb.Append($"*** Console Commands ({CommandRegistry.GetCommandCount()}) ***\n");

                foreach (var cmd in CommandRegistry.GetAllCommands())
                {
                    sb.Append($"\t{cmd.CommandId} - \t{cmd.CommandDescription}\n\n");
                }

                OutputToConsole(sb.ToString());
                Debug.Log("Console commands listed.");
            }));

        // Clear command - clear text from console interface
        CommandRegistry.RegisterCommand(new ConsoleCommand(
            "clear",
            "Clears the console output (if using a log window).",
            args =>
            {
                ClearConsole();
                Debug.Log("Console cleared.");
            }));

        // Print input memory command - Print memory of console
        CommandRegistry.RegisterCommand(new ConsoleCommand(
            "printinputmem",
            "Prints the input memory of the console.",
            args =>
            {
                PrintInputMemory();
                Debug.Log("Input Memory printed to console.");
            }));
    }
}
