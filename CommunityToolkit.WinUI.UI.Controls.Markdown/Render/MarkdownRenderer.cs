// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using CommunityToolkit.Common.Parsers.Core;
using CommunityToolkit.Common.Parsers.Markdown;
using CommunityToolkit.Common.Parsers.Markdown.Inlines;
using CommunityToolkit.Common.Parsers.Markdown.Render;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace CommunityToolkit.WinUI.UI.Controls.Markdown.Render
{
    /// <summary>
    /// Generates Framework Elements for the UWP Markdown Textblock.
    /// </summary>
    public partial class MarkdownRenderer : MarkdownRendererBase
    {
#pragma warning disable CS0618 // Type or member is obsolete
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownRenderer"/> class.
        /// </summary>
        /// <param name="document">The Document to Render.</param>
        /// <param name="linkRegister">The LinkRegister, <see cref="MarkdownTextBlock"/> will use itself.</param>
        /// <param name="imageResolver">The Image Resolver, <see cref="MarkdownTextBlock"/> will use itself.</param>
        /// <param name="codeBlockResolver">The Code Block Resolver, <see cref="MarkdownTextBlock"/> will use itself.</param>
        public MarkdownRenderer(MarkdownDocument document, ILinkRegister linkRegister, IImageResolver imageResolver, ICodeBlockResolver codeBlockResolver)
            : base(document)
        {
            LinkRegister = linkRegister;
            ImageResolver = imageResolver;
            CodeBlockResolver = codeBlockResolver;
            DefaultEmojiFont = new FontFamily("Segoe UI Emoji");
        }

        /// <summary>
        /// Called externally to render markdown to a text block.
        /// </summary>
        /// <returns> A XAML UI element. </returns>
        public UIElement Render()
        {
            var stackPanel = new StackPanel();
            RootElement = stackPanel;
            Render(new UIElementCollectionRenderContext(stackPanel.Children) { Foreground = Foreground });

            // Set background and border properties.
            stackPanel.Background = Background;
            stackPanel.BorderBrush = BorderBrush;
            stackPanel.BorderThickness = BorderThickness;
            stackPanel.Padding = Padding;

            return stackPanel;
        }

        /// <summary>
        /// Creates a new RichTextBlock, if the last element of the provided collection isn't already a RichTextBlock.
        /// </summary>
        /// <returns>The rich text block</returns>
        protected RichTextBlock CreateOrReuseRichTextBlock(IRenderContext context)
        {
            var localContext = context as UIElementCollectionRenderContext;
            if (localContext == null)
            {
                throw new RenderContextIncorrectException();
            }

            var blockUIElementCollection = localContext.BlockUIElementCollection;

            // Reuse the last RichTextBlock, if possible.
            if (blockUIElementCollection != null && blockUIElementCollection.Count > 0 && blockUIElementCollection[blockUIElementCollection.Count - 1] is RichTextBlock)
            {
                return (RichTextBlock)blockUIElementCollection[blockUIElementCollection.Count - 1];
            }

            var result = new RichTextBlock
            {
                CharacterSpacing = CharacterSpacing,
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontStretch = FontStretch,
                FontStyle = FontStyle,
                FontWeight = FontWeight,
                Foreground = localContext.Foreground,
                IsTextSelectionEnabled = IsTextSelectionEnabled,
                TextWrapping = TextWrapping,
                FlowDirection = FlowDirection
            };
            localContext.BlockUIElementCollection?.Add(result);

            return result;
        }

        /// <summary>
        /// Creates a new TextBlock, with default settings.
        /// </summary>
        /// <returns>The created TextBlock</returns>
        protected TextBlock CreateTextBlock(RenderContext context)
        {
            var result = new TextBlock
            {
                CharacterSpacing = CharacterSpacing,
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontStretch = FontStretch,
                FontStyle = FontStyle,
                FontWeight = FontWeight,
                Foreground = context.Foreground,
                IsTextSelectionEnabled = IsTextSelectionEnabled,
                TextWrapping = TextWrapping,
                FlowDirection = FlowDirection
            };
            return result;
        }

        /// <summary>
        /// Performs an action against any runs that occur within the given span.
        /// </summary>
        protected void AlterChildRuns(Span parentSpan, Action<Span, Run> action)
        {
            foreach (var inlineElement in parentSpan.Inlines)
            {
                if (inlineElement is Span span)
                {
                    AlterChildRuns(span, action);
                }
                else if (inlineElement is Run)
                {
                    action(parentSpan, (Run)inlineElement);
                }
            }
        }

        /// <summary>
        /// Checks if all text elements inside the given container are superscript.
        /// </summary>
        /// <returns> <c>true</c> if all text is superscript (level 1); <c>false</c> otherwise. </returns>
        private bool AllTextIsSuperscript(IInlineContainer container, int superscriptLevel = 0)
        {
            foreach (var inline in container.Inlines)
            {
                if (inline is SuperscriptTextInline textInline)
                {
                    // Remove any nested superscripts.
                    if (AllTextIsSuperscript(textInline, superscriptLevel + 1) == false)
                    {
                        return false;
                    }
                }
                else if (inline is IInlineContainer)
                {
                    // Remove any superscripts.
                    if (AllTextIsSuperscript((IInlineContainer)inline, superscriptLevel) == false)
                    {
                        return false;
                    }
                }
                else if (inline is IInlineLeaf && !ParseHelpers.IsMarkdownBlankOrWhiteSpace(((IInlineLeaf)inline).Text))
                {
                    if (superscriptLevel != 1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Removes all superscript elements from the given container.
        /// </summary>
        private void RemoveSuperscriptRuns(IInlineContainer container, bool insertCaret)
        {
            for (int i = 0; i < container.Inlines.Count; i++)
            {
                var inline = container.Inlines[i];
                if (inline is SuperscriptTextInline textInline)
                {
                    // Remove any nested superscripts.
                    RemoveSuperscriptRuns(textInline, insertCaret);

                    // Remove the superscript element, insert all the children.
                    container.Inlines.RemoveAt(i);
                    if (insertCaret)
                    {
                        container.Inlines.Insert(i++, new TextRunInline { Text = "^" });
                    }

                    foreach (var superscriptInline in textInline.Inlines)
                    {
                        container.Inlines.Insert(i++, superscriptInline);
                    }

                    i--;
                }
                else if (inline is IInlineContainer)
                {
                    // Remove any superscripts.
                    RemoveSuperscriptRuns((IInlineContainer)inline, insertCaret);
                }
            }
        }

        private void Preventative_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint((UIElement)sender);

            if (pointerPoint.Properties.IsHorizontalMouseWheel)
            {
                e.Handled = false;
                return;
            }

            var rootViewer = RootElement.FindAscendant<ScrollViewer>();
            if (rootViewer != null)
            {
                pointerWheelChanged?.Invoke(rootViewer, new object[] { e });
                e.Handled = true;
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}