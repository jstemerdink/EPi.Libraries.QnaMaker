
The settings were added to your appSettings:

<add key="qna:baseuri" value="https://westus.api.cognitive.microsoft.com/qnamaker/v2.0/knowledgebases" />
<add key="qna:subscriptionkey" value="" />

You will need to set the appropriate values, else a configuration error will be thrown.

Usage example:

    [QnaOverviewPage]
    public class QnaOverviewPage : PageData
    {
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

    [QnaItem]
    public class QnaItemBlock : BlockData
    {
        [CultureSpecific]
        [Display(Name = "The answer", Description = "The answer", GroupName = SystemTabNames.Content, Order = 2)]
        [QnaAnswer]
        public virtual XhtmlString Answer { get; set; }

        [CultureSpecific]
        [Display(Name = "The question", Description = "The question", GroupName = SystemTabNames.Content, Order = 1)]
        [QnaQuestion]
        public virtual string Question { get; set; }
    }

