using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.NoteRandomizer.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.NoteRandomizer.ViewModels
{
    /// <summary>
    /// 主窗口的 ViewModel，负责与界面交互和数据绑定。
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;

        [ObservableProperty] private ObservableCollection<string> _logs = new();
        [ObservableProperty] private string _note = "C";
        [ObservableProperty] private string _nextNote = "D";
        [ObservableProperty] private int _bpm = 60;
        
        private Func<Task> _randomNoteAction = null!;

        private int _noteCount;
        private int _lastNoteIndex;
        
        private readonly List<string> _notes = new()
        {
            "Cb", "C", "C#",
            "Db", "D", "D#",
            "Eb", "E", "E#",
            "Fb", "F", "F#",
            "Gb", "G", "G#",
            "Ab", "A", "A#",
            "Bb", "B", "B#"
        };

        public MainWindowViewModel()
        {
            _settingsService = App.ServiceProvider.GetService<SettingsService>()!;
            App.ServiceProvider.GetRequiredService<LogCollector>().LogAdded += AppendLog;

            _randomNoteAction = async () =>
            {
                _noteCount %= 4;
                int frequency = 1000;
                if (_noteCount == 0)
                {
                    GetRandomNote();
                    frequency = 2000;
                }
                _noteCount++;
                await BeepAtInterval(frequency, 200, TimeSpan.Zero);
            };
            
            var random = new Random();
            NextNote = _notes[random.Next(_notes.Count)];
        }

        [RelayCommand]
        private void StartRandomizer()
        {
            // 使用 TimerService 每隔 4 拍的时间触发一次 _randomNoteAction
            _noteCount = 0;
            var timerService = App.ServiceProvider.GetRequiredService<TimerService>();
            var period = TimeSpan.FromMilliseconds(60000 / Bpm);
            timerService.RegisterEvent(period, _randomNoteAction);
        }
        
        [RelayCommand]
        private void StopRandomizer()
        {
            var timerService = App.ServiceProvider.GetRequiredService<TimerService>();
            timerService.UnregisterEvent(_randomNoteAction);
        }
        
        private void GetRandomNote()
        {
            var random = new Random();
            int index = random.Next(_notes.Count);
            while (index == _lastNoteIndex)
            {
                index = random.Next(_notes.Count);
            }

            Note = NextNote;
            NextNote = _notes[index];
            _lastNoteIndex = index;
        }

        private async void AppendLog(string message)
        {
            try
            {
                // 确保在 UI 线程上更新 Logs 属性
                await Dispatcher.UIThread.InvokeAsync(() => { Logs.Add(message); });
            }
            catch
            {
                // 忽略异常
            }
        }

        /// <summary>
        /// 安排在指定延迟后播放指定频率和时长的 beep 声音。
        /// </summary>
        /// <param name="frequency">音调频率</param>
        /// <param name="duration">持续时间（毫秒）</param>
        /// <param name="delay">延迟时间</param>
        /// <returns></returns>
        private async Task BeepAtInterval(int frequency, int duration, TimeSpan delay)
        {
            await Task.Delay(delay);
            Console.Beep(frequency, duration);
        }
    }
}
