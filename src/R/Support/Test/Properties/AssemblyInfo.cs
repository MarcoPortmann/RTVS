﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UnitTests.Core.XUnit;
using Microsoft.UnitTests.References;

[assembly: TestFrameworkOverride]
#if VS14
[assembly: Dev14AssemblyLoader]
#endif
#if VS15
[assembly: Dev15AssemblyLoader]
#endif

