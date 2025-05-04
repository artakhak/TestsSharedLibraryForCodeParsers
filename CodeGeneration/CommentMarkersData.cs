// Copyright (c) TestsSharedLibraryForCodeParsers Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the solution root for license information.

using JetBrains.Annotations;

namespace TestsSharedLibraryForCodeParsers.CodeGeneration;

public class CommentMarkersData
{
    public CommentMarkersData([NotNull] string lineCommentMarker, [NotNull] string multilineCommentStartMarker, [NotNull] string multilineCommentEndMarker, CommentMarkerType commentMarkerType)
    {
        LineCommentMarker = lineCommentMarker;
        MultilineCommentStartMarker = multilineCommentStartMarker;
        MultilineCommentEndMarker = multilineCommentEndMarker;
        CommentMarkerType = commentMarkerType;
    }

    [NotNull]
    public string LineCommentMarker { get; }

    [NotNull]
    public string MultilineCommentStartMarker { get; }

    [NotNull]
    public string MultilineCommentEndMarker { get; }

    public CommentMarkerType CommentMarkerType { get; }
}