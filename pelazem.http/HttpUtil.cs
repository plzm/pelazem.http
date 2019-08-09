using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using pelazem.util;

namespace pelazem.http
{
	public class HttpUtil
	{
		private HttpClient _httpClient = null;

		public ILogger Logger { get; set; }

		public HttpClient HttpClient
		{
			get
			{
				// Use an instance-lifetime HttpClient and delay instantiation of HttpClient until needed
				if (_httpClient == null)
				{
					HttpClientHandler clientHandler = new HttpClientHandler()
					{
						AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
					};

					HttpRetryMessageHandler retryHandler = new HttpRetryMessageHandler(clientHandler) { Logger = this.Logger };

					_httpClient = new HttpClient(retryHandler);
				}

				return _httpClient;
			}
		}

		/// <summary>
		/// Adds a request header to each HTTP request that will be sent in this instance
		/// Common header: "Ocp-Apim-Subscription-Key" with an API key value
		/// </summary>
		/// <param name="headerName"></param>
		/// <param name="value"></param>
		public void AddRequestHeader(string headerName, string value)
		{
			this.HttpClient.DefaultRequestHeaders.Add(headerName, value);
		}

		/// <summary>
		/// Prepares HTTP body content that can be used with POST
		/// </summary>
		/// <param name="contents">Appropriately serialized HTTP body content</param>
		/// <param name="mediaType">HTTP media type expected by the URL, like "application/json" or "application/xml"</param>
		/// <returns></returns>
		public HttpContent GetHttpContent(string contents, string mediaType)
		{
			Encoding encoding = Encoding.UTF8;

			HttpContent result = new StringContent(contents, encoding, mediaType);

			return result;
		}

		public void AddContentHeader(HttpContent content, string headerName, string value)
		{
			if (content == null || string.IsNullOrWhiteSpace(headerName) || string.IsNullOrWhiteSpace(value))
				return;

			if (content.Headers.Contains(headerName))
				content.Headers.Remove(headerName);

			content.Headers.Add(headerName, value);
		}

		/// <summary>
		/// Invokes POST. Sets the HTTP response to the returned OpResult's Output property.
		/// </summary>
		/// <param name="requestUri"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		public async Task<OpResult> PostAsync(string requestUri, HttpContent content)
		{
			OpResult result = new OpResult();

			try
			{
				HttpResponseMessage response = await this.HttpClient.PostAsync(requestUri, content);

				result.Succeeded = response.IsSuccessStatusCode;
				result.Message = response.ReasonPhrase;
				result.Output = response.Content;
			}
			catch (Exception ex)
			{
				result.Succeeded = false;
				result.Exception = ex;
			}

			return result;
		}

		/// <summary>
		/// Invokes GET. Sets the HTTP response to the returned OpResult's Output property.
		/// </summary>
		/// <param name="requestUri"></param>
		/// <returns></returns>
		public async Task<OpResult> GetAsync(string requestUri)
		{
			OpResult result = new OpResult();

			try
			{
				HttpResponseMessage response = await this.HttpClient.GetAsync(requestUri, HttpCompletionOption.ResponseContentRead);

				result.Succeeded = response.IsSuccessStatusCode;
				result.Message = response.ReasonPhrase;
				result.Output = response.Content;
			}
			catch (Exception ex)
			{
				result.Succeeded = false;
				result.Exception = ex;
			}

			return result;
		}

		/// <summary>
		/// Given an HTTP request body that is flat JSON and can be serialized to a dictionary of string keys and object values, performs that conversion
		/// </summary>
		/// <param name="requestBody"></param>
		/// <returns></returns>
		public IDictionary<string, object> ConvertRequestBody(string requestBody)
		{
			IDictionary<string, object> result = null;

			var converter = new ExpandoObjectConverter();

			// Deserialize the passed request body string to a dynamic ExpandoObject
			var interim = JsonConvert.DeserializeObject<ExpandoObject>(requestBody, converter);

			// If conversion succeeded, cast it as the return type
			if (interim != null)
				result = interim as IDictionary<string, object>;

			return result;
		}
	}
}
