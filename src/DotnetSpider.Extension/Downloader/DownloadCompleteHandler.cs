﻿using DotnetSpider.Core;
using DotnetSpider.Core.Downloader;
using DotnetSpider.Core.Infrastructure;
using DotnetSpider.Core.Redial;

namespace DotnetSpider.Extension.Downloader
{
	public class RedialWhenContainsHandler : DownloadCompleteHandler
	{
		public string Content { get; set; }

		public override bool Handle(Page page, ISpider spider)
		{
			if (!string.IsNullOrEmpty(page.Content) && page.Content.Contains(Content))
			{
				NetworkCenter.Current.Executor.Redial();
				page.Exception = new DownloadException($"Content downloaded contains string: {Content}.");
			}
			return true;
		}
	}

	public class RedialWhenExceptionThrowHandler : DownloadCompleteHandler
	{
		public string ExceptionMessage { get; set; } = string.Empty;

		public override bool Handle(Page page, ISpider spider)
		{
			if (page.Exception != null)
			{
				if (string.IsNullOrEmpty(ExceptionMessage))
				{
					page.Exception = new SpiderException("ExceptionMessage should not be empty/null.");
				}
				if (page.Exception.Message.Contains(ExceptionMessage))
				{
					NetworkCenter.Current.Executor.Redial();
					page.Exception = new DownloadException($"Download failed and redial finished already.");
				}
			}
			return true;
		}
	}

	public class RedialAndUpdateCookieWhenContainsHandler : DownloadCompleteHandler
	{
		public string Content { get; set; }
		public ICookieInjector CookieInjector { get; set; }

		public override bool Handle(Page page, ISpider spider)
		{
			if (!string.IsNullOrEmpty(page.Content) && CookieInjector != null && page.Content.Contains(Content))
			{
				NetworkCenter.Current.Executor.Redial();

				CookieInjector?.Inject(spider);
				page.Exception = new DownloadException($"Content downloaded contains string: {Content}.");
			}
			return true;
		}
	}

	public class CycleRedialHandler : DownloadCompleteHandler
	{
		public int RedialLimit { get; set; }
		public static int RequestedCount { get; set; }

		public override bool Handle(Page page, ISpider spider)
		{
			if (RedialLimit != 0)
			{
				lock (this)
				{
					++RequestedCount;

					if (RedialLimit > 0 && RequestedCount == RedialLimit)
					{
						RequestedCount = 0;

						NetworkCenter.Current.Executor.Redial();
					}
				}
			}
			return true;
		}
	}
}