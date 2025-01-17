using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.NoteRandomizer.Services
{
    public class TimerService : IDisposable
    {
        private readonly List<Timer> _timers = new();
        private readonly Dictionary<Func<Task>, Timer> _registeredEvents = new();

        /// <summary>
        /// 注册一个异步事件和时间间隔。每当达到指定的时间间隔时，就会触发该异步事件。
        /// </summary>
        /// <param name="interval">事件的时间间隔</param>
        /// <param name="eventHandler">定时触发的异步事件处理委托</param>
        public void RegisterEvent(TimeSpan interval, Func<Task> eventHandler)
        {
            if (eventHandler == null) throw new ArgumentNullException(nameof(eventHandler));

            // 创建一个 TimerCallback，以异步方式调用 eventHandler
            TimerCallback callback = _ =>
            {
                // 启动一个后台任务来执行异步方法，但不等待其完成
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await eventHandler.Invoke();
                    }
                    catch (Exception ex)
                    {
                        // 处理异步方法中的异常，根据需要记录或处理
                        Console.Error.WriteLine($"异步事件处理异常: {ex}");
                    }
                });
            };

            var timer = new Timer(callback, null, interval, interval);

            lock (_timers)
            {
                _timers.Add(timer);
            }

            lock (_registeredEvents)
            {
                if (_registeredEvents.ContainsKey(eventHandler))
                {
                    UnregisterEvent(eventHandler);
                }
                _registeredEvents[eventHandler] = timer;
            }
        }

        /// <summary>
        /// 解除先前注册的指定异步事件。停止对应的定时器并移除事件。
        /// </summary>
        /// <param name="eventHandler">需要解除注册的异步事件处理委托</param>
        public void UnregisterEvent(Func<Task> eventHandler)
        {
            if (eventHandler == null) throw new ArgumentNullException(nameof(eventHandler));

            lock (_registeredEvents)
            {
                if (_registeredEvents.TryGetValue(eventHandler, out var timer))
                {
                    timer.Dispose();
                    _registeredEvents.Remove(eventHandler);

                    lock (_timers)
                    {
                        _timers.Remove(timer);
                    }
                }
            }
        }

        /// <summary>
        /// 释放所有定时器资源
        /// </summary>
        public void Dispose()
        {
            lock (_timers)
            {
                foreach (var timer in _timers)
                {
                    timer.Dispose();
                }
                _timers.Clear();
            }

            lock (_registeredEvents)
            {
                _registeredEvents.Clear();
            }
        }
    }
}
