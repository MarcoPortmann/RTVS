// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using Microsoft.Languages.Core.Text;
using Microsoft.R.Editor;
using Microsoft.R.Editor.Document;
using Microsoft.R.Editor.Formatting;
using Microsoft.R.Editor.Settings;
using Microsoft.VisualStudio.R.Package.Utilities;
using Microsoft.VisualStudio.R.Packages.R;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.R.Package.Expansions {
    /// <summary>
    /// Text view client that manages insertion of snippets
    /// </summary>
    public sealed class ExpansionClient : IVsExpansionClient {
        private static readonly string[] AllStandardSnippetTypes = { "Expansion", "SurroundsWith" };
        private static readonly string[] SurroundWithSnippetTypes = { "SurroundsWith" };

        private IVsExpansionManager _expansionManager;
        private IVsExpansionSession _expansionSession;
        private IExpansionsCache _cache;

        private bool _earlyEndExpansionHappened = false;
        private string _shortcut = null;
        private string _title = null;

        public ExpansionClient(ITextView textView, ITextBuffer textBuffer, IVsExpansionManager expansionManager, IExpansionsCache cache) {
            TextView = textView;
            TextBuffer = textBuffer;
            _expansionManager = expansionManager;
            _cache = cache;
        }

        public ITextBuffer TextBuffer { get; }
        public ITextView TextView { get; }

        internal IVsExpansionSession Session => _expansionSession;

        public bool IsEditingExpansion() {
            return _expansionSession != null;
        }

        internal bool IsCaretInsideSnippetFields() {
            if (!IsEditingExpansion() || TextView.Caret.InVirtualSpace) {
                return false;
            }

            // Get the snippet span
            TextSpan[] pts = new TextSpan[1];
            ErrorHandler.ThrowOnFailure(_expansionSession.GetSnippetSpan(pts));
            TextSpan snippetSpan = pts[0];

            // Convert text span to stream positions
            int snippetStart, snippetEnd;
            var vsTextLines = TextBuffer.GetBufferAdapter<IVsTextLines>();
            ErrorHandler.ThrowOnFailure(vsTextLines.GetPositionOfLineIndex(snippetSpan.iStartLine, snippetSpan.iStartIndex, out snippetStart));
            ErrorHandler.ThrowOnFailure(vsTextLines.GetPositionOfLineIndex(snippetSpan.iEndLine, snippetSpan.iEndIndex, out snippetEnd));

            var textStream = (IVsTextStream)vsTextLines;

            // check to see if the caret position is inside one of the snippet fields
            IVsEnumStreamMarkers enumMarkers;
            if (VSConstants.S_OK == textStream.EnumMarkers(snippetStart, snippetEnd - snippetStart, 0, (uint)(ENUMMARKERFLAGS.EM_ALLTYPES | ENUMMARKERFLAGS.EM_INCLUDEINVISIBLE | ENUMMARKERFLAGS.EM_CONTAINED), out enumMarkers)) {
                IVsTextStreamMarker curMarker;
                SnapshotPoint caretPoint = TextView.Caret.Position.BufferPosition;
                while (VSConstants.S_OK == enumMarkers.Next(out curMarker)) {
                    int curMarkerPos;
                    int curMarkerLen;
                    if (VSConstants.S_OK == curMarker.GetCurrentSpan(out curMarkerPos, out curMarkerLen)) {
                        if (caretPoint.Position >= curMarkerPos && caretPoint.Position <= curMarkerPos + curMarkerLen) {
                            int markerType;
                            if (VSConstants.S_OK == curMarker.GetType(out markerType)) {
                                if (markerType == (int)MARKERTYPE2.MARKER_EXSTENCIL || markerType == (int)MARKERTYPE2.MARKER_EXSTENCIL_SELECTED) {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public int InvokeInsertionUI(int invokationCommand) {
            if ((_expansionManager != null) && (TextView != null)) {
                // Set the allowable snippet types and prompt text according to the current command.
                string[] snippetTypes = null;
                string promptText = "";
                if (invokationCommand == (uint)VSConstants.VSStd2KCmdID.INSERTSNIPPET) {
                    snippetTypes = AllStandardSnippetTypes;
                    promptText = Resources.InsertSnippet;
                } else if (invokationCommand == (uint)VSConstants.VSStd2KCmdID.SURROUNDWITH) {
                    snippetTypes = SurroundWithSnippetTypes;
                    promptText = Resources.SurrondWithSnippet;
                }

                return _expansionManager.InvokeInsertionUI(
                    TextView.GetViewAdapter<IVsTextView>(),
                    this,
                    RGuidList.RLanguageServiceGuid,
                    snippetTypes,
                    (snippetTypes != null) ? snippetTypes.Length : 0,
                    0,
                    null, // Snippet kinds
                    0,    // Length of snippet kinds
                    0,
                    promptText,
                    "\t");
            }

            return VSConstants.E_UNEXPECTED;
        }

        public int GoToNextExpansionField() {
            return _expansionSession.GoToNextExpansionField(0 /* fCommitIfLast - so don't commit */);
        }

        public int GoToPreviousExpansionField() {
            return _expansionSession.GoToPreviousExpansionField();
        }

        public int EndExpansionSession(bool leaveCaretWhereItIs) {
            return _expansionSession.EndCurrentExpansion(leaveCaretWhereItIs ? 1 : 0);
        }

        /// <summary>
        /// Inserts a snippet based on a shortcut string.
        /// </summary>
        public int StartSnippetInsertion(out bool snippetInserted) {
            int hr = VSConstants.E_FAIL;
            snippetInserted = false;

            // Get the text at the current caret position and
            // determine if it is a snippet shortcut.
            if (!TextView.Caret.InVirtualSpace) {
                SnapshotPoint caretPoint = TextView.Caret.Position.BufferPosition;

                var document = REditorDocument.FindInProjectedBuffers(TextView.TextBuffer);
                // Document may be null in tests
                var textBuffer = document != null ? document.TextBuffer : TextView.TextBuffer;
                var expansion = textBuffer.GetBufferAdapter<IVsExpansion>();
                _earlyEndExpansionHappened = false;

                Span span;
                _shortcut = TextView.GetItemBeforeCaret(out span, x => true);
                VsExpansion? exp = _cache.GetExpansion(_shortcut);

                // Get view span
                var ts = span.Length > 0 ? TextSpanFromSpan(TextView, span) : TextSpanFromPoint(caretPoint);

                // Map it down to R buffer
                var start = TextView.MapDownToR(span.Start);
                var end = TextView.MapDownToR(span.End);

                if (exp.HasValue && start.HasValue && end.HasValue) {
                    // Insert into R buffer
                    ts = TextSpanFromSpan(textBuffer, Span.FromBounds(start.Value, end.Value));
                    hr = expansion.InsertNamedExpansion(exp.Value.title, exp.Value.path, ts, this, RGuidList.RLanguageServiceGuid, 0, out _expansionSession);
                    if (_earlyEndExpansionHappened) {
                        // EndExpansion was called before InsertExpansion returned, so set _expansionSession
                        // to null to indicate that there is no active expansion session. This can occur when 
                        // the snippet inserted doesn't have any expansion fields.
                        _expansionSession = null;
                        _earlyEndExpansionHappened = false;
                        _shortcut = null;
                        _title = null;
                    }
                    ErrorHandler.ThrowOnFailure(hr);
                    snippetInserted = true;
                    return hr;
                }
            }
            return hr;
        }

        private static TextSpan TextSpanFromPoint(SnapshotPoint point) {
            var ts = new TextSpan();
            ITextSnapshotLine line = point.GetContainingLine();
            ts.iStartLine = line.LineNumber;
            ts.iEndLine = line.LineNumber;
            ts.iStartIndex = point.Position;
            ts.iEndIndex = point.Position;
            return ts;
        }

        private static TextSpan TextSpanFromSpan(ITextView textView, Span span) {
            return TextSpanFromSpan(textView.TextBuffer, span);
        }

        private static TextSpan TextSpanFromSpan(ITextBuffer textBuffer, Span span) {
            var ts = new TextSpan();
            ITextSnapshotLine line = textBuffer.CurrentSnapshot.GetLineFromPosition(span.Start);
            ts.iStartLine = line.LineNumber;
            ts.iEndLine = line.LineNumber;
            ts.iStartIndex = span.Start - line.Start;
            ts.iEndIndex = span.End - line.Start;
            return ts;
        }

        #region IVsExpansionClient
        public int EndExpansion() {
            if (_expansionSession == null) {
                _earlyEndExpansionHappened = true;
            } else {
                _expansionSession = null;
            }
            _title = null;
            _shortcut = null;

            return VSConstants.S_OK;
        }

        public int FormatSpan(IVsTextLines pBuffer, TextSpan[] ts) {
            int hr = VSConstants.S_OK;
            int startPos = -1;
            int endPos = -1;
            var vsTextLines = TextBuffer.GetBufferAdapter<IVsTextLines>();
            if (ErrorHandler.Succeeded(vsTextLines.GetPositionOfLineIndex(ts[0].iStartLine, ts[0].iStartIndex, out startPos)) &&
                ErrorHandler.Succeeded(vsTextLines.GetPositionOfLineIndex(ts[0].iEndLine, ts[0].iEndIndex, out endPos))) {

                var rStart = TextView.MapDownToR(startPos);
                var rEnd = TextView.MapDownToR(endPos);

                if (rStart.HasValue && rEnd.HasValue && rStart.Value < rEnd.Value) {
                    RangeFormatter.FormatRange(TextView, TextBuffer, TextRange.FromBounds(rStart.Value, rEnd.Value), REditorSettings.FormatOptions);
                }
            }
            return hr;
        }

        public int GetExpansionFunction(MSXML.IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc) {
            pFunc = null;
            return VSConstants.S_OK;
        }

        public int IsValidKind(IVsTextLines pBuffer, TextSpan[] ts, string bstrKind, out int pfIsValidKind) {
            pfIsValidKind = 1;
            return VSConstants.S_OK;
        }

        public int IsValidType(IVsTextLines pBuffer, TextSpan[] ts, string[] rgTypes, int iCountTypes, out int pfIsValidType) {
            pfIsValidType = 1;
            return VSConstants.S_OK;
        }

        public int OnAfterInsertion(IVsExpansionSession pSession) {
            return VSConstants.S_OK;
        }

        public int OnBeforeInsertion(IVsExpansionSession pSession) {
            return VSConstants.S_OK;
        }

        public int OnItemChosen(string pszTitle, string pszPath) {
            int hr = VSConstants.E_FAIL;
            if (!TextView.Caret.InVirtualSpace) {
                SnapshotPoint caretPoint = TextView.Caret.Position.BufferPosition;

                IVsExpansion expansion = TextBuffer.GetBufferAdapter<IVsExpansion>();
                _earlyEndExpansionHappened = false;
                _title = pszTitle;
                var ts = TextSpanFromPoint(caretPoint);

                hr = expansion.InsertNamedExpansion(pszTitle, pszPath, ts, this, RGuidList.RLanguageServiceGuid, 0, out _expansionSession);
                if (_earlyEndExpansionHappened) {
                    // EndExpansion was called before InsertNamedExpansion returned, so set _expansionSession
                    // to null to indicate that there is no active expansion session. This can occur when 
                    // the snippet inserted doesn't have any expansion fields.
                    _expansionSession = null;
                    _earlyEndExpansionHappened = false;
                    _title = null;
                    _shortcut = null;
                }
            }
            return hr;
        }

        public int PositionCaretForEditing(IVsTextLines pBuffer, TextSpan[] ts) {
            return VSConstants.S_OK;
        }
        #endregion
    }
}
