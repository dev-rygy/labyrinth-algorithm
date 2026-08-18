/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/17/2026
 * Last Modified:   08/17/2026 (Ryan)
 * Notes:           Intermediary class for console commands; 
 *                  handles registration, logging, and execution
*/
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RyansLibrary.Debugging
{
    public class Console
    {
        private static ConsoleCommandRegistry _commandRegistry;        // Console Command Registry
        public static ConsoleCommandRegistry CommandRegistry => _commandRegistry;

        public event Action<string> OnConsoleOutput;        // Event for console output

        // Log Type Colors
        public Color ErrorTextColor = Color.white;
        public Color AssertTextColor = Color.white;
        public Color WarningTextColor = Color.white;
        public Color LogTextColor = Color.white;
        public Color ExceptionTextColor = Color.white;
        public Color GeneralTextColor = Color.white;

        // Log Type Tags
        public string ErrorTextTag = "[ERROR]";
        public string AssertTextTag = "[ASSERT]";
        public string WarningTextTag = "[WARNING]";
        public string LogTextTag = "[LOG]";
        public string ExceptionTextTag = "[EXCEPTION]";

        // Input Memory Variables
        private string[] _inputMemory;
        private int _inputMemoryCapacity = 10;      // Holds prev commands
        private bool _toggleInputMemory = true;
        private int _inputMemoryIndex = 0;
        private int _currentInputMemCapacity = 0;

        // Command Suggestion Variables
        private int _maxSuggestions = 5;
        private List<string> _currentSuggestions = new();

        public Console(int inputMemoryCapacity = 10, int maxSuggestions = 5, bool enableDevCommands = false)
        {
            _commandRegistry = new ConsoleCommandRegistry();          // Init. registry
            _inputMemory = new string[_inputMemoryCapacity];        // Init. command memory
            _maxSuggestions = maxSuggestions;

            if (_inputMemoryCapacity <= 0)      // Incorrect input mem setting
                _toggleInputMemory = false;

            Application.logMessageReceived += CreateLogOutput;

            CommandRegistry.ToggleDevCommands(enableDevCommands);
        }

        ~Console()
        {
            Application.logMessageReceived -= CreateLogOutput;
            _commandRegistry = null;
        }

        public void AddInputToMemory(string input)
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

        public string GetNextInputInMemory()
        {
            string input = _inputMemory[_inputMemoryIndex];

            _inputMemoryIndex--;
            if (_inputMemoryIndex < 0)
                _inputMemoryIndex = 0;

            return input;
        }

        public string GetPrevInputInMemory()
        {
            string input = _inputMemory[_inputMemoryIndex];

            _inputMemoryIndex++;
            if (_inputMemoryIndex >= _currentInputMemCapacity)
                _inputMemoryIndex -= 1;

            return input;
        }

        public string[] GetInputMemory()
        {
            return _inputMemory;
        }

        public void ClearInputMemory()
        {
            _inputMemory = new string[_inputMemoryCapacity];
            _currentInputMemCapacity = 0;
            _inputMemoryIndex = 0;
        }

        public bool SubmitTicket(string input)
        {
            if (input == "")
                return false;

            // Add input to memory
            AddInputToMemory(input);
            _inputMemoryIndex = 0;

            bool success = CommandRegistry.TryExecuteCommand(input);

            return success;
        }

        // TODO: Make suggestions into a drop down and use arrow keys to switch between them; this means
        // return list of suggestions instead of just the first one
        public string SuggestCommand(string input)
        {
            // Split the input string to get the command part (first word)
            string commandPart = input.Split(' ')[0];
            if (string.IsNullOrWhiteSpace(commandPart))
                _currentSuggestions = new List<string>();
            else
                _currentSuggestions = CommandRegistry.GetSuggestions(commandPart, _maxSuggestions);

            // Return likely suggestion or nothing if no suggestions are available
            if (_currentSuggestions.Count == 0)
                return string.Empty;
            else
                return _currentSuggestions[0];
        }

        public void CreateLogOutput(string output, string stackTrace = "", LogType logType = LogType.Log)
        {
            StringBuilder sb = new StringBuilder();

            var typeColor = logType switch
            {
                LogType.Error => ErrorTextColor,
                LogType.Assert => AssertTextColor,
                LogType.Warning => WarningTextColor,
                LogType.Exception => ExceptionTextColor,
                _ => LogTextColor
            };

            var typeTag = logType switch
            {
                LogType.Error => ErrorTextTag,
                LogType.Assert => AssertTextTag,
                LogType.Warning => WarningTextTag,
                LogType.Exception => ExceptionTextTag,
                _ => LogTextTag
            };

            sb.Append("<color=#")
              .Append(ColorUtility.ToHtmlStringRGB(typeColor))
              .Append(">[")
              .Append(typeTag)
              .Append("]</color>");

            sb.Append(" ");

            sb.Append("<color=#")
              .Append(ColorUtility.ToHtmlStringRGB(GeneralTextColor))
              .Append(">")
              .Append(output)
              .Append("</color>");

            sb.Append("\n");

            OnConsoleOutput?.Invoke(sb.ToString());
        }
    }
}