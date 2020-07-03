using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
			if (string.IsNullOrWhiteSpace(headerName))
				return;

			RemoveRequestHeader(headerName);

			this.HttpClient.DefaultRequestHeaders.Add(headerName, value);
		}

		/// <summary>
		/// Removes a request header from each HTTP request that will be sent in this instance - checks for header existence first
		/// </summary>
		/// <param name="headerName"></param>
		public void RemoveRequestHeader(string headerName)
		{
			if (string.IsNullOrWhiteSpace(headerName))
				return;

			if (this.HttpClient.DefaultRequestHeaders.Contains(headerName))
				this.HttpClient.DefaultRequestHeaders.Remove(headerName);
		}

		/// <summary>
		/// Prepares HTTP body content that can be used with POST
		/// </summary>
		/// <param name="contents">Appropriately serialized HTTP body content</param>
		/// <param name="mediaType">HTTP media type expected by the URL, like "application/json" or "application/xml"</param>
		/// <returns></returns>
		public HttpContent PrepareHttpContent(string contents, string mediaType)
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

		public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
		{
			return await this.HttpClient.PostAsync(requestUri, content);
		}

		public async Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
		{
			return await this.HttpClient.GetAsync(requestUri, completionOption);
		}

		public async Task<string> GetHttpResponseContentAsync(HttpResponseMessage httpResponse)
		{
			return await httpResponse?.Content?.ReadAsStringAsync();
		}
	}
}
