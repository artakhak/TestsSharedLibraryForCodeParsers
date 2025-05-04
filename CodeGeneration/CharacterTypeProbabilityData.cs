// Copyright (c) TestsSharedLibraryForCodeParsers Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the solution root for license information.

namespace TestsSharedLibraryForCodeParsers.CodeGeneration;

public class CharacterTypeProbabilityData
{
    /// <summary>
    /// Character generation data.
    /// </summary>
    /// <param name="generatedCharacterType">Character type</param>
    /// <param name="probability">Number between 0 and 100</param>
    public CharacterTypeProbabilityData(GeneratedCharacterType generatedCharacterType, int probability)
    {
        GeneratedCharacterType = generatedCharacterType;
        Probability = probability;
    }

    public GeneratedCharacterType GeneratedCharacterType { get; }
    public int Probability { get; }
}