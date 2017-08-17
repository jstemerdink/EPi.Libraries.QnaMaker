// Copyright © 2017 Jeroen Stemerdink.
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

namespace EPi.Libraries.QnaMaker.Examples
{
    using System.ComponentModel.DataAnnotations;

    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.DataAnnotations;

    using Episerver.Playground10.Models.Blocks;

    using EPi.Libraries.QnaMaker.Attributes;

    /// <summary>
    /// Class QnaOverviewPage.
    /// </summary>
    /// <seealso cref="EPiServer.Core.PageData" />
    [ContentType(DisplayName = "QnA Overviewpage", GUID = "38279961-a27d-4f94-9a21-27b565fa2cd7", Description = "A QnA Overviewpage")]
    [QnaOverviewPage]
    public class QnaOverviewPage : PageData
    {
        /// <summary>
        /// Gets or sets the main body.
        /// </summary>
        /// <value>The main body.</value>
        [CultureSpecific]
        [Display(
            Name = "Main body",
            Description =
                "The main body will be shown in the main content area of the page, using the XHTML-editor you can insert for example text, images and tables.",
            GroupName = SystemTabNames.Content,
            Order = 1)]
        public virtual XhtmlString MainBody { get; set; }

        /// <summary>
        /// Gets or sets the qna items.
        /// </summary>
        /// <value>The qna items.</value>
        [Display(Name = "QnA items", Description = "QnA items", GroupName = SystemTabNames.Content, Order = 10)]
        [QnaContainer]
        [AllowedTypes(typeof(QnaItemBlock))]
        public virtual ContentArea QnaItems { get; set; }

        /// <summary>
        /// Gets or sets the qna knowledgebase identifier.
        /// </summary>
        /// <value>The qna knowledgebase identifier.</value>
        [Display(
            Name = "Knowledgebase Id",
            Description = "The Knowledgebase Id",
            GroupName = SystemTabNames.Content,
            Order = 20)]
        [ScaffoldColumn(false)]
        [QnaKnowledgebaseId]
        public virtual string QnaKnowledgebaseId { get; set; }
    }
}