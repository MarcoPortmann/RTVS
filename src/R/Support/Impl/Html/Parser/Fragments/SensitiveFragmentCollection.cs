﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Languages.Core.Text;

namespace Microsoft.Html.Core.Parser {
    /// <summary>
    /// Class represents collection of text ranges that have 'sensitive' separator
    /// sequences at the beginning and at the end. For example, HTML comments &lt;!-- -->
    /// or ASP.NET blocks like &lt;% %>. This collection has additional methods that
    /// help to detemine if change to a text buffer may have created new or invalidated
    /// existing comments or external code fragments.
    /// </summary>
    /// <typeparam name="T">Type that implements ITextRange and supplies separator information via ISensitiveFragmentSeparatorsInfo interface</typeparam>
    public abstract class SensitiveFragmentCollection<T> : TextRangeCollection<T> where T : ITextRange {
        /// <summary>
        /// Determines if particular change to the document creates new or changes boundaries
        /// of one of the existing sensitive fragments (comments, artifacts).
        /// </summary>
        /// <param name="collection">Fragment collection.</param>
        /// <param name="oldText">Document text before the change.</param>
        /// <param name="newText">Document text after the change.</param>
        /// <param name="start">Change start position.</param>
        /// <param name="oldLength">Length of changed area before the change. Zero means new text was inserted.</param>
        /// <param name="newLength">Length of changed area after the change. Zero means text was deleted.</param>
        /// <returns>True of change is destruction and document needs to be reprocessed.</returns>
        public virtual bool IsDestructiveChange(int start, int oldLength, int newLength, ITextProvider oldText, ITextProvider newText) {
            // Get list of items overlapping the change. Note that items haven't been
            // shifted yet and hence their positions match the old text snapshot.
            IReadOnlyList<T> itemsInRange = ItemsInRange(new TextRange(start, oldLength));

            // Is crosses item boundaries, it is destructive
            if (itemsInRange.Count > 1 || (itemsInRange.Count == 1 && (!itemsInRange[0].Contains(start) || !itemsInRange[0].Contains(start + oldLength))))
                return true;

            foreach (ISensitiveFragmentSeparatorsInfo curSeparatorInfo in SeparatorInfos) {
                if (IsDestructiveChangeForSeparator(curSeparatorInfo, itemsInRange, start, oldLength, newLength, oldText, newText)) {
                    return true;
                }
            }

            return false;
        }

        private bool IsDestructiveChangeForSeparator(ISensitiveFragmentSeparatorsInfo separatorInfo, IReadOnlyList<T> itemsInRange, int start, int oldLength, int newLength, ITextProvider oldText, ITextProvider newText) {
            if (separatorInfo == null) {
                return false;
            }

            if (separatorInfo.LeftSeparator.Length == 0 && separatorInfo.RightSeparator.Length == 0) {
                return false;
            }

            // Find out if one of the existing fragments contains position 
            // and if change damages fragment start or end separators

            string leftSeparator = separatorInfo.LeftSeparator;
            string rightSeparator = separatorInfo.RightSeparator;

            // If no items are affected, change is unsafe only if new region contains left side separators.
            if (itemsInRange.Count == 0) {
                // Simple optimization for whitespace insertion
                if (oldLength == 0 && String.IsNullOrWhiteSpace(newText.GetText(new TextRange(start, newLength))))
                    return false;

                int fragmentStart = Math.Max(0, start - leftSeparator.Length + 1);
                int fragmentEnd;

                // Take into account that user could have deleted space between existing 
                // <! and -- or added - to the existing <!- so extend search range accordingly.

                fragmentEnd = Math.Min(newText.Length, start + newLength + leftSeparator.Length - 1);

                int fragmentStartPosition = newText.IndexOf(leftSeparator, TextRange.FromBounds(fragmentStart, fragmentEnd), true);
                if (fragmentStartPosition >= 0) {
                    // We could've found the left separator only in the newly inserted text since we extended the range we examined
                    // by one less than the separator length. Return true, no further checks necessary.
                    return true;
                }

                return false;
            }

            // Is change completely inside an existing item?
            if (itemsInRange.Count == 1 && (itemsInRange[0].Contains(start) && itemsInRange[0].Contains(start + oldLength))) {
                // Check that change does not affect item left separator
                if (TextRange.Contains(itemsInRange[0].Start, leftSeparator.Length, start))
                    return true;

                // Check that change does not affect item right separator. Note that we should not be using 
                // TextRange.Intersect since in case oldLength is zero (like when user is typing right before %> or ?>)
                // TextRange.Intersect will determine that zero-length range intersects with the right separator
                // which is incorrect. Typing at position 10 does not change separator at position 10. Similarly,
                // deleting text right before %> or ?> does not make change destructive.

                IHtmlToken htmlToken = itemsInRange[0] as IHtmlToken;
                if (htmlToken == null || htmlToken.IsWellFormed) {
                    int rightSeparatorStart = itemsInRange[0].End - rightSeparator.Length;
                    if (start + oldLength > rightSeparatorStart) {
                        if (TextRange.Intersect(rightSeparatorStart, rightSeparator.Length, start, oldLength))
                            return true;
                    }
                }

                // Touching left separator is destructive too, like when changing <% to <%@
                // Check that change does not affect item left separator (whitespace is fine)
                if (itemsInRange[0].Start + leftSeparator.Length == start) {
                    if (oldLength == 0) {
                        string text = newText.GetText(new TextRange(start, newLength));
                        if (String.IsNullOrWhiteSpace(text))
                            return false;
                    }

                    return true;
                }

                int fragmentStart = itemsInRange[0].Start + separatorInfo.LeftSeparator.Length;
                fragmentStart = Math.Max(fragmentStart, start - separatorInfo.RightSeparator.Length + 1);
                int changeLength = newLength - oldLength;
                int fragmentEnd = itemsInRange[0].End + changeLength;
                fragmentEnd = Math.Min(fragmentEnd, start + newLength + separatorInfo.RightSeparator.Length - 1);

                if (newText.IndexOf(separatorInfo.RightSeparator, TextRange.FromBounds(fragmentStart, fragmentEnd), true) >= 0)
                    return true;

                return false;
            }

            return true;
        }

        protected abstract IEnumerable<ISensitiveFragmentSeparatorsInfo> SeparatorInfos { get; }

        public override IReadOnlyList<T> ItemsInRange(ITextRange range) {
            IReadOnlyList<T> list = base.ItemsInRange(range);

            if (Count > 0) {
                IHtmlToken lastItem = this[Count - 1] as IHtmlToken;
                if (lastItem != null && !lastItem.IsWellFormed) {
                    if (range.Contains(lastItem.End)) {
                        // Underlying method returs static readonly collection if nothing was found 
                        // in the range so we need to create a new collection here.
                        List<T> modifiedList = new List<T>(list.Count + 1);
                        modifiedList.AddRange(list);
                        modifiedList.Add(this[Count - 1]);

                        list = modifiedList;
                    }
                }
            }

            return list;
        }
    }
}
