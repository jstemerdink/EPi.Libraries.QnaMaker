# EPi.Libraries.QnaMaker

[![Build status](https://ci.appveyor.com/api/projects/status/p9wb03tyde11si8s/branch/master?svg=true)](https://ci.appveyor.com/project/jstemerdink/epi-libraries-qnamaker/branch/master)
[![GitHub version](https://badge.fury.io/gh/jstemerdink%2FEPi.Libraries.QnaMaker.svg)](https://badge.fury.io/gh/jstemerdink%2FEPi.Libraries.QnaMaker)
[![Platform](https://img.shields.io/badge/platform-.NET%204.5.2-blue.svg?style=flat)](https://msdn.microsoft.com/en-us/library/w0x726c2%28v=vs.110%29.aspx)
[![Platform](https://img.shields.io/badge/EPiServer-%2010.0.1-orange.svg?style=flat)](http://world.episerver.com/cms/)
[![GitHub license](https://img.shields.io/badge/license-MIT%20license-blue.svg?style=flat)](LICENSE)

## About
Connect your site to the QnA maker api
See [QnA maker site](https://azure.microsoft.com/en-us/services/cognitive-services/qna-maker/) for information about the QnA maker API.

## How to use
```
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

        // <summary>
        /// Gets or sets the knowledgebase name.
        /// </summary>
        /// <value>The  knowledgebase name.</value>
        [Display(Name = "QnA knowledgebase name", Description = "QnA knowledgebase name", GroupName = SystemTabNames.Content, Order = 20)]
        [QnaKnowledgebaseName]
        public virtual string QnaKnowledgebaseName { get; set; }

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
```


> *Powered by ReSharper*

> [![image](http://resources.jetbrains.com/assets/media/open-graph/jetbrains_250x250.png)](http://jetbrains.com)