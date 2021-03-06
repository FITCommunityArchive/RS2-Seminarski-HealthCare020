﻿using Flurl.Http;
using Healthcare020.Mobile.Interfaces;
using Healthcare020.Mobile.Resources;
using HealthCare020.Core.Extensions;
using HealthCare020.Core.ResponseModels;
using HealthCare020.Core.ServiceModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

#pragma warning disable 168

namespace Healthcare020.Mobile.Services
{
    public class APIService : IAPIService
    {
        private readonly string BaseUrl;
        private readonly IFlurlRequest request;

        /// <summary>
        /// Create new API service with specific route
        /// </summary>
        /// <param name="route">Specific route </param>
        public APIService(string route = "")
        {
            try
            {
#if  DEBUG
                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain,
                        errors) => true
                };
#endif

                var httpClient = new HttpClient(httpClientHandler)
                {
                    BaseAddress = new Uri(Device.RuntimePlatform == Device.Android
                        ? AppResources.ApiUrlAndroid
                        : AppResources.ApiUrl)
                };

                IFlurlClient flurlClient = new FlurlClient(httpClient);
                request = flurlClient.Request(route).AllowAnyHttpStatus();
                if (Auth.IsAuthenticated(setLoginPage: false))
                    request.Headers.Add("Authorization", $"Bearer {Auth.AccessToken.ConvertToString()}");
                BaseUrl = request.Url;
            }
#pragma warning disable 168
            catch (Exception ex)
#pragma warning restore 168
            {
                // ignored
            }
        }

        /// <summary>
        /// Add route to the base request for the API. The route will be removed after the first request.
        /// </summary>
        public void AddRoute(string route)
        {
            request.Url.AppendPathSegments(route);
        }

        public void ChangeRoute(string route)
        {
            request.Url = Device.RuntimePlatform == Device.Android
                ? AppResources.ApiUrlAndroid
                : AppResources.ApiUrl;
            request.AppendPathSegment(route);
        }

        public async Task<APIServiceResult<List<int>>> Count(int MonthsCount = 0)
        {
            try
            {
                if (!request.Url.Path.Contains("count"))
                    request.Url.AppendPathSegment("count");

                var response = await request.SetQueryParam("MonthsCount", MonthsCount).GetAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                    }

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                    }

                    return APIServiceResult<List<int>>.WithStatusCode(response.StatusCode);
                }

                return APIServiceResult<List<int>>.OK(await response.Content.ReadAsAsync<List<int>>());
            }
            catch (Exception ex)
            {
                return APIServiceResult<List<int>>.Exception();
            }
        }

        /// <summary>
        /// DELETE request to the API
        /// </summary>
        /// <typeparam name="T">Type of return data</typeparam>
        /// <param name="id">Unique identifier of entity that will be partially updated</param>
        /// <param name="pathToAppend">Additional path to append on base url (e.g. "lock" custom operation as "/users/1/lock")</param>
        public async Task<APIServiceResult<T>> Delete<T>(int id, string pathToAppend = "")
        {
            try
            {
                request.Url.AppendPathSegment(id);
                if (!string.IsNullOrWhiteSpace(pathToAppend))
                {
                    request.Url.AppendPathSegment(pathToAppend);
                }

                var response = await request.DeleteAsync();
                RevertToBaseRequest();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                    }

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        if (response.Content != null)
                        {
                            var errorMessage = await response.Content?.ReadAsStringAsync() ?? string.Empty;
                            return APIServiceResult<T>.BadRequest(errorMessage);
                        }
                    }

                    return APIServiceResult<T>.WithStatusCode(response.StatusCode);
                }

                if (response.Content != null)
                {
                    var result = await response.Content?.ReadAsAsync<T>();

                    return result != null ? APIServiceResult<T>.OK(result) : APIServiceResult<T>.NoContent();
                }

                APIServiceResult<T>.NoContent();
            }
            catch (Exception ex)
            {
                return APIServiceResult<T>.Exception();
            }

            return APIServiceResult<T>.BadRequest();
        }

        /// <summary>
        /// GET request to the API
        /// </summary>
        /// <typeparam name="T">Type of return data</typeparam>
        /// <param name="resourceParameters">Resource parameters that will be sent as query string params</param>
        /// <param name="pathToAppend">Additional path to append on base url (e.g. "lock" custom operation as "/users/1/lock")</param>
        /// <returns></returns>
        public async Task<APIServiceResult<List<T>>> Get<T>(object resourceParameters = null, string pathToAppend = "")
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(pathToAppend))
                {
                    request.Url.AppendPathSegment(pathToAppend);
                }

                var response = await request.SetQueryParams(resourceParameters).GetAsync();
                RevertToBaseRequest(resourceParameters);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                    }

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        if (response.Content != null)
                        {
                            var errorMessage = await response.Content.ReadAsStringAsync();
                            return APIServiceResult<List<T>>.BadRequest(errorMessage);
                        }
                    }

                    return APIServiceResult<List<T>>.WithStatusCode(response.StatusCode);
                }

                var headers = response.Headers;

                var result = await response.Content.ReadAsAsync<List<T>>();

                if (response.StatusCode == HttpStatusCode.OK && result == null)
                    return APIServiceResult<List<T>>.OK();

                var xpaginationHeader = headers.FirstOrDefault(x => x.Key == "X-Pagination").Value?.FirstOrDefault();

                PaginationMetadata paginationMetadata = null;

                if (!string.IsNullOrWhiteSpace(xpaginationHeader))
                    paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(xpaginationHeader);

                return APIServiceResult<List<T>>.OK(result,paginationMetadata);
            }
            catch (Exception ex)
            {
                return APIServiceResult<List<T>>.Exception();
            }
        }

        public async Task<APIServiceResult<T>> GetById<T>(int id, string pathToAppend = "", bool eagerLoaded = false)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(pathToAppend))
                {
                    request.Url.AppendPathSegment(pathToAppend);
                }

                if (eagerLoaded)
                {
                    request.SetQueryParam("eagerLoaded", "true");
                }

                HttpResponseMessage response = null;
                request.Url.AppendPathSegment(id.ToString());

                response = await request.GetAsync();

                RevertToBaseRequest();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                    }

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        if (response.Content != null)
                        {
                            var errorMessage = await response.Content.ReadAsStringAsync();
                            return APIServiceResult<T>.BadRequest(errorMessage);
                        }
                    }

                    return APIServiceResult<T>.WithStatusCode(response.StatusCode);
                }

                var result = await response.Content.ReadAsAsync<T>();

                return APIServiceResult<T>.OK(result);
            }
            catch (FlurlHttpTimeoutException ex)
            {
                return APIServiceResult<T>.Exception();
            }
            catch (FlurlHttpException ex)
            {
                return APIServiceResult<T>.Exception();
            }
            catch (Exception ex)
            {
                return APIServiceResult<T>.Exception();
            }
        }

        /// <summary>
        /// PATCH request to the API
        /// </summary>
        /// <typeparam name="T">Type of return data</typeparam>
        /// <param name="id">Unique identifier of entity that will be partially updated</param>
        /// <param name="patchDocument">JSON patch document for updating entity</param>
        /// <param name="pathToAppend">Additional path to append on base url (e.g. "lock" custom operation as "/users/1/lock")</param>
        public async Task<APIServiceResult<T>> PartiallyUpdate<T>(int id, object patchDocument,
            string pathToAppend = "")
        {
            try
            {
                request.Url.AppendPathSegment(id);
                if (!string.IsNullOrWhiteSpace(pathToAppend))
                {
                    request.Url.AppendPathSegment(pathToAppend);
                }

                var response = await request.PatchJsonAsync(patchDocument);
                RevertToBaseRequest();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if ((int)response.StatusCode == 422 || response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        if (response.Content != null)
                        {
                            var errorMessage = await response.Content.ReadAsStringAsync();
                            return APIServiceResult<T>.BadRequest(errorMessage);
                        }
                    }

                    return APIServiceResult<T>.WithStatusCode(response.StatusCode);
                }

                var result = await response.Content.ReadAsAsync<T>();

                return APIServiceResult<T>.OK(result);
            }
            catch (Exception ex)
            {
                return APIServiceResult<T>.Exception();
            }
        }

        /// <summary>
        /// POST request to the API
        /// </summary>
        /// <typeparam name="T">Type of return data</typeparam>
        /// <param name="dtoForCreation">Data Transfer Object for creating new entity</param>
        /// <param name="ReturnData">Flag that indicates data returning</param>
        /// <param name="pathToAppend">Additional path to append on base url (e.g. "lock" custom operation as "/users/1/lock")</param>
        public async Task<APIServiceResult<T>> Post<T>(object dtoForCreation, bool ReturnData = false,
            string pathToAppend = "")
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(pathToAppend))
                {
                    request.Url.AppendPathSegment(pathToAppend);
                }

                HttpResponseMessage response = null;

                response = await request.PostJsonAsync(dtoForCreation);
                RevertToBaseRequest();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        if (response.Content != null)
                        {
                            var errorDetails = await response.Content?.ReadAsStringAsync();
                            return APIServiceResult<T>.BadRequest(errorDetails);
                        }
                    }
                    else if ((int)response.StatusCode == 422)
                    {
                        if (response.Content != null)
                        {
                            var errorDetails = await response.Content.ReadAsStringAsync();
                            var validationProblemDetails =
                                JsonConvert.DeserializeObject<ValidationProblemDetails>(errorDetails);

                            if (validationProblemDetails == null)
                                return APIServiceResult<T>.BadRequest();

                            return APIServiceResult<T>.BadRequest(validationProblemDetails.Detail);
                        }
                    }

                    return APIServiceResult<T>.WithStatusCode(response.StatusCode);
                }

                if (ReturnData)
                {
                    var result = await response.Content.ReadAsAsync<T>();
                    return APIServiceResult<T>.OK(result);
                }

                return APIServiceResult<T>.OK();
            }
            catch (Exception ex)
            {
                return APIServiceResult<T>.Exception();
            }
        }

        /// <summary>
        /// UPDATE request to the API
        /// </summary>
        /// <typeparam name="T">Type of return data</typeparam>
        /// <param name="id">Unique identifier of entity that will be updated</param>
        /// <param name="dtoForUpdate">Data Transfer Object for updating entity</param>
        /// <param name="pathToAppend">Additional path to append on base url (e.g. "lock" custom operation as "/users/1/lock")</param>
        public async Task<APIServiceResult<T>> Update<T>(int id, object dtoForUpdate, string pathToAppend = "")
        {
            try
            {
                request.Url.AppendPathSegment(id);
                if (!string.IsNullOrWhiteSpace(pathToAppend))
                {
                    request.Url.AppendPathSegment(pathToAppend);
                }

                HttpResponseMessage response = null;

                response = await request.PutJsonAsync(dtoForUpdate);
                RevertToBaseRequest();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    if ((int)response.StatusCode == 422 || response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        if (response.Content != null)
                        {
                            var errorMessage = await response.Content.ReadAsStringAsync();
                            return APIServiceResult<T>.WithStatusCode((HttpStatusCode)422, errorMessage);
                        }
                    }

                    return APIServiceResult<T>.WithStatusCode(response.StatusCode);
                }

                var result = await response.Content.ReadAsAsync<T>();
                return APIServiceResult<T>.OK(result);
            }
            catch (Exception ex)
            {
                return APIServiceResult<T>.Exception();
            }
        }

        public void RevertToBaseRequest(object resourceParameters = null)
        {
            if (resourceParameters != null)
            {
                request.Url.RemoveQueryParams(resourceParameters.GetType().GetProperties().Select(x => x.Name));
            }

            request.Url = BaseUrl;
        }
    }
}