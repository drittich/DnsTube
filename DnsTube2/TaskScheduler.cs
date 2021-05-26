using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DnsTube2
{
	public class TaskScheduler
	{
		static TaskScheduler _instance;
		List<Timer> timers = new List<Timer>();

		TaskScheduler() { }

		public static TaskScheduler Instance => _instance ?? (_instance = new TaskScheduler());

		public TaskScheduler ScheduleTask(TimeSpan interval, Action task, bool runImmediately = false)
		{
			var timeToFirstRun = runImmediately ? TimeSpan.Zero : interval;

			var timer = new Timer(x =>
			{
				task.Invoke();
			}, null, timeToFirstRun, interval);
			timers.Add(timer);

			return Instance;
		}

		public static void StopAll()
		{
			// Ref: https://stackoverflow.com/a/14359490/39430
			if (_instance != null)
			{
				foreach (var timer in Instance.timers)
					timer.Change(Timeout.Infinite, Timeout.Infinite);
				_instance = null;
			}
		}
	}
}
