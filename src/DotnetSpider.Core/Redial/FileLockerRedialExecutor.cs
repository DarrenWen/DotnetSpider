﻿using System;
using System.IO;
using System.Threading;
using DotnetSpider.Core.Redial.Redialer;
using System.Linq;
using DotnetSpider.Core.Redial.InternetDetector;

namespace DotnetSpider.Core.Redial
{
	public class FileLockerRedialExecutor : RedialExecutor
	{
		private static readonly string AtomicActionFolder;
		private static readonly string RedialLockerFile;
		private static readonly int RedialTimeout = 120 * 1000 / 50;

		static FileLockerRedialExecutor()
		{
			AtomicActionFolder = Path.Combine(Core.Infrastructure.Environment.GlobalDirectory, "atomicaction");
			RedialLockerFile = Path.Combine(Core.Infrastructure.Environment.GlobalDirectory, "redial.lock");
		}

		public FileLockerRedialExecutor(IRedialer redialer, IInternetDetector validater) : base(redialer, validater)
		{
			if (!Directory.Exists(AtomicActionFolder))
			{
				Directory.CreateDirectory(AtomicActionFolder);
			}
		}

		public override void WaitAll()
		{
			File.Create(RedialLockerFile).Dispose();
			while (true)
			{
				if (!Directory.GetFiles(AtomicActionFolder).Any())
				{
					break;
				}
				Thread.Sleep(50);
			}
		}

		public override void WaitRedialExit()
		{
			lock (this)
			{
				if (File.Exists(RedialLockerFile))
				{
					for (int i = 0; i < RedialTimeout; ++i)
					{
						Thread.Sleep(50);
						if (!File.Exists(RedialLockerFile))
						{
							break;
						}
					}
				}
			}
		}

		public override string CreateActionIdentity(string name)
		{
			string id = Path.Combine(AtomicActionFolder, name + "-" + Guid.NewGuid().ToString("N"));
			File.Create(id).Dispose();
			return id;
		}

		public override void DeleteActionIdentity(string identity)
		{
			File.Delete(identity);
		}

		public override bool CheckIsRedialing()
		{
			lock (Lock)
			{
				if (!File.Exists(RedialLockerFile))
				{
					//File.Create(RedialLockerFile).Dispose();
					return false;
				}
				return true;
			}
		}

		public override void ReleaseRedialLocker()
		{
			File.Delete(RedialLockerFile);
		}
	}
}
