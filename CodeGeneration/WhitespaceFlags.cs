// Copyright (c) TestsSharedLibraryForCodeParsers Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the solution root for license information.

using System;

namespace TestsSharedLibraryForCodeParsers.CodeGeneration;

[Flags]
public enum WhitespaceFlags
{
    Space = 1,
    Tab = 2,
    NewLine = 4,
    AnyWhitespace = Space | Tab | NewLine
}