/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/04/2025
 * Last Modified:   06/12/2025 (Ryan)
 * Notes:           Defines a console command;
 *                  creates and manages commands
*/
using System;
using System.Collections.Generic;
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

        public string CommandId { get { return _commandId; } }      // Unique command identifier
        public string CommandDescription { get { return _commandDescription; } }    // Description of command's purpose
        public Action<string[]> Execute { get { return _execute; } }        // Event that will execute from command

        public ConsoleCommand(string commandId, string commandDescription, Action<string[]> execute)
        {
            _commandId = commandId;
            _commandDescription = commandDescription;
            _execute = execute;
        }
    }

    public class ConsoleCommandRegistry
    {
        // List to hold all commands
        private Dictionary<string, ConsoleCommand> _commands = new();

        /// <summary>
        /// Add a command to the registry; command list
        /// </summary>
        /// <param name="command">Command to add</param>
        public void RegisterCommand(ConsoleCommand command)
        {
            _commands.Add(command.CommandId, command);
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

            string commandName = splitString[0].ToLower();      // First part of string is command key
            string[] args = splitString.Length > 1 ? splitString[1..] : Array.Empty<string>();      // All rest are arguements

            // Try executing command
            if (_commands.TryGetValue(commandName, out var command))
            {
                try
                {
                    command.Execute.Invoke(args);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Console Registry Error: Error executing command '{commandName}': {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Console Registry Warning: Unknown command - {commandName}");
            }

            return false;
        }

        public IEnumerable<ConsoleCommand> GetAllCommands() => _commands.Values;
    }
}
