using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MyThreadPool
{
	public class PoolEventArgs : EventArgs
	{
		public int Threads { get; }
		public int Queue { get; }
		public string Message { get; }
		public PoolEventArgs(int threads, int queue, string msg) { Threads = threads; Queue = queue; Message = msg; }
	}

	public class CustomThreadPool : IDisposable
	{
		private readonly int _minThreads;
		private readonly int _maxThreads;
		private readonly int _idleTimeoutMs;
		private readonly Queue<Action> _taskQueue = new Queue<Action>();
		private readonly List<WorkerNode> _workers = new List<WorkerNode>();
		private readonly object _lock = new object();
		private bool _isDisposed = false;

		public event EventHandler<PoolEventArgs> ScaledUp;
		public event EventHandler<PoolEventArgs> ScaledDown;
		public event EventHandler<PoolEventArgs> ThreadReplaced;
		public event EventHandler<PoolEventArgs> PoolStopped;

		public int CurrentThreadCount { get { lock (_lock) return _workers.Count; } }
		public int QueueCount { get { lock (_lock) return _taskQueue.Count; } }

		public CustomThreadPool(int minThreads, int maxThreads, int idleTimeoutMs = 2000)
		{
			_minThreads = minThreads;
			_maxThreads = maxThreads;
			_idleTimeoutMs = idleTimeoutMs;
			lock (_lock) 
				for (int i = 0; i < _minThreads; i++) 
					AddWorker();
			new Thread(ManagePool) { IsBackground = true, Name = "PoolManager" }.Start();
		}

		public void Enqueue(Action task)
		{
			lock (_lock) 
			{ 
				_taskQueue.Enqueue(task); 
				Monitor.Pulse(_lock); 
			}
		}

		private void AddWorker() 
		{ 
			var w = new WorkerNode(this); 
			_workers.Add(w); w.Start(); 
		}

		private void Fire(EventHandler<PoolEventArgs> ev, string msg)
		{
			int t, q;
			lock (_lock) 
			{ 
				t = _workers.Count; q = _taskQueue.Count; 
			}
			ev?.Invoke(this, new PoolEventArgs(t, q, msg));
		}

		private void ManagePool()
		{
			while (!_isDisposed)
			{
				lock (_lock)
				{
					if (_taskQueue.Count > 0 && _workers.Count < _maxThreads)
					{
						AddWorker();
						Fire(ScaledUp, $"Scale UP: {_workers.Count} threads");
						Monitor.PulseAll(_lock);
					}

					for (int i = _workers.Count - 1; i >= _minThreads; i--)
					{
						if (_workers[i].IsIdleTooLong(_idleTimeoutMs))
						{
							_workers[i].Stop(); _workers.RemoveAt(i);
							Fire(ScaledDown, $"Scale DOWN: {_workers.Count} threads");
						}
					}

					for (int i = 0; i < _workers.Count; i++)
					{
						if (_workers[i].IsHung(5000))
						{
							int hungId = _workers[i].Id;
							_workers[i].Abandon(); _workers.RemoveAt(i); AddWorker();
							int newId = _workers[_workers.Count - 1].Id;
							Fire(ThreadReplaced, $"Thread {hungId} HUNG. Replaced with Thread {newId}. Total threads: {_workers.Count}");
						}
					}
				}
				Thread.Sleep(200);
			}
		}

		private class WorkerNode
		{
			private Thread _thread;
			private readonly CustomThreadPool _parent;
			private bool _shouldStop = false;
			private bool _isWorking = false;
			private Stopwatch _timer = Stopwatch.StartNew();

			public int Id => _thread.ManagedThreadId;
			public bool IsIdleTooLong(int limit) => !_isWorking && _timer.ElapsedMilliseconds > limit;
			public bool IsHung(int limit) => _isWorking && _timer.ElapsedMilliseconds > limit;
			public void Stop() => _shouldStop = true;
			public void Abandon() => _shouldStop = true;

			public WorkerNode(CustomThreadPool parent)
			{
				_parent = parent;
				_thread = new Thread(WorkLoop) { IsBackground = true };
			}

			public void Start() => _thread.Start();

			private void WorkLoop()
			{
				while (!_shouldStop)
				{
					Action task = null;
					lock (_parent._lock)
					{
						while (_parent._taskQueue.Count == 0 && !_shouldStop && !_parent._isDisposed)
						{ 
							_isWorking = false;
							Monitor.Wait(_parent._lock, 1000); 
						}
						if (_shouldStop || _parent._isDisposed) break;
						if (_parent._taskQueue.Count > 0) 
							task = _parent._taskQueue.Dequeue();
					}
					if (task == null) continue;
					try 
					{ 
						_isWorking = true; 
						_timer.Restart(); 
						task();
					}
					catch { }
					finally { _isWorking = false; _timer.Restart(); }
				}
			}
		}

		public void Dispose()
		{
			_isDisposed = true;
			lock (_lock)
				Monitor.PulseAll(_lock);
			Fire(PoolStopped, "Pool stopped");
		}
	}
}