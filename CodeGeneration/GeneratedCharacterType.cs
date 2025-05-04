// Copyright (c) TestsSharedLibraryForCodeParsers Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the solution root for license information.

using System;

namespace TestsSharedLibraryForCodeParsers.CodeGeneration;

[Flags]
public enum GeneratedCharacterType
{
    Letter,
    Number,
    SpecialOperatorCharacter,
    SpecialNonOperatorCharacter,
    Apostrophe,
    Underscore,
    Dot,
    NonLatinCharacter
}