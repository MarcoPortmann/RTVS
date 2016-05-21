﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.R.Components.Plots;
using Microsoft.VisualStudio.R.Package.Commands;
using Microsoft.VisualStudio.R.Package.Plots.Definitions;
using Microsoft.VisualStudio.R.Package.Shell;

namespace Microsoft.VisualStudio.R.Package.Plots.Commands {
    internal sealed class ExportPlotAsPdfCommand : PlotWindowCommand {
        private readonly IApplicationShell _appShell;

        public ExportPlotAsPdfCommand(IApplicationShell appShell, IPlotHistory plotHistory) :
            base(plotHistory, RPackageCommandId.icmdExportPlotAsPdf) {
            _appShell = appShell;
        }

        protected override void SetStatus() {
            Enabled = PlotHistory.ActivePlotIndex >= 0 && !IsInLocatorMode;
        }

        protected override void Handle() {
            string destinationFilePath = _appShell.BrowseForFileSave(IntPtr.Zero, Resources.PlotExportAsPdfFilter, null, Resources.ExportPlotAsPdfDialogTitle);
            if (!string.IsNullOrEmpty(destinationFilePath)) {
                PlotHistory.PlotContentProvider.ExportAsPdf(destinationFilePath);
            }
        }
    }
}
