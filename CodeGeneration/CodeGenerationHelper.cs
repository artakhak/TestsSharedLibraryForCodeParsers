// Copyright (c) TestsSharedLibraryForCodeParsers Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the solution root for license information.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniversalExpressionParser;

namespace TestsSharedLibraryForCodeParsers.CodeGeneration;

public delegate bool IsValidCharacterInGeneratedText([NotNull] StringBuilder currentlyGeneratedText, char charToAppend);

public class CodeGenerationHelper
{
    [NotNull]
    public static IReadOnlyList<char> ApostropheCharacters = new List<char> { '\'', '"', '`' };
        
    [NotNull]
    public static IReadOnlyList<char> SpecialOperatorCharacters { get; } = new List<char>(SpecialCharactersCacheThreadStaticContext.Context.SpecialOperatorCharacters);

    [NotNull]
    public static IReadOnlyList<char> SpecialNonOperatorCharacters { get; } = new List<char>(
        SpecialCharactersCacheThreadStaticContext.Context.SpecialCharacters.Where(x => 
            !(SpecialCharactersCacheThreadStaticContext.Context.IsSpecialOperatorCharacter(x) || CodeGenerationHelper.ApostropheCharacters.Contains(x))));

    [NotNull]
    public static readonly List<char> NonLatinCharacters = new List<char>(100);

    [NotNull] private readonly ICodeGeneratorParametersProvider _parametersProvider;

    static CodeGenerationHelper()
    {
        var minCharCode = (Int16.MaxValue << 1) + 1000;
        ReGenerateNonLatinCharacters((minCharCode, 100));
    }

    /// <summary>
    /// Regenerates the in-latin characters used in generated text for comments and texts.
    /// </summary>
    /// <param name="characterCodeRanges">Character ranges used for generation of non-latin characters. Examples: new {(1000, 1100), (1300, 1500)}
    /// The startCharacterCode values should be greater or equal to 15000.
    /// </param>
    public static void ReGenerateNonLatinCharacters(params (int startCharacterCode, int numberOfCharaceters)[] characterCodeRanges)
    {
        NonLatinCharacters.Clear();

        foreach (var charactersRange in characterCodeRanges)
        {
            if (charactersRange.startCharacterCode < 15000)
                throw new ArgumentOutOfRangeException(nameof(characterCodeRanges),
                    $"The value of minCharacterCode={charactersRange.startCharacterCode} should be greater ot equal to 15000.");

            if (charactersRange.numberOfCharaceters <= 0)
                throw new ArgumentOutOfRangeException(nameof(characterCodeRanges),
                    $"The value of numberOfCharaceters={charactersRange.numberOfCharaceters} should be positive");
                
            for (int currentCharacterCode = charactersRange.startCharacterCode; currentCharacterCode < charactersRange.startCharacterCode + charactersRange.numberOfCharaceters; ++currentCharacterCode)
            {
                var character = (char)currentCharacterCode;

                if (CodeGenerationHelper.ApostropheCharacters.Contains(character) || SpecialCharactersCacheThreadStaticContext.Context.IsSpecialCharacter(character) || 
                    Helpers.IsLatinLetter(character) || char.IsDigit(character) || character == '.' || character == '_')
                    throw new Exception($"Invalid character '{character}'.");

                NonLatinCharacters.Add(character);
            }
        }
    }

    public CodeGenerationHelper([NotNull] ICodeGeneratorParametersProvider parametersProvider)
    {
        _parametersProvider = parametersProvider;
    }

    public void GenerateWhitespacesAndComments([NotNull] StringBuilder inProgressGeneratedCode, bool addAtLeastOneWhitespaceOrComment,
        [CanBeNull] OnCommentGenerated onCommentGenerated = null,
        WhitespaceCommentFlags whitespaceCommentFlags = WhitespaceCommentFlags.WhiteSpacesAndComments,
        [CanBeNull, ItemNotNull] IEnumerable<CharacterTypeProbabilityData> characterTypeProbabilityData = null)
    {
        if ((whitespaceCommentFlags & WhitespaceCommentFlags.WhiteSpace) != WhitespaceCommentFlags.WhiteSpace &&
            _parametersProvider.CommentMarkersData == null)
            throw new Exception($"Both comments and white-spaces are disabled in {nameof(GenerateWhitespacesAndComments)}.");

        var numberOfWhitespacesToAdd = 0;
        var numberOfCommentsToAdd = 0;

        if (!_parametersProvider.SimulateNiceCode)
        {
            numberOfWhitespacesToAdd = (whitespaceCommentFlags & WhitespaceCommentFlags.WhiteSpace) != WhitespaceCommentFlags.WhiteSpace ? 0 :
                _parametersProvider.RandomNumberGenerator.Next(1, _parametersProvider.MaxNumberOfAdditionalWhitespaces);

            numberOfCommentsToAdd = (whitespaceCommentFlags & WhitespaceCommentFlags.Comment) != WhitespaceCommentFlags.Comment ||
                                    _parametersProvider.CommentMarkersData == null ||
                                    _parametersProvider.RandomNumberGenerator.Next(100) <= 50 ? 0 :
                _parametersProvider.RandomNumberGenerator.Next(1, _parametersProvider.MaxNumberOfAdditionalComments);
        }

        if (numberOfWhitespacesToAdd + numberOfCommentsToAdd == 0)
        {
            if (addAtLeastOneWhitespaceOrComment)
            {
                if (_parametersProvider.CommentMarkersData == null ||
                    (whitespaceCommentFlags & WhitespaceCommentFlags.Comment) != WhitespaceCommentFlags.Comment ||
                    (whitespaceCommentFlags & WhitespaceCommentFlags.WhiteSpace) == WhitespaceCommentFlags.WhiteSpace &&
                    _parametersProvider.RandomNumberGenerator.Next(100) <= 70)
                    inProgressGeneratedCode.Append(" ");
                else
                    GenerateComment(inProgressGeneratedCode, onCommentGenerated, true, false, characterTypeProbabilityData);
            }

            return;
        }

        while (numberOfWhitespacesToAdd + numberOfCommentsToAdd > 0)
        {
            bool generateWhitespace;

            if (numberOfCommentsToAdd == 0)
                generateWhitespace = true;
            else if (numberOfWhitespacesToAdd == 0)
                generateWhitespace = false;
            else
                generateWhitespace = _parametersProvider.RandomNumberGenerator.Next(100) <= 70;

            if (generateWhitespace)
            {
                inProgressGeneratedCode.Append(GenerateWhiteSpace(WhitespaceFlags.AnyWhitespace));
                --numberOfWhitespacesToAdd;
            }
            else
            {
                GenerateComment(inProgressGeneratedCode, onCommentGenerated, false, false, characterTypeProbabilityData);
                --numberOfCommentsToAdd;
            }
        }
    }

    public delegate void OnCommentGenerated(ICommentedTextData commentedTextData);

    public void GenerateComment([NotNull] StringBuilder inProgressGeneratedCode, [CanBeNull] OnCommentGenerated onCommentGenerated = null,
        bool generateNiceComment = false,
        bool isCommentEndingCode = false,
        [CanBeNull, ItemNotNull] IEnumerable<CharacterTypeProbabilityData> characterTypeProbabilityData = null)
    {
        if (_parametersProvider.CommentMarkersData == null)
            throw new Exception($"Method {nameof(GenerateComment)}() can be called only if {nameof(_parametersProvider.CommentMarkersData)} is non-null.");

        bool isLineComment = generateNiceComment || _parametersProvider.RandomNumberGenerator.Next(0, 100) <= 70;

        var commentStrBldr = new StringBuilder();
        void DoAddCommentText()
        {
            if (_parametersProvider.RandomNumberGenerator.Next(0, 100) <= 20)
                return;

            var whiteSpaceFlagsInCommentText = WhitespaceFlags.AnyWhitespace;

            if (isLineComment)
                whiteSpaceFlagsInCommentText &= (~WhitespaceFlags.NewLine);

            bool TextMatches(StringBuilder text, char newCharacter, string textToMatch)
            {
                var numberOfCharactersToCopy = textToMatch.Length - 1;
                if (text.Length < numberOfCharactersToCopy)
                    return false;

                var copiedCharacters = new char[textToMatch.Length];
                text.CopyTo(text.Length - numberOfCharactersToCopy, copiedCharacters, 0, numberOfCharactersToCopy);
                copiedCharacters[textToMatch.Length - 1] = newCharacter;

                var copiedText = new String(copiedCharacters);

                return string.Equals(copiedText, textToMatch, StringComparison.OrdinalIgnoreCase);
            }

            var commentText = GenerateText(_parametersProvider.RandomNumberGenerator.Next(1, generateNiceComment ? 3 : _parametersProvider.MaxLengthOfComment),
                whiteSpaceFlagsInCommentText, '\0',
                characterTypeProbabilityData,
                (currentlyGeneratedText, newCharacter) =>
                {
                    if (isLineComment)
                    {
                        if (_parametersProvider.CommentMarkersData.CommentMarkerType == CommentMarkerType.RemarkText &&
                            currentlyGeneratedText.Length == 0 && newCharacter == '*')
                            return false;
                    }
                    else if (TextMatches(currentlyGeneratedText, newCharacter, _parametersProvider.CommentMarkersData.MultilineCommentEndMarker))
                    {
                        return false;
                    }

                    return true;
                });

            commentStrBldr.Append(commentText);
        }

        int commentPosition = inProgressGeneratedCode.Length;
        int commentLength;

        if (_parametersProvider.CommentMarkersData.CommentMarkerType == CommentMarkerType.CSharpStyle)
        {
            if (inProgressGeneratedCode.Length > 0 && inProgressGeneratedCode[inProgressGeneratedCode.Length - 1] == '/')
            {
                // If the current text ends with '/' lets add space, since otherwise, the last symbol will become a comment.
                commentStrBldr.Append(_parametersProvider.RandomNumberGenerator.Next(0, 100) <= 50 ? ' ' : '\t');
                ++commentPosition;
            }
        }
        else if (_parametersProvider.CommentMarkersData.CommentMarkerType == CommentMarkerType.RemarkText)
        {
            if (inProgressGeneratedCode.Length > 0 && !char.IsWhiteSpace(inProgressGeneratedCode[inProgressGeneratedCode.Length - 1]))
            {
                // If the current text does not end with white space (say it is abc) lets add a space
                commentStrBldr.Append(' ');
                ++commentPosition;
            }
        }

        if (isLineComment)
        {
            commentStrBldr.Append(ApplyRandomCapitalization(_parametersProvider.CommentMarkersData.LineCommentMarker));
            DoAddCommentText();
            commentLength = commentStrBldr.Length;

            if (!isCommentEndingCode)
                commentStrBldr.AppendLine();
        }
        else
        {
            commentStrBldr.Append(ApplyRandomCapitalization(_parametersProvider.CommentMarkersData.MultilineCommentStartMarker));
            DoAddCommentText();
            commentStrBldr.Append(ApplyRandomCapitalization(_parametersProvider.CommentMarkersData.MultilineCommentEndMarker));
            commentLength = commentStrBldr.Length;
        }

        // If we added extra spaces, before starting the comment, lets account for those 
        // spaces when calculating the comment length.
        commentLength -= (commentPosition - inProgressGeneratedCode.Length);

        onCommentGenerated?.Invoke(new CommentedTextData(commentPosition, commentLength, isLineComment));

        inProgressGeneratedCode.Append(commentStrBldr.ToString());
    }

    public string ApplyRandomCapitalization([NotNull] string text)
    {
        if (_parametersProvider.IsLanguageCaseSensitive)
            return text;

        var textStrBldr = new StringBuilder();

        for (var i = 0; i < text.Length; ++i)
        {
            var transformedChar = text[i];
            var currentCharToLower = Char.ToLower(transformedChar);
                
            if (currentCharToLower == transformedChar)
            {
                if (_parametersProvider.RandomNumberGenerator.Next(100) >= 50)
                    transformedChar = Char.ToUpper(transformedChar);
            }
            else if (_parametersProvider.RandomNumberGenerator.Next(100) >= 50)
                transformedChar = Char.ToLower(transformedChar);

            textStrBldr.Append(transformedChar);
        }

        return textStrBldr.ToString();
    }

    public string GenerateWhiteSpace(WhitespaceFlags whitespaceFlags)
    {
        if (_parametersProvider.SimulateNiceCode)
            return " ";

        var whiteSpaces = new List<string>();

        if ((whitespaceFlags & WhitespaceFlags.Space) == WhitespaceFlags.Space)
            whiteSpaces.Add(" ");

        if ((whitespaceFlags & WhitespaceFlags.Tab) == WhitespaceFlags.Tab)
            whiteSpaces.Add("\t");

        if ((whitespaceFlags & WhitespaceFlags.NewLine) == WhitespaceFlags.NewLine)
            whiteSpaces.Add(Environment.NewLine);

        return whiteSpaces[_parametersProvider.RandomNumberGenerator.Next(0, whiteSpaces.Count - 1)];
    }

    /// <summary>
    /// Generates a text that can be used as a comment or constant string. 
    /// </summary>
    /// <returns></returns>
    public string GenerateText(int textLength, WhitespaceFlags whitespaceFlags,
        char textStartEndMarker = '\0',
        [CanBeNull, ItemNotNull]IEnumerable<CharacterTypeProbabilityData> characterTypeProbabilityData = null,
        [CanBeNull] IsValidCharacterInGeneratedText isValidCharacterInGeneratedText = null)
    {
           
        var simulationRandomNumberGenerator = _parametersProvider.RandomNumberGenerator;

        var textStartEndMarkers = ApostropheCharacters.Where(x => x != textStartEndMarker).ToList();
        var constantTextStrBldr = new StringBuilder();
        while (constantTextStrBldr.Length < textLength)
        {
            var randomNumber = simulationRandomNumberGenerator.Next(0, 100);

            if (randomNumber <= 5)
            {
                if (simulationRandomNumberGenerator.Next(0, 100) <= 50 && textStartEndMarker != '\0')
                    constantTextStrBldr.Append($"{textStartEndMarker}{textStartEndMarker}");
                else
                    constantTextStrBldr.Append(textStartEndMarkers[simulationRandomNumberGenerator.Next(textStartEndMarkers.Count - 1)]);
            }
            else if (randomNumber < 10 && _parametersProvider.MaxNumberOfAdditionalWhitespaces > 0)
            {
                var numberOfSpaces =
                    Math.Min(textLength - constantTextStrBldr.Length,
                        _parametersProvider.RandomNumberGenerator.Next(1, _parametersProvider.MaxNumberOfAdditionalWhitespaces));

                for (int i = 0; i < numberOfSpaces; ++i)
                    constantTextStrBldr.Append(GenerateWhiteSpace(whitespaceFlags));
            }
            else
            {
                CharacterTypeProbabilityData[] characterTypeProbabilityDataArray = null;

                // ReSharper disable once PossibleMultipleEnumeration
                if (characterTypeProbabilityData != null && characterTypeProbabilityData.Any())
                {
                    characterTypeProbabilityDataArray = characterTypeProbabilityData.ToArray();
                }
                else
                {
                    characterTypeProbabilityDataArray = new CharacterTypeProbabilityData[]
                    {
                        new CharacterTypeProbabilityData(GeneratedCharacterType.SpecialOperatorCharacter, 15),
                        new CharacterTypeProbabilityData(GeneratedCharacterType.SpecialNonOperatorCharacter, 15),
                        new CharacterTypeProbabilityData(GeneratedCharacterType.Dot, 5),
                        new CharacterTypeProbabilityData(GeneratedCharacterType.Underscore, 5),
                        new CharacterTypeProbabilityData(GeneratedCharacterType.NonLatinCharacter, 10),
                        new CharacterTypeProbabilityData(GeneratedCharacterType.Number, 10),
                        new CharacterTypeProbabilityData(GeneratedCharacterType.Letter, 40)
                    };
                }

                var newCharacter = GenerateCharacter(characterTypeProbabilityDataArray);

                if (!(isValidCharacterInGeneratedText?.Invoke(constantTextStrBldr, newCharacter) ?? true))
                    continue;

                constantTextStrBldr.Append(newCharacter);
            }
        }

        return constantTextStrBldr.ToString();
    }

    public char GenerateCharacter(params CharacterTypeProbabilityData[] characterTypeProbabilities)
    {
        var randomNumberGenerator = _parametersProvider.RandomNumberGenerator;

        if (characterTypeProbabilities == null)
            throw new ArgumentNullException(nameof(characterTypeProbabilities));

        void ThrowInvalidProbabilities(int cumulativeProbabilityParam)
        {
            throw new ArgumentException($"Probabilities in '{nameof(characterTypeProbabilities)}' should add up to 100. The actual value is {cumulativeProbabilityParam}.");
        }

        var cumulativeProbability = characterTypeProbabilities?.Sum(x => x.Probability) ?? 0;

        if (cumulativeProbability != 100)
            throw new ArgumentException($"Probabilities in '{nameof(characterTypeProbabilities)}' should add up to 100. The actual value is {cumulativeProbability}.");

        var randomNumber = randomNumberGenerator.Next(0, 100);

        cumulativeProbability = 0;

        for (int i = 0; i < characterTypeProbabilities.Length; ++i)
        {
            var characterTypeProbabilityData = characterTypeProbabilities[i];
            var newCumulativeProbability = cumulativeProbability + characterTypeProbabilityData.Probability;

            if (newCumulativeProbability > 100)
                ThrowInvalidProbabilities(newCumulativeProbability);

            if (randomNumber >= cumulativeProbability && randomNumber <= newCumulativeProbability)
            {
                switch (characterTypeProbabilityData.GeneratedCharacterType)
                {
                    case GeneratedCharacterType.Letter:

                        randomNumber = randomNumberGenerator.Next(51);

                        if (randomNumber < 26)
                            return (char)('a' + randomNumber);

                        return (char)('A' + randomNumber - 26);

                    case GeneratedCharacterType.Number:
                        return (char)('0' + randomNumberGenerator.Next(9));

                    case GeneratedCharacterType.SpecialOperatorCharacter:
                        return SpecialOperatorCharacters[randomNumberGenerator.Next(SpecialOperatorCharacters.Count - 1)];

                    case GeneratedCharacterType.Apostrophe:
                        return CodeGenerationHelper.ApostropheCharacters[randomNumberGenerator.Next(CodeGenerationHelper.ApostropheCharacters.Count - 1)];

                    case GeneratedCharacterType.SpecialNonOperatorCharacter:
                        return SpecialNonOperatorCharacters[randomNumberGenerator.Next(SpecialNonOperatorCharacters.Count - 1)];

                    case GeneratedCharacterType.Underscore:
                        return '_';

                    case GeneratedCharacterType.Dot:
                        return '.';

                    case GeneratedCharacterType.NonLatinCharacter:
                        return NonLatinCharacters[randomNumberGenerator.Next(NonLatinCharacters.Count - 1)];

                    default:
                        throw new ArgumentException($"Invalid value '{characterTypeProbabilityData.GeneratedCharacterType}'.");
                }
            }

            cumulativeProbability = newCumulativeProbability;
        }

        throw new Exception("Failed to generate a random character.");
    }
}