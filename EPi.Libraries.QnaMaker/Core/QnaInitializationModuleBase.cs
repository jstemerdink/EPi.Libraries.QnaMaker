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
namespace EPi.Libraries.QnaMaker.Core
{
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;

    using EPi.Libraries.QnaMaker.Attributes;
    using EPi.Libraries.QnaMaker.Models;

    using EPiServer;
    using EPiServer.Core;
    using EPiServer.DataAbstraction;
    using EPiServer.DataAccess;
    using EPiServer.Framework;
    using EPiServer.Framework.Initialization;
    using EPiServer.Logging;
    using EPiServer.Security;

    /// <summary>
    /// Class QnaInitializationModuleBase.
    /// </summary>
    /// <seealso cref="EPiServer.Framework.IInitializableModule" />
    public abstract class QnaInitializationModuleBase : IInitializableModule
    {
        /// <summary>
        /// Gets the qna API wrapper
        /// </summary>
        protected QnApiWrapper ApiWrapper { get; private set; }

        /// <summary>
        /// Gets the content events
        /// </summary>
        /// <value>The content events.</value>
        protected IContentEvents ContentEvents { get; private set; }

        /// <summary>
        /// Gets the content repository
        /// </summary>
        protected IContentRepository ContentRepository { get; private set; }

        /// <summary>
        /// Gets the content version repository
        /// </summary>
        protected IContentVersionRepository ContentVersionRepository { get; private set; }

        /// <summary>
        /// Gets the logger
        /// </summary>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Gets the content soft link repository
        /// </summary>
        protected IContentSoftLinkRepository SoftLinkRepository { get; private set; }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <remarks>Gets called as part of the EPiServer Framework initialization sequence. Note that it will be called
        /// only once per AppDomain, unless the method throws an exception. If an exception is thrown, the initialization
        /// method will be called repeadetly for each request reaching the site until the method succeeds.</remarks>
        public void Initialize(InitializationEngine context)
        {
            if (context == null)
            {
                return;
            }

            this.ContentRepository = context.Locate.Advanced.GetInstance<IContentRepository>();
            this.SoftLinkRepository = context.Locate.Advanced.GetInstance<IContentSoftLinkRepository>();
            this.ContentVersionRepository = context.Locate.Advanced.GetInstance<IContentVersionRepository>();
            this.ContentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
            this.Logger = context.Locate.Advanced.GetInstance<ILogger>();
            this.ApiWrapper = context.Locate.Advanced.GetInstance<QnApiWrapper>();

            this.ContentEvents.PublishedContent += this.OnPublishedContent;
            this.ContentEvents.PublishingContent += this.OnPublishingContent;
            this.ContentEvents.DeletedContent += this.OnDeletedContent;
            this.ContentEvents.MovingContent += this.OnMovingContent;
        }

        /// <summary>
        /// Resets the module into an uninitialized state.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <remarks><para>
        /// This method is usually not called when running under a web application since the web app may be shut down very
        /// abruptly, but your module should still implement it properly since it will make integration and unit testing
        /// much simpler.
        /// </para>
        /// <para>
        /// Any work done by <see cref="M:EPiServer.Framework.IInitializableModule.Initialize(EPiServer.Framework.Initialization.InitializationEngine)" /> as well as any code executing on <see cref="E:EPiServer.Framework.Initialization.InitializationEngine.InitComplete" /> should be reversed.
        /// </para></remarks>
        public void Uninitialize(InitializationEngine context)
        {
            this.ContentEvents.PublishedContent -= this.OnPublishedContent;
            this.ContentEvents.PublishingContent -= this.OnPublishingContent;
            this.ContentEvents.DeletedContent -= this.OnDeletedContent;
            this.ContentEvents.MovingContent -= this.OnMovingContent;
        }

        /// <summary>
        /// Handles the <see cref="E:DeletedContent" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DeleteContentEventArgs"/> instance containing the event data.</param>
        protected abstract void OnDeletedContent(object sender, DeleteContentEventArgs e);

        /// <summary>
        /// Handles the <see cref="E:MovingContent" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ContentEventArgs"/> instance containing the event data.</param>
        protected abstract void OnMovingContent(object sender, ContentEventArgs e);

        /// <summary>
        /// Handles the <see cref="E:PublishedContent" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EPiServer.ContentEventArgs"/> instance containing the event data.</param>
        protected abstract void OnPublishedContent(object sender, ContentEventArgs e);

        /// <summary>
        /// Handles the <see cref="E:PublishingContent" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ContentEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPublishingContent(object sender, ContentEventArgs e)
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

            if (!contentData.IsOverviewPage())
            {
                return;
            }

            if (!contentData.HasPropertyWithAttribute<QnaContainerAttribute>())
            {
                e.CancelAction = true;
                e.CancelReason = "The page does not contain a property marked with the QnaContainer Attribute.";

                return;
            }

            PropertyInfo knowledgebaseIdProperty = contentData.GetType().GetProperties()
                .FirstOrDefault(info => info.HasAttribute<QnaKnowledgebaseIdAttribute>());

            if (knowledgebaseIdProperty == null)
            {
                e.CancelAction = true;
                e.CancelReason =
                    "The page does not contain a string property marked with the QnaKnowledgebaseId Attribute.";

                return;
            }

            string knowledgebaseId = contentData.KnowledgebaseId();

            // If there is a knowledge base id, there is no need to create a new knowledgebase.
            if (!string.IsNullOrWhiteSpace(knowledgebaseId))
            {
                return;
            }

            string knowledgebaseName = contentData.KnowledgebaseName();

            if (string.IsNullOrWhiteSpace(knowledgebaseName))
            {
                knowledgebaseName = e.Content.Name;
            }

            // Create a new empty knowledgebase.
            CreateKnowledgebaseRequest createKnowledgeBaseRequest = new CreateKnowledgebaseRequest();
            createKnowledgeBaseRequest.Name = knowledgebaseName;
            createKnowledgeBaseRequest.QnaPairs = new QnaPair[0];
            createKnowledgeBaseRequest.Urls = new[] { e.Content.ContentUrl() };

            try
            {
                knowledgebaseId = this.ApiWrapper.CreateQnaKnowledgebase(createKnowledgeBaseRequest);
            }
            catch (HttpRequestException httpRequestException)
            {
                this.Logger.Error(httpRequestException.Message, httpRequestException);

                e.CancelAction = true;
                e.CancelReason =
                    "Unable to create a new knowledge base.";
            }

            // When being "delayed published" the pagedata is readonly. Create a writable clone to be safe.
            ContentData editableContent = contentData.CreateWritableClone() as ContentData;

            if (editableContent != null)
            {
                try
                {
                    editableContent[knowledgebaseIdProperty.Name] = knowledgebaseId;
                }
                catch (EPiServerException epiServerException)
                {
                    this.Logger.Error(epiServerException.Message, epiServerException);

                    e.CancelAction = true;
                    e.CancelReason =
                        string.Format(CultureInfo.InvariantCulture, "Unable to add the knowledge base id to the content: {0}", knowledgebaseId);
                }
            }

            // Save the writable contentData, do not create a new version
            this.ContentRepository.Save(
                (IContent)editableContent,
                SaveAction.Save | SaveAction.ForceCurrentVersion,
                AccessLevel.NoAccess);
        }
    }
}