// Copyright (c) TestsSharedLibraryForCodeParsers Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the solution root for license information.

using JetBrains.Annotations;
using TestsSharedLibrary.TestSimulation;

namespace TestsSharedLibraryForCodeParsers.CodeGeneration;

public interface ICodeGeneratorParametersProvider
{
    [NotNull] IRandomNumberGenerator RandomNumberGenerator { get; }
    int MaxNumberOfAdditionalWhitespaces { get; }
    int MaxNumberOfAdditionalComments { get; }
    int MaxLengthOfComment { get; }
    [CanBeNull] CommentMarkersData CommentMarkersData { get; }
    bool SimulateNiceCode { get; }
    bool IsLanguageCaseSensitive { get; }
}