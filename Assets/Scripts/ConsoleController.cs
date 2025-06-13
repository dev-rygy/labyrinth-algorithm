/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/04/2025
 * Last Modified:   06/04/2025 (Ryan)
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
        private Action<string[]> _command;

        public string CommandId { get { return _commandId; } }      // Unique command identifier
        public string CommandDescription { get { return _commandDescription; } }    // Description of command's purpose
        public Action<string[]> Command { get { return _command; } }        // Event that will execute from command

        public ConsoleCommand(string commandId, string commandDescription, Action<string[]> execute)
        {
            _commandId = commandId;
            _commandDescription = commandDescription;
            _command = execute;
        }
    }

    public class ConsoleController : MonoBehaviour
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

        public bool TryExecuteCommand(string input)
        {
            // parse command and execute
            return false;
        }
    }
}
