/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/04/2025
 * Last Modified:   06/12/2025 (Ryan)
 * Notes:           Defines a console command;
 *                  creates and manages commands
*/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RyansLibrary.Console
{
    /// <summary>
    /// Class that holds the format and function of a command
    /// </summary>
    public class ConsoleCommand
    {
        private string _commandId;
        private string _commandDescription;
        private Action<string[]> _execute;
        private bool _isDevCommand;

        public string CommandId { get { return _commandId; } }      // Unique command identifier
        public string CommandDescription { get { return _commandDescription; } }    // Description of command's purpose
        public Action<string[]> Execute { get { return _execute; } }        // Event that will execute from command
        public bool IsDevCommand { get { return _isDevCommand; } }

        public ConsoleCommand(string commandId, string commandDescription, Action<string[]> execute, bool isDevCommand = false)
        {
            _commandId = commandId;
            _commandDescription = commandDescription;
            _execute = execute;
            _isDevCommand = isDevCommand;
        }
    }

    public class ConsoleCommandRegistry
    {
        private const string DEV_MODE_PASSCODE = "notpasscode";

        // List to hold all commands
        private Dictionary<string, ConsoleCommand> _commands = new();
        private bool _devCommandsVisible = false;

        public bool DevCommandsVisable { get { return _devCommandsVisible; } }

        /// <summary>
        /// Add a command to the registry; command list
        /// </summary>
        /// <param name="command">Command to add</param>
        public void RegisterCommand(ConsoleCommand command)
        {
            if (_commands.ContainsKey(command.CommandId.ToLower()))     // Prevent double command registry
            {
                Debug.LogWarning($"Command '{command.CommandId}' is already registered.");
                return;
            }

            // Add command to registry
            _commands.Add(command.CommandId, command);
        }

        public void UnregisterCommand(string commandId)
        {
            if (!_commands.ContainsKey(commandId.ToLower()))     // Prevent unregistering a command that doesn't exist
            {
                // Debug.LogWarning($"Command '{commandId}' is not registered.");
                return;
            }
            // Remove command from registry
            _commands.Remove(commandId.ToLower());
        }

        /// <summary>
        /// Try to execute a command from a string input,
        /// commands will execute the function they are linked to.
        /// </summary>
        /// <param name="input">Text/string input</param>
        /// <returns>Success/failure of execution</returns>
        public bool TryExecuteCommand(string input)
        {
            // Split the string into individual parts
            var splitString = input.Split(' ');

            if (splitString.Length == 0)
                return false;

            string commandName = splitString[0].ToLower();      // First part of string is command keyword
            string[] args = splitString.Length > 1 ? splitString[1..] : Array.Empty<string>();      // All rest are arguments

            // Search command registry for command with the name specified
            if (_commands.TryGetValue(commandName, out var command)
                && (_devCommandsVisible || !command.IsDevCommand))
            {
                try         // Attempt to execute the command
                {
                    command.Execute.Invoke(args);
                    return true;
                }
                catch (Exception e)     // Error executing command
                {
                    Debug.LogError($"Error executing command '{commandName}': {e.Message}.");
                    return false;
                }
            }
            else    // Command keyword not recognized
            {
                Debug.LogWarning($"Unknown command - {commandName}.");
            }

            return false;
        }

        public List<string> GetSuggestions(string prefix, int maxResults = 5)
        {
            List<string> results = new();

            if (string.IsNullOrEmpty(prefix))
                return results;

            foreach (var kvp in _commands)
            {
                if (!_devCommandsVisible && kvp.Value.IsDevCommand)
                    continue;

                if (kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(kvp.Key);
                    if (results.Count >= maxResults)
                        break;
                }
            }

            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        // Parse all commands
        public IEnumerable<ConsoleCommand> GetAllCommands() => _devCommandsVisible ? _commands.Values : _commands.Values.Where(c => !c.IsDevCommand);

        public int GetCommandCount()
        {
            return GetAllCommands().Count();
        }

        public bool ToggleDevCommands(string passcode)
        {
            if (passcode != DEV_MODE_PASSCODE) // Replace with your actual passcode
            {
                Debug.LogWarning("Incorrect passcode. Developer commands remain hidden.");
                return _devCommandsVisible;
            }

            _devCommandsVisible = !_devCommandsVisible;
            return _devCommandsVisible;
        }

        public void ToggleDevCommands(bool toggle)
        {
            _devCommandsVisible = toggle;
        }
    }
}
