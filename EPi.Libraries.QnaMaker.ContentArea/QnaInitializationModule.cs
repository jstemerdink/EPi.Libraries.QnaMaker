﻿// Copyright © 2017 Jeroen Stemerdink.
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

namespace EPi.Libraries.QnaMaker.ContentArea
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
   
    using EPi.Libraries.QnaMaker.Attributes;
    using EPi.Libraries.QnaMaker.Core;
    using EPi.Libraries.QnaMaker.Models;

    using EPiServer;
    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.Framework;
    using EPiServer.Logging;

    /// <summary>
    /// Class QnaInitializationModule.
    /// </summary>
    /// <seealso cref="EPiServer.Framework.IInitializableModule" />
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class QnaInitializationModule : QnaInitializationModuleBase
    {
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

            this.Logger.Log(Level.Debug, "[QnA Maker] Moving content.");

            if (e.TargetLink.ID != 2)
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
                this.Logger.Log(Level.Debug, "[QnA Maker] Content is overview page.");

                this.DeleteKnowledgeBase(contentData);

                return;
            }

            if (contentData.IsQnaItem())
            {
                this.Logger.Log(Level.Debug, "[QnA Maker] Updating knowledgebase: deleting item(s), because moved to trash.");

                // Delete the QnA item
                this.DeleteQnaPair(contentData, e.ContentLink);
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

            this.Logger.Log(Level.Debug, "[QnA Maker] Published content.");

            ContentData contentData = e.Content as ContentData;

            if (contentData == null)
            {
                return;
            }

            if (contentData.IsOverviewPage())
            {
                this.Logger.Log(Level.Debug, "[QnA Maker] Content is overview page.");

                this.UpdateKnowlegdebase(e.Content);

                return;
            }

            if (contentData.IsQnaItem())
            {
                this.UpdateQnaPairs(contentData, e.ContentLink);
            }
        }

        /// <summary>
        /// Gets the qna pairs.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>A List of <see cref="QnaPair" />.</returns>
        private static ReadOnlyCollection<QnaPair> GetQnaPairs(IEnumerable<ContentAreaItem> items)
        {
            return items.Select(i => i.ContentLink).GetQnaPairs();
        }

        /// <summary>
        /// Updates the knowlegdebase.
        /// </summary>
        /// <param name="content">The content.</param>
        private void UpdateKnowlegdebase(IContent content)
        {
            ContentData contentData = content as ContentData;

            if (contentData == null)
            {
                return;
            }

            string knowledgebaseId = contentData.KnowledgebaseId();

            if (string.IsNullOrWhiteSpace(value: knowledgebaseId))
            {
                this.Logger.Log(Level.Debug, "[QnA Maker] Content has no knowledgebase id.");
                return;
            }

            ContentArea contentArea = contentData.GetPropertyValue<QnaContainerAttribute, ContentArea>();

            if (contentArea == null)
            {
                return;
            }

            List<ContentAreaItem> previousItems = new List<ContentAreaItem>();
            List<ContentAreaItem> currentItems = contentArea.Items.ToList();

            ContentData previousContent = content.PreviousVersion<ContentData>();

            if (previousContent != null)
            {
                ContentArea previousContentArea = previousContent.GetPropertyValue<QnaContainerAttribute, ContentArea>();

                if (previousContentArea != null)
                {
                    previousItems = previousContentArea.Items.ToList();
                }
            }

            this.Logger.Log(
                Level.Debug,
                "[QnA Maker] Adding items to add from the knowledgebase with id: {0}",
                knowledgebaseId);

            List<ContentAreaItem> contentAreaItemsToAdd = currentItems
                .Where(
                    contentAreaItem => !previousItems.Select(p => p.ContentGuid).Contains(value: contentAreaItem.ContentGuid))
                .ToList();

            this.Logger.Log(
                Level.Debug,
                "[QnA Maker] Adding items to delete from the knowledgebase with id: {0}",
                knowledgebaseId);

            List<ContentAreaItem> contentAreaItemsToDelete = previousItems
                .Where(contentAreaItem => !currentItems.Select(p => p.ContentGuid).Contains(value: contentAreaItem.ContentGuid))
                .ToList();

            UpdateKnowledgebaseRequest updateKnowledgebaseRequest = new UpdateKnowledgebaseRequest();

            ItemsToAdd itemsToAdd = new ItemsToAdd();
            ReadOnlyCollection<QnaPair> qnaPairsToAdd = GetQnaPairs(items: contentAreaItemsToAdd);
            itemsToAdd.QnaPairs = qnaPairsToAdd.ToArray();

            if (this.IncludeUrl)
            {
                itemsToAdd.Urls = new[] { content.ContentUrl() };
            }

            updateKnowledgebaseRequest.Add = itemsToAdd;

            ItemsToDelete itemsToDelete = new ItemsToDelete();
            ReadOnlyCollection<QnaPair> qnaPairsToDelete = GetQnaPairs(items: contentAreaItemsToDelete);
            itemsToDelete.QnaPairs = qnaPairsToDelete.ToArray();
            updateKnowledgebaseRequest.Delete = itemsToDelete;

            this.Logger.Log(Level.Debug, "[QnA Maker] Updating the knowledgebase with id: {0}", knowledgebaseId);

            this.ApiWrapper.UpdateQnaItem(
                updateKnowledgebaseRequest: updateKnowledgebaseRequest,
                knowledgebaseId: knowledgebaseId);

            if (!contentData.ContentChanged())
            {
                return;
            }

            this.Logger.Log(
                Level.Debug,
                "[QnA Maker] Publishing the knowledgebase with id: {0}, because it was marked as changed.",
                knowledgebaseId);

            this.ApiWrapper.PublishKnowledgeBase(knowledgebaseId: knowledgebaseId);
        }

        /// <summary>
        /// Deletes the qna pair.
        /// </summary>
        /// <param name="contentData">The content data.</param>
        /// <param name="contentReference">The content reference.</param>
        private void DeleteQnaPair(ContentData contentData, ContentReference contentReference)
        {
            QnaPair qnaPair = contentData.GetQnaPair();

            if (qnaPair == null)
            {
                return;
            }
            
            UpdateKnowledgebaseRequest updateKnowledgebaseRequest = new UpdateKnowledgebaseRequest();
            ItemsToDelete itemsToDelete = new ItemsToDelete { QnaPairs = new[] { qnaPair } };

            updateKnowledgebaseRequest.Delete = itemsToDelete;

                // Delete it from every knowledgebase
                List<IContent> overviewPageList = this.GetOverviewPages(contentLink: contentReference);

                foreach (IContent page in overviewPageList)
                {
                    string knowledgebaseId = (page as ContentData).KnowledgebaseId();

                    if (string.IsNullOrWhiteSpace(value: knowledgebaseId))
                    {
                        continue;
                    }

                    this.Logger.Log(Level.Debug, "[QnA Maker] Deleting item from the knowledgebase with id: {0}", knowledgebaseId);

                    this.ApiWrapper.UpdateQnaItem(
                        updateKnowledgebaseRequest: updateKnowledgebaseRequest,
                        knowledgebaseId: knowledgebaseId);
                }
        }

        /// <summary>
        /// Updates the qna pairs.
        /// </summary>
        /// <param name="contentData">The content data.</param>
        /// <param name="contentReference">The content reference.</param>
        private void UpdateQnaPairs(ContentData contentData, ContentReference contentReference)
        {
            QnaPair qnaPair = contentData.GetQnaPair();

            if (qnaPair == null)
            {
                return;
            }

            List<IContent> overviewPageList = this.GetOverviewPages(contentLink: contentReference);

            if (overviewPageList.Count <= 0)
            {
                return;
            }

            ContentData previousContent = contentReference.PreviousVersion<ContentData>();
            QnaPair previousQnaPair = previousContent.GetQnaPair();

            UpdateKnowledgebaseRequest updateKnowledgebaseRequest = new UpdateKnowledgebaseRequest();

            // If the question has changed, delete the old version. qna is based on question.
            if (!qnaPair.Question.Equals(previousQnaPair.Question))
            {
                ItemsToDelete itemsToDelete = new ItemsToDelete { QnaPairs = new[] { previousQnaPair } };

                updateKnowledgebaseRequest.Delete = itemsToDelete;
            }

            // Add the QnA item
            ItemsToAdd itemsToAdd = new ItemsToAdd
                                        {
                                            QnaPairs = new[] { qnaPair },
                                            Urls = overviewPageList.Select(page => page.ContentUrl())
                                                .ToArray()
                                        };

            updateKnowledgebaseRequest.Add = itemsToAdd;

            foreach (IContent page in overviewPageList)
            {
                string knowledgebaseId = (page as ContentData).KnowledgebaseId();

                if (!string.IsNullOrWhiteSpace(value: knowledgebaseId))
                {
                    this.ApiWrapper.UpdateQnaItem(
                        updateKnowledgebaseRequest: updateKnowledgebaseRequest,
                        knowledgebaseId: knowledgebaseId);
                }
            }
        }

        /// <summary>
        /// Gets the knowledgebase identifier list.
        /// </summary>
        /// <param name="contentLink">The content link.</param>
        /// <returns>A list of <see cref="IContent"/>.</returns>
        private List<IContent> GetOverviewPages(ContentReference contentLink)
        {
            List<IContent> knowledgebaseIdList = new List<IContent>();

            // Get the references to this block
            List<ContentReference> referencingContentLinks = this.SoftLinkRepository
                .Load(contentLink: contentLink, reversed: true)
                .Where(
                    link => link.SoftLinkType == ReferenceType.PageLinkReference
                            && !ContentReference.IsNullOrEmpty(contentLink: link.OwnerContentLink))
                .Select(link => link.OwnerContentLink).ToList();

            foreach (ContentReference referencingContentLink in referencingContentLinks)
            {
                IContent parent;
                if (!this.ContentRepository.TryGet(contentLink: referencingContentLink, content: out parent))
                {
                    continue;
                }

                ContentData parentContentData = parent as ContentData;

                if (!parentContentData.IsOverviewPage())
                {
                    continue;
                }

                string knowledgebaseId = parentContentData.KnowledgebaseId();

                if (!string.IsNullOrWhiteSpace(value: knowledgebaseId))
                {
                    knowledgebaseIdList.Add(item: parent);
                }
            }

            return knowledgebaseIdList;
        }
    }
}