﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.Design;
using System.Threading;

namespace Microsoft.Common.Core.Shell {
    /// <summary>
    /// Basic shell provides access to services such as 
    /// composition container, export provider, global VS IDE
    /// services and so on.
    /// </summary>
    public interface ICoreShell: ICompositionCatalog {
        /// <summary>
        /// Provides a way to execute action on UI thread while
        /// UI thread is waiting for the completion of the action.
        /// May be implemented using ThreadHelper in VS or via
        /// SynchronizationContext in all-managed application.
        /// 
        /// This can be blocking or non blocking dispatch, preferrably
        /// non blocking
        /// </summary>
        /// <param name="action">Action to execute</param>
        void DispatchOnUIThread(Action action);

        /// <summary>
        /// Provides access to the application main thread, so users can know if the task they are trying
        /// to execute is executing from the right thread.
        /// </summary>
        Thread MainThread { get; }

        /// <summary>
        /// Fires when host application enters idle state.
        /// </summary>
        event EventHandler<EventArgs> Idle;

        /// <summary>
        /// Fires when host application is terminating
        /// </summary>
        event EventHandler<EventArgs> Terminating;

        /// <summary>
        /// Displays error message in a host-specific UI
        /// </summary>
        void ShowErrorMessage(string message);

        /// <summary>
        /// Shows the context menu with the specified command ID at the specified location
        /// </summary>
        /// <param name="commandId"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        void ShowContextMenu(CommandID commandId, int x, int y);

        /// <summary>
        /// Displays message with specified buttons in a host-specific UI
        /// </summary>
        MessageButtons ShowMessage(string message, MessageButtons buttons);

        /// <summary>
        /// Returns host locale ID
        /// </summary>
        int LocaleId { get; }

        /// <summary>
        /// If the specified file is opened as a document, and it has unsaved changes, save those changes.
        /// </summary>
        /// <param name="fullPath">The full path to the document to be saved.</param>
        /// <returns> The path to which the file was saved. This is either the original path or a new path specified by the user.</returns>
        string SaveFileIfDirty(string fullPath);
    }
}
