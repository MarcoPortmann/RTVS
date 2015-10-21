﻿using System;
using System.Diagnostics;
using Microsoft.Languages.Editor;
using Microsoft.Languages.Editor.Completion;
using Microsoft.Languages.Editor.Controller.Constants;
using Microsoft.Languages.Editor.Services;
using Microsoft.R.Core.AST;
using Microsoft.R.Editor.Completion;
using Microsoft.R.Editor.Completion.AutoCompletion;
using Microsoft.R.Editor.Document;
using Microsoft.R.Editor.Document.Definitions;
using Microsoft.R.Editor.Formatting;
using Microsoft.R.Editor.Tree.Definitions;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.R.Editor.Commands
{
    /// <summary>
    /// Processes typing in the R editor document. 
    /// Implements <seealso cref="ICommandTarget" /> 
    /// to receive typing as commands
    /// </summary>
    internal class RTypingCommandHandler : TypingCommandHandler
    {
        public RTypingCommandHandler(ITextView textView)
            : base(textView)
        {
        }

        public override CommandResult Invoke(Guid group, int id, object inputArg, ref object outputArg)
        {
            if (group == VSConstants.VSStd2K)
            {
                char typedChar = GetTypedChar(group, id, inputArg);
                if(typedChar != '\0')
                {
                    // Let provisional text completion check if the new character
                    // position in the tree inside existing string so it doesn't.
                    // complete " inside "".
                    SeparatorCompletion.BeforeTypeCharacter(TextView, typedChar);
                }
            }

            return base.Invoke(group, id, inputArg, ref outputArg);
        }

        #region ICommand
        public override void PostProcessInvoke(CommandResult result, Guid group, int id, object inputArg, ref object outputArg)
        {
            if (group == VSConstants.VSStd2K)
            {
                char typedChar = GetTypedChar(group, id, inputArg);

                if (AutoFormat.IsAutoformatTriggerCharacter(typedChar))
                {
                    IREditorDocument document = REditorDocument.TryFromTextBuffer(TextView.TextBuffer);
                    if (document != null && !document.IsTransient)
                    {
                        IEditorTree tree = document.EditorTree;
                        tree.EnsureTreeReady();
                        AutoFormat.HandleAutoFormat(TextView, TextView.TextBuffer, tree.AstRoot, typedChar);
                    }
                }

                HandleCompletion(typedChar);

                base.PostProcessInvoke(result, group, id, inputArg, ref outputArg);
            }
        }
        #endregion

        protected override CompletionController CompletionController
        {
            get { return ServiceManager.GetService<RCompletionController>(TextView); }
        }

        /// <summary>
        /// Handles completion of braces in R document
        /// </summary>
        protected virtual void HandleCompletion(char typedChar)
        {
            switch (typedChar)
            {
                case '\'':
                case '\"':
                case '{':
                case '(':
                case '[':
                    SeparatorCompletion.Complete(TextView, typedChar);
                    break;
            }

            // Workaround for Dev12 bug 730266 - QuoteCompletion will suppress adding provisional text,
            // but it has no idea when to allow it again.
            // Hopefully someday the static variable workaround in that class can be removed and this
            // workaround can be removed.
            SeparatorCompletion.CancelSuppression();
        }
    }
}
