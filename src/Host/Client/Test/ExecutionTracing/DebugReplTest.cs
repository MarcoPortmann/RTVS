﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Common.Core.Test.Utility;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Session;
using Microsoft.R.Host.Client.Test;
using Microsoft.R.Host.Client.Test.Script;
using Microsoft.UnitTests.Core.XUnit;
using Xunit;

namespace Microsoft.R.ExecutionTracing.Test {
    [ExcludeFromCodeCoverage]
    public class DebugReplTest : IAsyncLifetime {
        private readonly MethodInfo _testMethod;
        private readonly IRSessionProvider _sessionProvider;
        private readonly IRSession _session;

        public DebugReplTest(TestMethodFixture testMethod) {
            _testMethod = testMethod.MethodInfo;
            _sessionProvider = new RSessionProvider();
            _session = _sessionProvider.GetOrCreate(Guid.NewGuid());
        }

        public async Task InitializeAsync() {
            await _session.StartHostAsync(new RHostStartupInfo {
                Name = _testMethod.Name,
                RBasePath = RUtilities.FindExistingRBasePath()
            }, new RHostClientTestApp(), 50000);
        }

        public async Task DisposeAsync() {
            await _session.StopHostAsync();
            _sessionProvider.Dispose();
        }

        [Test]
        [Category.R.ExecutionTracing]
        public async Task InteractDuringBrowse() {
            var tracer = await _session.TraceExecutionAsync();
            using (var sf = new SourceFile("x <- 'old'; browser()")) {
                var browse = new TaskCompletionSource<bool>();
                tracer.Browse += (s, e) => {
                    browse.TrySetResult(true);
                };

                await sf.Source(_session);
                await browse.Task;

                using (var inter = await _session.BeginInteractionAsync()) {
                    await inter.RespondAsync("x <- 'new'\n");
                }

                string x;
                using (var eval = await _session.BeginEvaluationAsync()) {
                    x = await eval.EvaluateAsync<string>("x", REvaluationKind.Normal);
                }

                x.Should().Be("new");
            }
        }
    }
}
