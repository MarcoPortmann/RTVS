﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Common.Core.Diagnostics;
using Microsoft.R.Components.InteractiveWorkflow;
using Microsoft.R.Components.PackageManager.Model;
using Microsoft.R.Components.Settings;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Session;
using Newtonsoft.Json.Linq;

namespace Microsoft.R.Components.PackageManager.Implementation {
    internal class RPackageManager : IRPackageManager {
        private readonly IRInteractiveWorkflow _interactiveWorkflow;
        private readonly Action _dispose;
        private readonly SessionPool _sessionPool;

        public IRPackageManagerVisualComponent VisualComponent { get; private set; }

        public RPackageManager(IRSessionProvider sessionProvider, IRSettings settings, IRInteractiveWorkflow interactiveWorkflow, Action dispose) {
            _interactiveWorkflow = interactiveWorkflow;
            _dispose = dispose;
            _sessionPool = new SessionPool(sessionProvider, settings);
        }

        public IRPackageManagerVisualComponent GetOrCreateVisualComponent(IRPackageManagerVisualComponentContainerFactory visualComponentContainerFactory, int instanceId = 0) {
            if (VisualComponent != null) {
                return VisualComponent;
            }

            VisualComponent = visualComponentContainerFactory.GetOrCreate(this, _interactiveWorkflow.RSession, instanceId).Component;
            return VisualComponent;
        }

        public async Task<IReadOnlyList<RPackage>> GetInstalledPackagesAsync() {
            return await GetPackagesAsync(async (eval) => await eval.InstalledPackagesAsync());
        }

        public async Task<IReadOnlyList<RPackage>> GetAvailablePackagesAsync() {
            return await GetPackagesAsync(async (eval) => await eval.AvailablePackagesAsync());
        }

        public async Task InstallPackageAsync(string name, string libraryPath) {
            if (!_interactiveWorkflow.RSession.IsHostRunning) {
                throw new RPackageManagerException(Resources.PackageManager_EvalSessionNotAvailable);
            }

            try {
                using (var request = await _interactiveWorkflow.RSession.BeginInteractionAsync()) {
                    if (string.IsNullOrEmpty(libraryPath)) {
                        await request.InstallPackageAsync(name);
                    } else {
                        await request.InstallPackageAsync(name, libraryPath);
                    }
                }
            } catch (MessageTransportException ex) {
                throw new RPackageManagerException(Resources.PackageManager_TransportError, ex);
            }
        }

        public async Task UninstallPackageAsync(string name, string libraryPath) {
            if (!_interactiveWorkflow.RSession.IsHostRunning) {
                throw new RPackageManagerException(Resources.PackageManager_EvalSessionNotAvailable);
            }

            try {
                using (var request = await _interactiveWorkflow.RSession.BeginInteractionAsync()) {
                    if (string.IsNullOrEmpty(libraryPath)) {
                        await request.UninstallPackageAsync(name);
                    } else {
                        await request.UninstallPackageAsync(name, libraryPath);
                    }
                }
            } catch (MessageTransportException ex) {
                throw new RPackageManagerException(Resources.PackageManager_TransportError, ex);
            }
        }

        public async Task LoadPackageAsync(string name, string libraryPath) {
            if (!_interactiveWorkflow.RSession.IsHostRunning) {
                throw new RPackageManagerException(Resources.PackageManager_EvalSessionNotAvailable);
            }

            try {
                using (var request = await _interactiveWorkflow.RSession.BeginInteractionAsync()) {
                    if (string.IsNullOrEmpty(libraryPath)) {
                        await request.LoadPackageAsync(name);
                    } else {
                        await request.LoadPackageAsync(name, libraryPath);
                    }
                }
            } catch (MessageTransportException ex) {
                throw new RPackageManagerException(Resources.PackageManager_TransportError, ex);
            }
        }

        public async Task UnloadPackageAsync(string name) {
            if (!_interactiveWorkflow.RSession.IsHostRunning) {
                throw new RPackageManagerException(Resources.PackageManager_EvalSessionNotAvailable);
            }

            try {
                using (var request = await _interactiveWorkflow.RSession.BeginInteractionAsync()) {
                    await request.UnloadPackageAsync(name);
                }
            } catch (MessageTransportException ex) {
                throw new RPackageManagerException(Resources.PackageManager_TransportError, ex);
            }
        }

        public async Task<string[]> GetLoadedPackagesAsync() {
            if (!_interactiveWorkflow.RSession.IsHostRunning) {
                throw new RPackageManagerException(Resources.PackageManager_EvalSessionNotAvailable);
            }

            try {
                var result = await WrapRException(_interactiveWorkflow.RSession.LoadedPackagesAsync());
                return result.Select(p => (string)((JValue)p).Value).ToArray();
            } catch (MessageTransportException ex) {
                throw new RPackageManagerException(Resources.PackageManager_TransportError, ex);
            }
        }

        public async Task<string[]> GetLibraryPathsAsync() {
            if (!_interactiveWorkflow.RSession.IsHostRunning) {
                throw new RPackageManagerException(Resources.PackageManager_EvalSessionNotAvailable);
            }

            try {
                var result = await WrapRException(_interactiveWorkflow.RSession.LibraryPathsAsync());
                return result.Select(p => (string)((JValue)p).Value).ToArray();
            } catch (MessageTransportException ex) {
                throw new RPackageManagerException(Resources.PackageManager_TransportError, ex);
            }
        }

        public PackageLockState GetPackageLockState(string name, string libraryPath) {
            string dllPath = GetPackageDllPath(name, libraryPath);
            if (!string.IsNullOrEmpty(dllPath)) {
                var processes = RestartManager.GetProcessesUsingFiles(new string[] { dllPath }).ToArray();
                if (processes != null) {
                    if (processes.Length == 1 && processes[0].Id == _interactiveWorkflow.RSession.ProcessId) {
                        return PackageLockState.LockedByRSession;
                    }

                    if (processes.Length > 0) {
                        return PackageLockState.LockedByOther;
                    }
                }
            }

            return PackageLockState.Unlocked;
        }

        private string GetPackageDllPath(string name, string libraryPath) {
            string pkgFolder = Path.Combine(libraryPath.Replace("/", "\\"), name);
            if (Directory.Exists(pkgFolder)) {
                string dllPath = Path.Combine(pkgFolder, "libs", "x64", name + ".dll");
                if (File.Exists(dllPath)) {
                    return dllPath;
                }
            }
            return null;
        }

        private async Task<IReadOnlyList<RPackage>> GetPackagesAsync(Func<IRExpressionEvaluator, Task<JArray>> queryFunc) {
            // Fetching of installed and available packages is done in a
            // separate package query session to avoid freezing the REPL.
            try {
                using (var sessionToken = await _sessionPool.GetSession()) {
                    // Get the repos and libpaths from the REPL session and set them
                    // in the package query session
                    await EvalRepositoriesAsync(sessionToken.Session, await DeparseRepositoriesAsync());
                    await EvalLibrariesAsync(sessionToken.Session, await DeparseLibrariesAsync());

                    var result = await WrapRException(queryFunc(sessionToken.Session));
                    return result.Select(p => p.ToObject<RPackage>()).ToList().AsReadOnly();
                }
            } catch (MessageTransportException ex) {
                throw new RPackageManagerException(Resources.PackageManager_TransportError, ex);
            }
        }

        private async Task EvalRepositoriesAsync(IRSession session, string deparsedRepositories) {
            var code = string.Format("options(repos=eval(parse(text={0})))", deparsedRepositories.ToRStringLiteral());
            using (var eval = await session.BeginEvaluationAsync()) {
                await WrapRException(eval.ExecuteAsync(code));
            }
        }

        private async Task EvalLibrariesAsync(IRSession session, string deparsedLibraries) {
            var code = string.Format(".libPaths(eval(parse(text={0})))", deparsedLibraries.ToRStringLiteral());
            using (var eval = await session.BeginEvaluationAsync()) {
                await WrapRException(eval.ExecuteAsync(code));
            }
        }

        private async Task<string> DeparseRepositoriesAsync() {
            return await WrapRException(_interactiveWorkflow.RSession.EvaluateAsync<string>("rtvs:::deparse_str(getOption('repos'))", REvaluationKind.Normal));
        }

        private async Task<string> DeparseLibrariesAsync() {
            return await WrapRException(_interactiveWorkflow.RSession.EvaluateAsync<string>("rtvs:::deparse_str(.libPaths())", REvaluationKind.Normal));
        }

        public void Dispose() {
            VisualComponent.Dispose();
            _sessionPool.Dispose();
            _dispose();
        }

        private async Task WrapRException(Task task) {
            try {
                await task;
            } catch (RException ex) {
                throw new RPackageManagerException(string.Format(CultureInfo.InvariantCulture, Resources.PackageManager_EvalError, ex.Message), ex);
            }
        }

        private async Task<T> WrapRException<T>(Task<T> task) {
            try {
                return await task;
            } catch (RException ex) {
                throw new RPackageManagerException(string.Format(CultureInfo.InvariantCulture, Resources.PackageManager_EvalError, ex.Message), ex);
            }
        }
    }
}
