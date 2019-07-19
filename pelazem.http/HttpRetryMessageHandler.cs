using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace pelazem.http
{
	public class HttpRetryMessageHandler : DelegatingHandler
	{
		public ILogger Logger { get; set; }

		public int HowManyRetries { get; set; } = 3;
		public int RetryExponent { get; set; } = 2;

		public HttpRetryMessageHandler(HttpClientHandler handler) : base(handler) { }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return HttpPolicyExtensions
				.HandleTransientHttpError()
				.Or<TimeoutRejectedException>()
				.Or<TaskCanceledException>()
				.WaitAndRetryAsync(this.HowManyRetries, retryCount => TimeSpan.FromSeconds(Math.Pow(this.RetryExponent, retryCount)), (response, timeSpan, retryCount, context) =>
				{
					if (response.Exception != null)
					{
						if (this.Logger != null)
							this.Logger.LogError(response.Exception, $"An exception occurred on retry {retryCount} to {request.RequestUri.AbsoluteUri}");
					}
					else
					{
						if (this.Logger != null)
							this.Logger.LogError($"A non-success code {(int)response.Result.StatusCode} was received on retry {retryCount} to {request.RequestUri.AbsoluteUri}");
					}
				})
				.ExecuteAsync(() => base.SendAsync(request, cancellationToken))
			;
		}
	}
}
