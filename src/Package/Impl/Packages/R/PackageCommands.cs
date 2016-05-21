﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Design;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Components.InteractiveWorkflow.Commands;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.DataInspect.Commands;
using Microsoft.VisualStudio.R.Package.Documentation;
using Microsoft.VisualStudio.R.Package.Feedback;
using Microsoft.VisualStudio.R.Package.Help;
using Microsoft.VisualStudio.R.Package.History;
using Microsoft.VisualStudio.R.Package.Options.R.Tools;
using Microsoft.VisualStudio.R.Package.PackageManager;
using Microsoft.VisualStudio.R.Package.Plots.Commands;
using Microsoft.VisualStudio.R.Package.Plots.Definitions;
using Microsoft.VisualStudio.R.Package.Repl;
using Microsoft.VisualStudio.R.Package.Repl.Commands;
using Microsoft.VisualStudio.R.Package.Repl.Debugger;
using Microsoft.VisualStudio.R.Package.Repl.Shiny;
using Microsoft.VisualStudio.R.Package.Repl.Workspace;
using Microsoft.VisualStudio.R.Package.RPackages.Commands;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.R.Package.Windows;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.R.Packages.R {
    internal static class PackageCommands {
        public static IEnumerable<MenuCommand> GetCommands(ExportProvider exportProvider) {
            var appShell = VsAppShell.Current;
            var interactiveWorkflowProvider = exportProvider.GetExportedValue<IRInteractiveWorkflowProvider>();
            var interactiveWorkflowComponentContainerFactory = exportProvider.GetExportedValue<IInteractiveWindowComponentContainerFactory>();
            var interactiveWorkflow = interactiveWorkflowProvider.GetOrCreate();
            var projectServiceAccessor = exportProvider.GetExportedValue<IProjectServiceAccessor>();
            var plotHistoryProvider = exportProvider.GetExportedValue<IPlotHistoryProvider>();
            var plotHistory = plotHistoryProvider.GetPlotHistory(interactiveWorkflow.RSession);
            var textViewTracker = exportProvider.GetExportedValue<IActiveWpfTextViewTracker>();
            var replTracker = exportProvider.GetExportedValue<IActiveRInteractiveWindowTracker>();
            var debuggerModeTracker = exportProvider.GetExportedValue<IDebuggerModeTracker>();
            var contentTypeRegistryService = exportProvider.GetExportedValue<IContentTypeRegistryService>();

            return new List<MenuCommand> {
                new GoToOptionsCommand(),
                new GoToEditorOptionsCommand(),
                new ImportRSettingsCommand(),
                new SurveyNewsCommand(),

                new ReportIssueCommand(),
                new SendSmileCommand(),
                new SendFrownCommand(),

                new OpenDocumentationCommand(RGuidList.RCmdSetGuid, RPackageCommandId.icmdRtvsDocumentation, DocumentationUrls.RtvsDocumentation),
                new OpenDocumentationCommand(RGuidList.RCmdSetGuid, RPackageCommandId.icmdRtvsSamples, DocumentationUrls.RtvsSamples),
                new OpenDocumentationCommand(RGuidList.RCmdSetGuid, RPackageCommandId.icmdRDocsIntroToR, DocumentationUrls.CranIntro),
                new OpenDocumentationCommand(RGuidList.RCmdSetGuid, RPackageCommandId.icmdRDocsTaskViews, DocumentationUrls.CranViews),
                new OpenDocumentationCommand(RGuidList.RCmdSetGuid, RPackageCommandId.icmdRDocsDataImportExport, DocumentationUrls.CranData),
                new OpenDocumentationCommand(RGuidList.RCmdSetGuid, RPackageCommandId.icmdRDocsWritingRExtensions, DocumentationUrls.CranExtensions),
                new OpenDocumentationCommand(RGuidList.RCmdSetGuid, RPackageCommandId.icmdCheckForUpdates, DocumentationUrls.CheckForRtvsUpdates),
                new OpenDocumentationCommand(RGuidList.RCmdSetGuid, RPackageCommandId.icmdMicrosoftRProducts, DocumentationUrls.MicrosoftRProducts),

                new LoadWorkspaceCommand(appShell, interactiveWorkflow, projectServiceAccessor),
                new SaveWorkspaceCommand(appShell, interactiveWorkflow, projectServiceAccessor),

                new AttachDebuggerCommand(interactiveWorkflow),
                new AttachToRInteractiveCommand(interactiveWorkflow),
                new StopDebuggingCommand(interactiveWorkflow),
                new ContinueDebuggingCommand(interactiveWorkflow),
                new StepOverCommand(interactiveWorkflow),
                new StepOutCommand(interactiveWorkflow),
                new StepIntoCommand(interactiveWorkflow),

                new CommandAsyncToOleMenuCommandShim(
                    RGuidList.RCmdSetGuid, RPackageCommandId.icmdSourceRScript,
                    new SourceRScriptCommand(interactiveWorkflow, textViewTracker, false)),
                new CommandAsyncToOleMenuCommandShim(
                    RGuidList.RCmdSetGuid, RPackageCommandId.icmdSourceRScriptWithEcho,
                    new SourceRScriptCommand(interactiveWorkflow, textViewTracker, true)),

                new RunShinyAppCommand(interactiveWorkflow),
                new StopShinyAppCommand(interactiveWorkflow),

                new InterruptRCommand(interactiveWorkflow, debuggerModeTracker),
                new ResetReplCommand(interactiveWorkflow),

                new ImportDataSetTextFileCommand(appShell, interactiveWorkflow.RSession),
                new ImportDataSetUrlCommand(interactiveWorkflow.RSession),
                new DeleteAllVariablesCommand(interactiveWorkflow.RSession),

                new InstallPackagesCommand(),
                new CheckForPackageUpdatesCommand(),

                // Window commands
                new ShowPlotWindowsCommand(),
                new ShowRInteractiveWindowsCommand(interactiveWorkflowProvider, interactiveWorkflowComponentContainerFactory),
                new ShowVariableWindowCommand(),
                new ShowHelpWindowCommand(),
                new ShowHelpOnCurrentCommand(interactiveWorkflow, textViewTracker, replTracker),
                new ShowHistoryWindowCommand(),
                new GotoEditorWindowCommand(textViewTracker, contentTypeRegistryService),
                new GotoSolutionExplorerCommand(),
                new ShowPackageManagerWindowCommand(),

                // Plot commands
                new ExportPlotAsImageCommand(appShell, plotHistory),
                new ExportPlotAsPdfCommand(appShell, plotHistory),
                new CopyPlotAsBitmapCommand(plotHistory),
                new CopyPlotAsMetafileCommand(plotHistory),
                new HistoryNextPlotCommand(plotHistory),
                new HistoryPreviousPlotCommand(plotHistory),
                new ClearPlotsCommand(plotHistory),
                new RemovePlotCommand(plotHistory),
                new EndLocatorCommand(plotHistory),
            };
        }
    }
}
