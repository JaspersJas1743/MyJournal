using System;
using System.Diagnostics;
using System.Timers;
using ReactiveUI;

namespace MyJournal.Desktop.Models;

public class InitialLoadingModel : ModelBase
{
	private const string BaseLoadingText = "Загрузка";
	private readonly Timer _timer = new Timer(interval: TimeSpan.FromMilliseconds(value: 250));
	private string _loadingText = BaseLoadingText;
	private int _counter = 0;

	public InitialLoadingModel()
	{
		_timer.Elapsed += OnTimerElapsed;
		_timer.Start();
	}

	public string LoadingText
	{
		get => _loadingText;
		set => this.RaiseAndSetIfChanged(backingField: ref _loadingText, newValue: value);
	}

	public void StopTimer()
		=> _timer.Stop();

	private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
	{
		if (_counter == 3)
			_counter = 0;

		++_counter;
		LoadingText = BaseLoadingText + new string(c: '.', count: _counter);
	}
}