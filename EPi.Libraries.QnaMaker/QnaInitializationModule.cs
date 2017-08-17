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

namespace EPi.Libraries.QnaMaker
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using EPi.Libraries.QnaMaker.Attributes;
    using EPi.Libraries.QnaMaker.Core;
    using EPi.Libraries.QnaMaker.Models;

    using EPiServer;
    using EPiServer.Core;
    using EPiServer.Core.Html;
    using EPiServer.DataAbstraction;
    using EPiServer.DataAccess;
    using EPiServer.Framework;
    using EPiServer.Framework.Initialization;
    using EPiServer.Logging;
    using EPiServer.Security;
    using EPiServer.Web;
    using EPiServer.Web.Routing;

    /// <summary>
    /// Class QnaInitializationModule.
    /// </summary>
    /// <seealso cref="EPiServer.Framework.IInitializableModule" />
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class QnaInitializationModule : QnaInitializationModuleBase////, IInitializableModule
    {
        /////// <summary>
        /////// The content events
        /////// </summary>
        ////private IContentEvents contentEvents;

        /////// <summary>
        /////// The content repository
        /////// </summary>
        ////private IContentRepository contentRepository;

        /////// <summary>
        /////// The content soft link repository
        /////// </summary>
        ////private IContentSoftLinkRepository contentSoftLinkRepository;

        /////// <summary>
        /////// The content version repository
        /////// </summary>
        ////private IContentVersionRepository contentVersionRepository;

        /////// <summary>
        /////// The logger
        /////// </summary>
        ////private ILogger logger;

        /////// <summary>
        /////// The qn API wrapper
        /////// </summary>
        ////private QnApiWrapper qnApiWrapper;

        /////// <summary>
        /////// Initializes this instance.
        /////// </summary>
        /////// <param name="context">The context.</param>
        /////// <remarks>Gets called as part of the EPiServer Framework initialization sequence. Note that it will be called
        /////// only once per AppDomain, unless the method throws an exception. If an exception is thrown, the initialization
        /////// method will be called repeadetly for each request reaching the site until the method succeeds.</remarks>
        ////public void Initialize(InitializationEngine context)
        ////{
        ////    this.contentRepository = context.Locate.Advanced.GetInstance<IContentRepository>();
        ////    this.contentSoftLinkRepository = context.Locate.Advanced.GetInstance<IContentSoftLinkRepository>();
        ////    this.contentVersionRepository = context.Locate.Advanced.GetInstance<IContentVersionRepository>();
        ////    this.contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
        ////    this.logger = context.Locate.Advanced.GetInstance<ILogger>();
        ////    this.qnApiWrapper = context.Locate.Advanced.GetInstance<QnApiWrapper>();

        ////    this.contentEvents.PublishedContent += this.OnPublishedContent;
        ////    this.contentEvents.PublishingContent += this.OnPublishingContent;
        ////    this.contentEvents.DeletedContent += this.OnDeletedContent;
        ////    this.contentEvents.MovingContent += this.OnMovingContent;
        ////}

        /////// <summary>
        /////// Resets the module into an uninitialized state.
        /////// </summary>
        /////// <param name="context">The context.</param>
        /////// <remarks><para>
        /////// This method is usually not called when running under a web application since the web app may be shut down very
        /////// abruptly, but your module should still implement it properly since it will make integration and unit testing
        /////// much simpler.
        /////// </para>
        /////// <para>
        /////// Any work done by <see cref="M:EPiServer.Framework.IInitializableModule.Initialize(EPiServer.Framework.Initialization.InitializationEngine)" /> as well as any code executing on <see cref="E:EPiServer.Framework.Initialization.InitializationEngine.InitComplete" /> should be reversed.
        /////// </para></remarks>
        ////public void Uninitialize(InitializationEngine context)
        ////{
        ////    this.contentEvents.PublishedContent -= this.OnPublishedContent;
        ////    this.contentEvents.PublishingContent -= this.OnPublishingContent;
        ////    this.contentEvents.DeletedContent -= this.OnDeletedContent;
        ////    this.contentEvents.MovingContent -= this.OnMovingContent;
        ////}

        /// <summary>
        /// Gets the knowledgebase identifier list.
        /// </summary>
        /// <param name="contentLink">The content link.</param>
        /// <returns>A list if Ids.</returns>
        protected List<IContent> GetKnowledgebaseIdList(ContentReference contentLink)
        {
            List<IContent> knowledgebaseIdList = new List<IContent>();

            // Get the references to this block
            List<ContentReference> referencingContentLinks = this.SoftLinkRepository
                .Load(contentLink: contentLink, reversed: true)
                .Where(
                    link => (link.SoftLinkType == ReferenceType.PageLinkReference)
                            && !ContentReference.IsNullOrEmpty(contentLink: link.OwnerContentLink))
                .Select(link => link.OwnerContentLink).ToList();

            foreach (ContentReference referencingContentLink in referencingContentLinks)
            {
                IContent parent;
                this.ContentRepository.TryGet(contentLink: referencingContentLink, content: out parent);

                if (parent == null)
                {
                    continue;
                }

                ContentData parentContentData = parent as ContentData;

                if (!parentContentData.IsOverviewPage())
                {
                    continue;
                }

                string knowledgebaseId =
                    parentContentData.GetPropertyValue<QnaKnowledgebaseIdAttribute, string>();

                if (!string.IsNullOrWhiteSpace(value: knowledgebaseId))
                {
                    knowledgebaseIdList.Add(item: parent);
                }
            }

            return knowledgebaseIdList;
        }

        /// <summary>
        /// Gets the qna pairs.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>A List of <see cref="QnaPair" />.</returns>
        protected List<QnaPair> GetQnaPairs(IEnumerable<ContentAreaItem> items)
        {
            List<IContent> contentList = new List<IContent>();

            foreach (ContentAreaItem contentAreaItem in items)
            {
                IContent content;
                if (!this.ContentRepository.TryGet(contentLink: contentAreaItem.ContentLink, content: out content))
                {
                    continue;
                }

                // content area item can be null when duplicating a page
                if (content == null)
                {
                    continue;
                }

                contentList.Add(content);
            }

            return contentList.GetQnaPairs();
        }

        /// <summary>
        /// Handles the <see cref="E:MovingContent" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ContentEventArgs"/> instance containing the event data.</param>
        protected override void OnMovingContent(object sender, ContentEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            ContentData contentData = e.Content as ContentData;

            if (contentData == null)
            {
                return;
            }

            if (e.TargetLink.ID != 2)
            {
                return;
            }

            if (contentData.IsQnaItem())
            {
                QnaPair qnaPair = contentData.GetQnaPair();

                if (qnaPair == null)
                {
                    return;
                }

                // Delete the QnA item
                UpdateKnowledgebase updateKnowledgebase = new UpdateKnowledgebase();
                ItemsToDelete itemsToDelete = new ItemsToDelete();
                itemsToDelete.QnaPairs = new[] { qnaPair };

                updateKnowledgebase.Delete = itemsToDelete;

                // Delete it from every knowledgebase
                List<IContent> overviewPageList = this.GetKnowledgebaseIdList(contentLink: e.ContentLink);

                foreach (IContent page in overviewPageList)
                {
                    string knowledgebaseId = (page as ContentData).GetKnowledgebaseId();

                    if (!string.IsNullOrWhiteSpace(value: knowledgebaseId))
                    {
                        this.ApiWrapper.UpdateQnaItem(
                            updateKnowledgebase: updateKnowledgebase,
                            knowledgebaseId: knowledgebaseId);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="E:DeletedContent" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DeleteContentEventArgs"/> instance containing the event data.</param>
        protected override void OnDeletedContent(object sender, DeleteContentEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            ContentData contentData = e.Content as ContentData;

            if (contentData == null)
            {
                return;
            }

            if (contentData.IsOverviewPage())
            {
                string knowledgebaseId = contentData.GetKnowledgebaseId();

                if (!string.IsNullOrWhiteSpace(value: knowledgebaseId))
                {
                    this.ApiWrapper.DeleteKnowledgeBase(knowledgebaseId: knowledgebaseId);
                }

                return;
            }

            if (contentData.IsQnaItem())
            {
                QnaPair qnaPair = contentData.GetQnaPair();

                if (qnaPair == null)
                {
                    return;
                }

                // Add the QnA item
                UpdateKnowledgebase updateKnowledgebase = new UpdateKnowledgebase();
                ItemsToDelete itemsToDelete = new ItemsToDelete();
                itemsToDelete.QnaPairs = new[] { qnaPair };

                updateKnowledgebase.Delete = itemsToDelete;

                // Delete it from every knowledgebase
                List<IContent> overviewPageList = this.GetKnowledgebaseIdList(contentLink: e.ContentLink);

                foreach (IContent page in overviewPageList)
                {
                    string knowledgebaseId = (page as ContentData).GetKnowledgebaseId();

                    if (!string.IsNullOrWhiteSpace(value: knowledgebaseId))
                    {
                        this.ApiWrapper.UpdateQnaItem(
                            updateKnowledgebase: updateKnowledgebase,
                            knowledgebaseId: knowledgebaseId);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="E:PublishedContent" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EPiServer.ContentEventArgs"/> instance containing the event data.</param>
        protected override void OnPublishedContent(object sender, ContentEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            ContentData contentData = e.Content as ContentData;
            
            if (contentData == null)
            {
                return;
            }

            if (contentData.IsOverviewPage())
            {
                string knowledgebaseId = contentData.GetKnowledgebaseId(); 

                if (!string.IsNullOrWhiteSpace(value: knowledgebaseId))
                {
                    return;
                }

                ContentArea contentArea =
                    contentData.GetPropertyValue<QnaContainerAttribute, ContentArea>();

                if (contentArea == null)
                {
                    return;
                }

                List<ContentAreaItem> previousItems = new List<ContentAreaItem>();
                List<ContentAreaItem> currentItems = contentArea.Items.ToList();

                ContentVersion previousVersion = this.ContentVersionRepository.List(e.Content.ContentLink).OrderByDescending(x => x.Saved).FirstOrDefault(version => version.IsMasterLanguageBranch && version.Status == VersionStatus.PreviouslyPublished);

                if (previousVersion != null)
                {
                    ContentData previousContent = e.Content.GetPreviousVersion<ContentData>();
                    ContentArea previousContentArea = previousContent.GetPropertyValue<QnaContainerAttribute, ContentArea>();

                    if (previousContentArea != null)
                    {
                        previousItems = previousContentArea.Items.ToList();
                    }

                    ////if (this.ContentRepository.TryGet(contentLink: previousVersion.ContentLink, content: out previousContent))
                    ////{
                    ////    ContentArea previousContentArea = previousContent.GetPropertyValue<QnaContainerAttribute, ContentArea>();

                    ////    if (previousContentArea != null)
                    ////    {
                    ////        previousItems = previousContentArea.Items.ToList();
                    ////    }
                    ////}
                }

                List<ContentAreaItem> contentAreaItemsToAdd = currentItems.Where(contentAreaItem => !previousItems.Select(p => p.ContentGuid).Contains(contentAreaItem.ContentGuid)).ToList();

                List<ContentAreaItem> contentAreaItemsToDelete = previousItems.Where(contentAreaItem => !currentItems.Select(p => p.ContentGuid).Contains(contentAreaItem.ContentGuid)).ToList();

                UpdateKnowledgebase updateKnowledgebase = new UpdateKnowledgebase();
                ItemsToAdd itemsToAdd = new ItemsToAdd();
                List<QnaPair> qnaPairsToAdd = this.GetQnaPairs(contentAreaItemsToAdd);
                itemsToAdd.QnaPairs = qnaPairsToAdd.ToArray();
                itemsToAdd.Urls = new[] { e.Content.GetContentUrl() };
                updateKnowledgebase.Add = itemsToAdd;

                ItemsToDelete itemsToDelete = new ItemsToDelete();
                List<QnaPair> qnaPairsToDelete = this.GetQnaPairs(contentAreaItemsToDelete);
                itemsToDelete.QnaPairs = qnaPairsToDelete.ToArray();
                updateKnowledgebase.Delete = itemsToDelete;

                this.ApiWrapper.UpdateQnaItem(
                    updateKnowledgebase: updateKnowledgebase,
                    knowledgebaseId: knowledgebaseId);

                if (contentData.ContentChanged())
                {
                    this.ApiWrapper.PublishKnowledgeBase(knowledgebaseId);
                }

                return;
            }

            if (contentData.IsQnaItem())
            {
                QnaPair qnaPair = contentData.GetQnaPair();

                if (qnaPair == null)
                {
                    return;
                }

                List<IContent> overviewPageList = this.GetKnowledgebaseIdList(contentLink: e.ContentLink);

                if (overviewPageList.Count <= 0)
                {
                    return;
                }

                // Add the QnA item
                UpdateKnowledgebase updateKnowledgebase = new UpdateKnowledgebase();
                ItemsToAdd itemsToAdd = new ItemsToAdd();
                itemsToAdd.QnaPairs = new[] { qnaPair };

                itemsToAdd.Urls = overviewPageList.Select(page => page.GetContentUrl()).ToArray();

                updateKnowledgebase.Add = itemsToAdd;

                foreach (IContent page in overviewPageList)
                {
                    string knowledgebaseId = (page as ContentData).GetKnowledgebaseId(); 

                    if (!string.IsNullOrWhiteSpace(value: knowledgebaseId))
                    {
                        this.ApiWrapper.UpdateQnaItem(
                            updateKnowledgebase: updateKnowledgebase,
                            knowledgebaseId: knowledgebaseId);
                    }
                }
            }
        }

        /////// <summary>
        /////// Handles the <see cref="E:PublishingContent" /> event.
        /////// </summary>
        /////// <param name="sender">The sender.</param>
        /////// <param name="e">The <see cref="ContentEventArgs"/> instance containing the event data.</param>
        ////protected void OnPublishingContent(object sender, ContentEventArgs e)
        ////{
        ////    if (e == null)
        ////    {
        ////        return;
        ////    }

        ////    ContentData contentData = e.Content as ContentData;

        ////    if (contentData == null)
        ////    {
        ////        return;
        ////    }

        ////    if (!contentData.IsOverviewPage())
        ////    {
        ////        return;
        ////    }

        ////    ContentArea contentArea =
        ////        contentData.GetPropertyValue<QnaContainerAttribute, ContentArea>();

        ////    if (contentArea == null)
        ////    {
        ////        e.CancelAction = true;
        ////        e.CancelReason = "The page does not contain a ContentArea marked with the QnaContainer Attribute.";

        ////        return;
        ////    }

        ////    PropertyInfo knowledgebaseIdProperty = contentData.GetType().GetProperties()
        ////        .Where(predicate: Extensions.HasAttribute<QnaKnowledgebaseIdAttribute>).FirstOrDefault();

        ////    if (knowledgebaseIdProperty == null)
        ////    {
        ////        e.CancelAction = true;
        ////        e.CancelReason =
        ////            "The page does not contain a string property marked with the QnaKnowledgebaseId Attribute.";

        ////        return;
        ////    }

        ////    string knowledgebaseId = contentData.GetPropertyValue<QnaKnowledgebaseIdAttribute, string>();

        ////    // If there is no knowledge base id, create one
        ////    if (string.IsNullOrWhiteSpace(value: knowledgebaseId))
        ////    {
        ////        string knowledgebaseName =
        ////            contentData.GetPropertyValue<QnaKnowledgebaseNameAttribute, string>();

        ////        if (string.IsNullOrWhiteSpace(value: knowledgebaseName))
        ////        {
        ////            knowledgebaseName = e.Content.Name;
        ////        }

        ////        CreateKnowledgebase createKnowledgebase = new CreateKnowledgebase();
        ////        createKnowledgebase.Name = knowledgebaseName;
        ////        createKnowledgebase.QnaPairs = new QnaPair[0];
        ////        createKnowledgebase.Urls = new[] { e.Content.GetContentUrl() };

        ////        knowledgebaseId =
        ////            this.qnApiWrapper.CreateQnaKnowledgebase(createKnowledgebase: createKnowledgebase);

        ////        // When being "delayed published" the pagedata is readonly. Create a writable clone to be safe.
        ////        ContentData editableContent = contentData.CreateWritableClone() as ContentData;

        ////        if (editableContent != null)
        ////        {
        ////            editableContent[index: knowledgebaseIdProperty.Name] = knowledgebaseId;
        ////        }

        ////        // Save the writable contentData, do not create a new version
        ////        this.contentRepository.Save(
        ////            (IContent)editableContent,
        ////            SaveAction.Save | SaveAction.ForceCurrentVersion,
        ////            access: AccessLevel.NoAccess);
        ////    }
        ////}
    }
}