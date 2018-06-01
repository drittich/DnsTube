using System;
using System.Collections.Generic;
using System.Threading;

namespace CloudflareDynDNS
{
	public class TaskScheduler
	{
		private static TaskScheduler _instance;
		private List<Timer> timers = new List<Timer>();

		private TaskScheduler() { }

		public static TaskScheduler Instance => _instance ?? (_instance = new TaskScheduler());

		public void ScheduleTask(TimeSpan interval, Action task, bool runImmediately = false)
		{
			var timeToFirstRun = runImmediately ? TimeSpan.Zero : interval;

			var timer = new Timer(x =>
			{
				task.Invoke();
			}, null, timeToFirstRun, interval);

			timers.Add(timer);
		}
	}
}
