﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Html.Core.Tree {
    public interface IHtmlScriptOrStyleTagNamesService {
        IReadOnlyList<string> GetScriptTagNames();
        IReadOnlyList<string> GetStyleTagNames();
    }
}
