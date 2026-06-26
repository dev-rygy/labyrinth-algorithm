/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/04/2025
 * Last Modified:   06/13/2025 (Ryan)
 * Notes:           Controls the user interface of the debug console
*/
using JetBrains.Annotations;
using NUnit.Framework;
using RyansLibrary.Console;
using RyansLibrary.Input;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class ConsoleUI : UIBehaviour
{
    public static ConsoleCommandRegistry CommandRegistry { get; private set; }        // Console Command Registry

    public static Action OnConsoleOpened;
    public static Action OnConsoleClosed;
    public static Action<string> OnNewConsoleOutput;        // Output to UI
    public static Action OnClearConsole;                    // Clear UI

    [Header("Console Settings")]
    [SerializeField] private bool _toggleInputMemory;       // Toggle input mem on and off
    [SerializeField] private int _inputMemoryCapacity;      // Holds prev commands

    [Header("Console References")]
    [SerializeField] private TMP_Text _outputText;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private ScrollRect _scrollRect;

    private string[] _inputMemory;
    private int _inputMemoryIndex = 0;
    private int _currentInputMemCapacity = 0;

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

        OnNewConsoleOutput += OutputToConsole;
        OnClearConsole += ClearConsole;

        AddBasicConsoleCommandsToRegistry();
    }

    private void OnDisable()
    {
        InputHandler.OnConsoleOpen -= OpenConsole;
        InputHandler.OnConsoleClose -= CloseConsole;
        InputHandler.OnSubmit -= SubmitTicket;
        InputHandler.OnNext -= GetNextInput;
        InputHandler.OnPrevious -= GetPrevInput;

        OnNewConsoleOutput -= OutputToConsole;
        OnClearConsole -= ClearConsole;
    }

    private void OpenConsole()
    {
        Show();

        // Focus cursor on the input field
        _inputField.Select();

        OnConsoleOpened.Invoke();
    }

    private void CloseConsole()
    {
        Hide();

        OnConsoleClosed.Invoke();
    }

    private void SubmitTicket()
    {
        string input = _inputField.text;

        if (input == "")
            return;

        // Display input on console output
        OutputToConsole(input);

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

    private void OutputToConsole(string output)
    {
        _outputText.text += output + "\n";
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
                foreach (var cmd in CommandRegistry.GetAllCommands())
                {
                    OutputToConsole($"{cmd.CommandId}: {cmd.CommandDescription}");
                }
                Debug.Log($"Console: Help Command");
            }));

        // Clear command - clear text from console interface
        CommandRegistry.RegisterCommand(new ConsoleCommand(
            "clear",
            "Clears the console output (if using a log window).",
            args =>
            {
                ClearConsole();
                Debug.Log("Console: Clear Command");
            }));

        // Print input memory command - Print memory of console
        CommandRegistry.RegisterCommand(new ConsoleCommand(
            "printinputmem",
            "Prints the input memory of the console.",
            args =>
            {
                PrintInputMemory();
                Debug.Log("Console: Input Memory Print Command");
            }));
    }
}
