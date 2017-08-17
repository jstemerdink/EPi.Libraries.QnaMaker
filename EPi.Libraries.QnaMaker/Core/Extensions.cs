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
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using EPi.Libraries.QnaMaker.Attributes;
    using EPi.Libraries.QnaMaker.Models;

    using EPiServer;
    using EPiServer.Core;
    using EPiServer.Core.Html;
    using EPiServer.DataAbstraction;
    using EPiServer.Logging;
    using EPiServer.ServiceLocation;
    using EPiServer.Web;
    using EPiServer.Web.Routing;

    /// <summary>
    /// Class Extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// The content repository
        /// </summary>
        private static IContentRepository contentRepository;

        /// <summary>
        /// The content version repository
        /// </summary>
        private static IContentVersionRepository contentVersionRepository;

        /// <summary>
        /// The URL resolver
        /// </summary>
        private static UrlResolver urlResolver;

        /// <summary>
        /// The logger
        /// </summary>
        private static ILogger logger;

        /// <summary>
        ///     Gets or sets the content repository.
        /// </summary>
        /// <value>The content repository instance.</value>
        /// <exception cref="ActivationException">if there is are errors resolving the service instance.</exception>
        public static IContentRepository ContentRepository
        {
            get
            {
                if (contentRepository != null)
                {
                    return contentRepository;
                }

                contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();

                return contentRepository;
            }

            set => contentRepository = value;
        }

        /// <summary>
        ///     Gets or sets the content version repository.
        /// </summary>
        /// <value>The content version instance.</value>
        /// <exception cref="ActivationException">if there is are errors resolving the service instance.</exception>
        public static IContentVersionRepository ContentVersionRepository
        {
            get
            {
                if (contentVersionRepository != null)
                {
                    return contentVersionRepository;
                }

                contentVersionRepository = ServiceLocator.Current.GetInstance<IContentVersionRepository>();

                return contentVersionRepository;
            }

            set => contentVersionRepository = value;
        }

        /// <summary>
        ///     Gets or sets the URL resolver.
        /// </summary>
        /// <value>The URL resolver instance.</value>
        /// <exception cref="ActivationException">if there is are errors resolving the service instance.</exception>
        public static UrlResolver UrlResolver
        {
            get
            {
                if (urlResolver != null)
                {
                    return urlResolver;
                }

                urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>();

                return urlResolver;
            }

            set => urlResolver = value;
        }

        /// <summary>
        ///     Gets or sets the ILogger.
        /// </summary>
        /// <value>The ILogger instance.</value>
        /// <exception cref="ActivationException">if there is are errors resolving the service instance.</exception>
        public static ILogger Logger
        {
            get
            {
                if (logger != null)
                {
                    return logger;
                }

                logger = ServiceLocator.Current.GetInstance<ILogger>();

                return logger;
            }

            set => logger = value;
        }

        /// <summary>
        /// Determines whether [the content was marked changed].
        /// </summary>
        /// <param name="contentData">The content data.</param>
        /// <returns><c>true</c> if the content was marked changed, <c>false</c> otherwise.</returns>
        public static bool ContentChanged(this ContentData contentData)
        {
            if (contentData == null)
            {
                return false;
            }

            try
            {
                return (bool)(contentData["PageChangedOnPublish"] ?? false);
            }
            catch (EPiServerException epiServerException)
            {
                LogException(exception: epiServerException);
                return false;
            }
        }

        /// <summary>
        /// Gets the content URL.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>The content url.</returns>
        public static string ContentUrl(this IContent content)
        {
            if (content == null)
            {
                return string.Empty;
            }

            try
            {
                return UrlResolver.GetUrl(
                    contentLink: content.ContentLink,
                    language: null,
                    virtualPathArguments: new VirtualPathArguments { ContextMode = ContextMode.Default });
            }
            catch (ActivationException activationException)
            {
                LogException(exception: activationException);
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the knowledgebase identifier.
        /// </summary>
        /// <param name="contentData">The content data.</param>
        /// <returns>The knowledgebase identifier.</returns>
        public static string KnowledgebaseId(this ContentData contentData)
        {
            return contentData == null
                       ? string.Empty
                       : contentData.GetPropertyValue<QnaKnowledgebaseIdAttribute, string>();
        }

        /// <summary>
        /// Gets the knowledgebase name.
        /// </summary>
        /// <param name="contentData">The content data.</param>
        /// <returns>The knowledgebase name.</returns>
        public static string KnowledgebaseName(this ContentData contentData)
        {
            return contentData == null
                       ? string.Empty
                       : contentData.GetPropertyValue<QnaKnowledgebaseNameAttribute, string>();
        }

        /// <summary>
        /// Gets the previous version.
        /// </summary>
        /// <typeparam name="T">The type of content.</typeparam>
        /// <param name="content">The content.</param>
        /// <returns>The previous version of the content.</returns>
        public static T PreviousVersion<T>(this IContent content)
            where T : class
        {
            return content.PreviousVersion() as T;
        }

        /// <summary>
        /// Gets the previous version.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>The previous version of the content.</returns>
        public static IContent PreviousVersion(this IContent content)
        {
            if (content == null)
            {
                return null;
            }

            try
            {
                ContentVersion previousVersion = ContentVersionRepository.List(contentLink: content.ContentLink)
                    .OrderByDescending(x => x.Saved).FirstOrDefault(
                        version => version.IsMasterLanguageBranch
                                   && version.Status == VersionStatus.PreviouslyPublished);

                if (previousVersion == null)
                {
                    return null;
                }
               
                IContent previousContent;
                return ContentRepository.TryGet(contentLink: previousVersion.ContentLink, settings: LanguageSelector.AutoDetect(true), content: out previousContent)
                           ? previousContent
                           : null;
            }
            catch (ActivationException activationException)
            {
                LogException(exception: activationException);
                return null;
            }
            catch (ArgumentNullException argumentNullException)
            {
                LogException(exception: argumentNullException);
                return null;
            }
        }

        /// <summary>
        ///     Gets the property value.
        /// </summary>
        /// <typeparam name="T">The type of the attribute to check for.</typeparam>
        /// <typeparam name="TO">The type of the class to check.</typeparam>
        /// <param name="contentData">The content data.</param>
        /// <returns>An instance of the property.</returns>
        public static TO GetPropertyValue<T, TO>(this ContentData contentData)
            where T : Attribute where TO : class
        {
            if (contentData == null)
            {
                return default(TO);
            }

            PropertyInfo propertyInfo;

            try
            {
                propertyInfo = contentData.GetType().GetProperties().Where(predicate: HasAttribute<T>).FirstOrDefault();
            }
            catch (ArgumentNullException argumentNullException)
            {
                LogException(exception: argumentNullException);
                return default(TO);
            }

            if (propertyInfo == null)
            {
                return default(TO);
            }

            return contentData.GetValue(name: propertyInfo.Name) as TO;
        }

        /// <summary>
        /// Gets the property value.
        /// </summary>
        /// <typeparam name="TO">The type of the class to check.</typeparam>
        /// <param name="propertyInfo">The property information.</param>
        /// <returns>An instance of the property.</returns>
        public static TO GetPropertyValue<TO>(this PropertyInfo propertyInfo)
            where TO : class
        {
            if (propertyInfo == null)
            {
                return default(TO);
            }

            object value = propertyInfo.GetValue(obj: propertyInfo);

            return value as TO;
        }

        /// <summary>
        ///     Gets the property value.
        /// </summary>
        /// <typeparam name="T">The type of the attribute to check for.</typeparam>
        /// <typeparam name="TO">The type of the class to check.</typeparam>
        /// <param name="contentReference">The content reference.</param>
        /// <returns>An instance of the property.</returns>
        public static TO GetPropertyValue<T, TO>(this ContentReference contentReference)
            where T : Attribute where TO : class
        {
            ContentData contentData = null;

            try
            {
                ContentRepository.TryGet(contentLink: contentReference, content: out contentData);
            }
            catch (ActivationException activationException)
            {
                LogException(exception: activationException);
            }

            return contentData == null ? default(TO) : GetPropertyValue<T, TO>(contentData: contentData);
        }

        /// <summary>
        /// Gets the qna pair.
        /// </summary>
        /// <param name="contentData">The content data.</param>
        /// <returns>A QnaPair based on the provided <paramref name="contentData"></paramref>.</returns>
        public static QnaPair GetQnaPair(this ContentData contentData)
        {
            if (!contentData.IsQnaItem())
            {
                return null;
            }

            string question = contentData.GetPropertyValue<QnaQuestionAttribute, string>();

            if (string.IsNullOrWhiteSpace(value: question))
            {
                return null;
            }

            string answer = contentData.GetPropertyValue<QnaAnswerAttribute, string>();

            if (string.IsNullOrWhiteSpace(value: answer))
            {
                answer = TextIndexer.StripHtml(
                    contentData.GetPropertyValue<QnaAnswerAttribute, XhtmlString>()?.ToHtmlString() ?? string.Empty,
                    0);
            }

            return string.IsNullOrWhiteSpace(value: answer)
                       ? null
                       : new QnaPair { Question = question, Answer = answer };
        }

        /// <summary>
        /// Gets the qna pairs.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>A List of <see cref="QnaPair" />.</returns>
        public static ReadOnlyCollection<QnaPair> GetQnaPairs(this IEnumerable<IContent> items)
        {
            List<QnaPair> qnaPairs = new List<QnaPair>();

            if (items == null)
            {
                return new ReadOnlyCollection<QnaPair>(qnaPairs);
            }

            foreach (IContent contentItem in items)
            {
                if (contentItem == null)
                {
                    continue;
                }

                ContentData contentData = contentItem as ContentData;

                if (!contentData.IsQnaItem())
                {
                    continue;
                }

                QnaPair qnaPair = contentData.GetQnaPair();

                if (qnaPair == null)
                {
                    continue;
                }

                qnaPairs.Add(item: qnaPair);
            }

            return new ReadOnlyCollection<QnaPair>(qnaPairs);
        }

        /// <summary>
        /// Gets the qna pairs.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>A List of <see cref="QnaPair" />.</returns>
        public static ReadOnlyCollection<QnaPair> GetQnaPairs(this IEnumerable<ContentReference> items)
        {
            List<IContent> contentList = new List<IContent>();

            if (items == null)
            {
                return new ReadOnlyCollection<QnaPair>(new List<QnaPair>());
            }

            foreach (ContentReference contentReference in items)
            {
                if (ContentReference.IsNullOrEmpty(contentLink: contentReference))
                {
                    continue;
                }

                try
                {
                    IContent content;
                    if (!ContentRepository.TryGet(contentLink: contentReference, content: out content))
                    {
                        continue;
                    }

                    contentList.Add(item: content);
                }
                catch (ActivationException activationException)
                {
                    LogException(exception: activationException);
                }
            }

            return contentList.GetQnaPairs();
        }

        /// <summary>
        ///     Determines whether [the specified property] has [the specified attribute].
        /// </summary>
        /// <typeparam name="T">The type of the attribute to check for.</typeparam>
        /// <param name="propertyInfo">The propertyInfo.</param>
        /// <returns><c>true</c> if [the specified property] has [the specified attribute]; otherwise, <c>false</c>.</returns>
        public static bool HasAttribute<T>(this PropertyInfo propertyInfo)
            where T : Attribute
        {
            T attr = default(T);

            try
            {
                attr = (T)Attribute.GetCustomAttribute(element: propertyInfo, attributeType: typeof(T));
            }
            catch (Exception exception)
            {
                LogException(exception: exception);
            }

            return attr != null;
        }

        /// <summary>
        ///     Determines whether [the specified member information] has [the specified attribute].
        /// </summary>
        /// <typeparam name="T">he type of the attribute to check for.</typeparam>
        /// <param name="memberInfo">The member information.</param>
        /// <returns><c>true</c> if [the specified member information] has [the specified attribute]; otherwise, <c>false</c>.</returns>
        public static bool HasAttribute<T>(this MemberInfo memberInfo)
            where T : Attribute
        {
            T attr = default(T);

            try
            {
                attr = (T)Attribute.GetCustomAttribute(element: memberInfo, attributeType: typeof(T));
            }
            catch (Exception exception)
            {
                LogException(exception: exception);
            }

            return attr != null;
        }

        /// <summary>
        /// Determines whether [the specified member information] has a [property with he specified attribute].
        /// </summary>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <param name="memberInfo">The member information.</param>
        /// <returns><c>true</c> if [the specified member information] has a [property with he specified attribute]; otherwise, <c>false</c>.</returns>
        public static bool HasPropertyWithAttribute<T>(this MemberInfo memberInfo)
            where T : Attribute
        {
            return memberInfo != null && memberInfo.GetType().GetProperties().Any(predicate: HasAttribute<T>);
        }

        /// <summary>
        /// Determines whether [the specified content data] has a [property with attribute].
        /// </summary>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <param name="contentData">The member information.</param>
        /// <returns><c>true</c> if [the specified content data] has a [property with attribute]; otherwise, <c>false</c>.</returns>
        public static bool HasPropertyWithAttribute<T>(this ContentData contentData)
            where T : Attribute
        {
            return contentData != null && contentData.GetType().GetProperties().Any(predicate: HasAttribute<T>);
        }

        /// <summary>
        ///     Determines whether [the specified content data] is an overview page.
        /// </summary>
        /// <param name="contentData">The content data.</param>
        /// <returns><c>true</c> if the [specified content data] is an overview page; otherwise, <c>false</c>.</returns>
        public static bool IsOverviewPage(this ContentData contentData)
        {
            return contentData != null && contentData.GetOriginalType().HasAttribute<QnaOverviewPageAttribute>();
        }

        /// <summary>
        ///  Determines whether [the specified content data] is a QnA item.
        /// </summary>
        /// <param name="contentData">The content data.</param>
        /// <returns><c>true</c> if [the specified content data] is a QnA item; otherwise, <c>false</c>.</returns>
        public static bool IsQnaItem(this ContentData contentData)
        {
            return contentData != null && contentData.GetOriginalType().HasAttribute<QnaItemAttribute>();
        }

        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        private static void LogException(Exception exception)
        {
            try
            {
                Logger.Error(string.Format(CultureInfo.InvariantCulture, "[QnA]: {0}", exception.Message), exception: exception);
            }
            catch (ActivationException)
            {
                // If there is no logger instance, we can't log.
            }
        }
    }
}