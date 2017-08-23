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
namespace EPi.Libraries.QnaMaker.Models
{
    using System.Runtime.Serialization;

    using Newtonsoft.Json;

    /// <summary>
    /// Class FeedbackRecord.
    /// </summary>
    [DataContract]
    public class FeedbackRecord
    {
        /// <summary>
        /// Gets or sets the user identifier. 
        /// </summary>
        /// <value>A unique identifier to distinguish feedback from different users.</value>
        [DataMember]
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user question.
        /// </summary>
        /// <value>Question asked by user (user utterance).</value>
        [DataMember]
        [JsonProperty("userQuestion")]
        public string UserQuestion { get; set; }

        /// <summary>
        /// Gets or sets the kb question.
        /// </summary>
        /// <value>Question field from the correct question-answer pair for userQuestion. This field should exactly match the question present in the knowledge base.</value>
        [DataMember]
        [JsonProperty("kbQuestion")]
        public string KbQuestion { get; set; }

        /// <summary>
        /// Gets or sets the kb answer.
        /// </summary>
        /// <value>Answer field from the correct question-answer pair for userQuestion. This field should exactly match the answer present in the knowledge base.</value>
        [DataMember]
        [JsonProperty("kbAnswer")]
        public string KbAnswer { get; set; }
    }
}