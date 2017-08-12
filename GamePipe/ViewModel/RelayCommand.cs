﻿/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System;
using System.Diagnostics;
using System.Windows.Input;


/// <summary>
/// A command whose sole purpose is to 
/// relay its functionality to other
/// objects by invoking delegates. The
/// default return value for the CanExecute
/// method is 'true'.
/// </summary>
public class RelayCommand : ICommand
{
    #region "Fields"

    private readonly Action<object> _execute;

    private readonly Predicate<object> _canExecute;
    #endregion

    #region "Constructors"

    /// <summary>
    /// Creates a new command that can always execute.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    public RelayCommand(Action<object> execute)
        : this(execute, null)
    {
    }

    /// <summary>
    /// Creates a new command.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    {
        if (execute == null)
        {
            throw new ArgumentNullException("execute");
        }

        _execute = execute;
        _canExecute = canExecute;
    }

    #endregion

    #region "ICommand Members"

    [DebuggerStepThrough()]
    public bool CanExecute(object parameter)
    {
        return _canExecute == null ? true : _canExecute(parameter);
    }

    public event EventHandler CanExecuteChanged
    {
        add
        {
            CommandManager.RequerySuggested += value;
        }
        remove
        {
            CommandManager.RequerySuggested -= value;
        }

    }
    public void Execute(object parameter)
    {
        _execute(parameter);
    }

    #endregion
}
