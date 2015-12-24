﻿using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Common.Core.IO;
using Microsoft.R.Host.Client;
using Microsoft.VisualStudio.R.Package.History;
using Microsoft.VisualStudio.R.Package.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.R.Package.Test.Repl {
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ExportsTest {
        [TestMethod]
        [TestCategory("Repl")]
        public void FileSystem_ExportTest() {
            Lazy<IFileSystem> lazy = VsAppShell.Current.ExportProvider.GetExport<IFileSystem>();
            Assert.IsNotNull(lazy.Value);
        }

        [TestMethod]
        [TestCategory("Repl")]
        public void RSessionProvider_ExportTest() {
            Lazy<IRSessionProvider> lazy = VsAppShell.Current.ExportProvider.GetExport<IRSessionProvider>();
            Assert.IsNotNull(lazy.Value);
        }

        [TestMethod]
        [TestCategory("Repl")]
        public void ReplHistoryProvider_ExportTest() {
            Lazy<IRHistoryProvider> provider = VsAppShell.Current.ExportProvider.GetExport<IRHistoryProvider>();
            Assert.IsNotNull(provider.Value);
        }
    }
}