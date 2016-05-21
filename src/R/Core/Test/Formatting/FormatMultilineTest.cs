﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.R.Core.Formatting;
using Microsoft.UnitTests.Core.XUnit;
using Xunit;

namespace Microsoft.R.Core.Test.Formatting {
    [ExcludeFromCodeCoverage]
    [Category.R.Formatting]
    public class FormatMultilineTest {
        [CompositeTest]
        [InlineData("x %>% y%>%\n   z%>%a", "x %>% y %>%\n   z %>% a")]
        [InlineData("((x %>% y)\n   %>%z%>%a)", "((x %>% y)\n   %>% z %>% a)")]
        public void Multiline(string original, string expected) {
            RFormatter f = new RFormatter();
            string actual = f.Format(original);
            actual.Should().Be(expected);
        }
    }
}
