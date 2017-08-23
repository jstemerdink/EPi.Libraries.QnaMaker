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

    using EPi.Libraries.QnaMaker.Attributes;

    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.DataAnnotations;

    [ContentType(DisplayName = "QnaItemBlock", GUID = "74956b03-1b6a-411f-8ada-5429f243496f", Description = "")]
    [QnaItem]
    public class QnaItemBlock : BlockData
    {
        [CultureSpecific]
        [Display(Name = "The question", Description = "The question", GroupName = SystemTabNames.Content, Order = 2)]
        [QnaAnswer]
        public virtual XhtmlString Answer { get; set; }

        [CultureSpecific]
        [Display(Name = "The question", Description = "The question", GroupName = SystemTabNames.Content, Order = 1)]
        [QnaQuestion]
        public virtual string Question { get; set; }
    }
}