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
    using System.Configuration;
    using System.Globalization;
    using System.Net.Http;
    using System.Net.Http.Formatting;

    using EPi.Libraries.QnaMaker.Models;

    using EPiServer.Logging;
    using EPiServer.ServiceLocation;

    using Newtonsoft.Json;

    /// <summary>
    /// Class QnApiWrapper.
    /// </summary>
    [ServiceConfiguration(typeof(QnApiWrapper), Lifecycle = ServiceInstanceScope.Singleton)]
    public class QnApiWrapper
    {
        /// <summary>
        /// The base URI
        /// </summary>
        /// <remarks>
        /// Default uri: https://westus.api.cognitive.microsoft.com/qnamaker/v2.0/knowledgebases
        /// </remarks>
        private readonly string baseUri = ConfigurationManager.AppSettings["qna:baseuri"];

        /// <summary>
        /// The HTTP client
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// The log
        /// </summary>
        private readonly ILogger log;

        /// <summary>
        /// The subscription key
        /// </summary>
        private readonly string subscriptionKey = ConfigurationManager.AppSettings["qna:subscriptionkey"];

        /// <summary>
        /// Initializes a new instance of the <see cref="QnApiWrapper" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <exception cref="ConfigurationErrorsException">The appSetting 'qna:subscriptionkey' is empty or not available.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Is OK for a HTTP client instance.")]
        public QnApiWrapper(ILogger log)
        {
            this.log = log;

            if (string.IsNullOrWhiteSpace(this.subscriptionKey))
            {
                throw new ConfigurationErrorsException(
                    "The appSetting 'qna:subscriptionkey' is empty or not available.");
            }

            if (string.IsNullOrWhiteSpace(this.baseUri))
            {
                this.baseUri = "https://westus.api.cognitive.microsoft.com/qnamaker/v2.0/knowledgebases";
            }

            this.httpClient = new HttpClient
                                  {
                                      BaseAddress = new Uri(this.baseUri),
                                      Timeout = TimeSpan.FromMinutes(5),
                                      DefaultRequestHeaders =
                                          {
                                              {
                                                  "Ocp-Apim-Subscription-Key",
                                                  this.subscriptionKey
                                              }
                                          }
                                  };
        }

        /// <summary>
        /// Creates the qna knowledgebase.
        /// </summary>
        /// <param name="createKnowledgeBaseRequest">The create knowledgebase.</param>
        /// <returns>The knowledgebease id.</returns>
        /// <exception cref="HttpRequestException">Failed to create knowledge base.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Is OK for a HttpRequestMessage.")]
        public string CreateQnaKnowledgebase(CreateKnowledgebaseRequest createKnowledgeBaseRequest)
        {
            string uri = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}",
                this.baseUri,
                "create");

            using (ObjectContent<CreateKnowledgebaseRequest> content =
                new ObjectContent<CreateKnowledgebaseRequest>(
                    createKnowledgeBaseRequest,
                    new JsonMediaTypeFormatter()))
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("POST"), uri) { Content = content };

                    HttpResponseMessage response = this.httpClient.SendAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        if (response.Content == null)
                        {
                            return string.Empty;
                        }

                        CreateKnowledgebaseResponse createKnowledgebaseResponse = JsonConvert.DeserializeObject<CreateKnowledgebaseResponse>(response.Content.ReadAsStringAsync().Result);
                        return createKnowledgebaseResponse.KbId;
                    }

                    string detailedReason = null;

                    if (response.Content != null)
                    {
                        detailedReason = response.Content.ReadAsStringAsync().Result;
                    }

                    ErrorInfo errorInfo = detailedReason != null
                                              ? JsonConvert.DeserializeObject<ErrorInfo>(detailedReason)
                                              : new ErrorInfo
                                                    {
                                                        ApiError = new ApiError
                                                                    {
                                                                        Code = string.Empty,
                                                                        Message = response.ReasonPhrase
                                                                    }
                                                    };

                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "[QnA] Error {0}: Failed to create knowledge base, \n reason {1}",
                        errorInfo.ApiError.Code,
                        errorInfo.ApiError.Message);

                    this.log.Error(errorMessage);

                    throw new HttpRequestException(errorMessage);
                }
                catch (Exception exception)
                {
                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "[QnA] Error: Failed to create knowledge base, \n reason {0}",
                        exception.Message);

                    this.log.Error(errorMessage);

                    throw new HttpRequestException(errorMessage, exception);
                }
            }
        }

        /// <summary>
        /// Generates the answer.
        /// </summary>
        /// <param name="answerRequest">The answer request.</param>
        /// <param name="knowledgebaseId">The knowledgebase identifier.</param>
        /// <returns>Ab instance of <see cref="GeneratedAnswer"/>.</returns>
        /// <exception cref="HttpRequestException">Failed to get an answer from  knowledge base</exception>
        public GeneratedAnswer GenerateAnswer(AnswerRequest answerRequest, string knowledgebaseId)
        {
            string uri = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}/generateAnswer",
                this.baseUri,
                knowledgebaseId);

            using (ObjectContent<AnswerRequest> content =
                new ObjectContent<AnswerRequest>(
                    answerRequest,
                    new JsonMediaTypeFormatter()))
            {
                try
                {
                    HttpResponseMessage response = this.httpClient.PostAsync(uri, content).Result;

                    string responseContent = response.Content?.ReadAsStringAsync().Result;

                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<GeneratedAnswer>(responseContent);
                    }

                    ErrorInfo errorInfo = responseContent != null
                                              ? JsonConvert.DeserializeObject<ErrorInfo>(responseContent)
                                              : new ErrorInfo
                                                    {
                                                        ApiError = new ApiError
                                                                    {
                                                                        Code = string.Empty,
                                                                        Message = response.ReasonPhrase
                                                                    }
                                                    };

                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "[QnA] Error {0}: Failed to get an answer from knowledge base with id '{1}', \n reason {2}",
                        errorInfo.ApiError.Code,
                        knowledgebaseId,
                        errorInfo.ApiError.Message);

                    this.log.Error(errorMessage);

                    throw new HttpRequestException(errorMessage);
                }
                catch (Exception exception)
                {
                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "[QnA] Error: Failed to get an answer from  knowledge base with id '{0}', \n reason {1}",
                        knowledgebaseId,
                        exception.Message);

                    this.log.Error(errorMessage);

                    throw new HttpRequestException(errorMessage, exception);
                }
            }
        }

        /// <summary>
        /// Deletes the knowledge base.
        /// </summary>
        /// <param name="knowledgebaseId">The knowledgebase identifier.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="HttpRequestException">Failed to delete knowledge base</exception>
        public bool DeleteKnowledgeBase(string knowledgebaseId)
        {
            if (string.IsNullOrWhiteSpace(knowledgebaseId))
            {
                return false;
            }

            string uri = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}",
                this.baseUri,
                knowledgebaseId);

            try
            {
                HttpResponseMessage response = this.httpClient.DeleteAsync(uri).Result;

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                string detailedReason = null;

                if (response.Content != null)
                {
                    detailedReason = response.Content.ReadAsStringAsync().Result;
                }

                ErrorInfo errorInfo = detailedReason != null
                                          ? JsonConvert.DeserializeObject<ErrorInfo>(detailedReason)
                                          : new ErrorInfo
                                                {
                                                    ApiError = new ApiError
                                                                {
                                                                    Code = string.Empty,
                                                                    Message = response.ReasonPhrase
                                                                }
                                                };

                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "[QnA] Error {0}: Failed to delete knowledge base with id '{1}', \n reason {2}",
                    errorInfo.ApiError.Code,
                    knowledgebaseId,
                    errorInfo.ApiError.Message);

                this.log.Error(errorMessage);

                throw new HttpRequestException(errorMessage);
            }
            catch (Exception exception)
            {
                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "[QnA] Error: Failed to delete knowledge base with id '{0}', \n reason {1}",
                    knowledgebaseId,
                    exception.Message);

                this.log.Error(errorMessage);

                throw new HttpRequestException(errorMessage, exception);
            }
        }

        /// <summary>
        /// Deletes the knowledge base.
        /// </summary>
        /// <param name="knowledgebaseId">The knowledgebase identifier.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="HttpRequestException">Failed to publish knowledge base</exception>
        public bool PublishKnowledgeBase(string knowledgebaseId)
        {
            if (string.IsNullOrWhiteSpace(knowledgebaseId))
            {
                return false;
            }

            string uri = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}",
                this.baseUri,
                knowledgebaseId);

            using (ObjectContent<string> content =
                new ObjectContent<string>(string.Empty, new JsonMediaTypeFormatter()))
            {
                try
                {
                    HttpResponseMessage response = this.httpClient.PutAsync(uri, content).Result;

                    string responseContent = response.Content?.ReadAsStringAsync().Result;

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }

                    ErrorInfo errorInfo = responseContent != null
                                              ? JsonConvert.DeserializeObject<ErrorInfo>(responseContent)
                                              : new ErrorInfo
                                                    {
                                                        ApiError = new ApiError
                                                                    {
                                                                        Code = string.Empty,
                                                                        Message = response.ReasonPhrase
                                                                    }
                                                    };

                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "[QnA] Error {0}: Failed to publish knowledge base with id '{1}', \n reason {2}",
                        errorInfo.ApiError.Code,
                        knowledgebaseId,
                        errorInfo.ApiError.Message);

                    this.log.Error(errorMessage);

                    throw new HttpRequestException(errorMessage);
                }
                catch (Exception exception)
                {
                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "[QnA] Error: Failed to publish knowledge base with id '{0}', \n reason {1}",
                        knowledgebaseId,
                        exception.Message);

                    this.log.Error(errorMessage);

                    throw new HttpRequestException(errorMessage, exception);
                }
            }
        }

        /// <summary>
        /// Updates the qna item.
        /// </summary>
        /// <param name="updateKnowledgebaseRequest">The update knowledgebase.</param>
        /// <param name="knowledgebaseId">The knowledgebase identifier.</param>
        /// <exception cref="HttpRequestException">Failed to add or delete qna item for knowledge base</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Is OK for a HttpRequestMessage.")]
        public void UpdateQnaItem(UpdateKnowledgebaseRequest updateKnowledgebaseRequest, string knowledgebaseId)
        {
            if (string.IsNullOrWhiteSpace(knowledgebaseId))
            {
                return;
            }

            string uri = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}",
                this.baseUri,
                knowledgebaseId);

            using (ObjectContent<UpdateKnowledgebaseRequest> content =
                new ObjectContent<UpdateKnowledgebaseRequest>(
                    updateKnowledgebaseRequest,
                    new JsonMediaTypeFormatter()))
            {
                try
                {
                    HttpRequestMessage request =
                        new HttpRequestMessage(new HttpMethod("PATCH"), uri) { Content = content };

                    HttpResponseMessage response = this.httpClient.SendAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }

                    string detailedReason = null;

                    if (response.Content != null)
                    {
                        detailedReason = response.Content.ReadAsStringAsync().Result;
                    }

                    ErrorInfo errorInfo = detailedReason != null
                                              ? JsonConvert.DeserializeObject<ErrorInfo>(detailedReason)
                                              : new ErrorInfo
                                                    {
                                                        ApiError = new ApiError
                                                                    {
                                                                        Code = string.Empty,
                                                                        Message = response.ReasonPhrase
                                                                    }
                                                    };

                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "[QnA] Error {0}: Failed to add or delete qna item for knowledge base with id '{1}', \n reason {2}",
                        errorInfo.ApiError.Code,
                        knowledgebaseId,
                        errorInfo.ApiError.Message);

                    this.log.Error(errorMessage);

                    throw new HttpRequestException(errorMessage);
                }
                catch (Exception exception)
                {
                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "[QnA] Error: Failed to add or delete qna item for knowledge base with id '{0}', \n reason {1}",
                        knowledgebaseId,
                        exception.Message);

                    this.log.Error(errorMessage);

                    throw new HttpRequestException(errorMessage, exception);
                }
            }
        }

        /// <summary>
        /// Trains the knowledgebase.
        /// </summary>
        /// <param name="feedback">The feedback.</param>
        /// <param name="knowledgebaseId">The knowledgebase identifier.</param>
        /// <exception cref="HttpRequestException">Failed to add feedback for knowledge base.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Is OK for a HttpRequestMessage.")]
        public void TrainKnowledgebase(Feedback feedback, string knowledgebaseId)
        {
            if (string.IsNullOrWhiteSpace(knowledgebaseId))
            {
                return;
            }

            string uri = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}/train",
                this.baseUri,
                knowledgebaseId);

            using (ObjectContent<Feedback> content =
                new ObjectContent<Feedback>(
                    feedback,
                    new JsonMediaTypeFormatter()))
            {
                try
                {
                    string detailedReason = null;
                    ErrorInfo errorInfo;
                    
                    using (HttpRequestMessage request =
                        new HttpRequestMessage(new HttpMethod("PATCH"), uri) { Content = content })
                    {
                        HttpResponseMessage response = this.httpClient.SendAsync(request).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            return;
                        }

                        if (response.Content != null)
                        {
                            detailedReason = response.Content.ReadAsStringAsync().Result;
                        }

                        errorInfo = detailedReason != null
                                                  ? JsonConvert.DeserializeObject<ErrorInfo>(detailedReason)
                                                  : new ErrorInfo
                                                        {
                                                            ApiError = new ApiError
                                                                        {
                                                                            Code = string.Empty,
                                                                            Message = response.ReasonPhrase
                                                                        }
                                                        };
                    }

                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "[QnA] Error {0}: Failed to add feedback for knowledge base with id '{1}', \n reason {2}",
                        errorInfo.ApiError.Code,
                        knowledgebaseId,
                        errorInfo.ApiError.Message);

                    this.log.Error(errorMessage);

                    throw new HttpRequestException(errorMessage);
                }
                catch (Exception exception)
                {
                    string errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "[QnA] Error: Failed to add feedback for knowledge base with id '{0}', \n reason {1}",
                        knowledgebaseId,
                        exception.Message);

                    this.log.Error(errorMessage);
                    
                    throw new HttpRequestException(errorMessage, exception);
                }
            }
        }
    }
}