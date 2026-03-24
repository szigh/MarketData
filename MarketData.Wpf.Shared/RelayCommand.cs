using System.Windows.Input;

namespace MarketData.Wpf.Shared;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();

    public void Execute(object? parameter) => _execute();
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute((T?)parameter);

    public void Execute(object? parameter) => _execute((T?)parameter);

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;
    private CancellationTokenSource? _cancellationTokenSource;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(ct => execute(), canExecute)
    {
    }

    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute == null || _canExecute());
    }

    public async void Execute(object? parameter)
    {
        if (_isExecuting)
            return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        // Create a new CTS for this execution
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            await _execute(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when operation is cancelled, don't propagate
        }
        finally
        {
            _isExecuting = false;

            // Atomically swap out the CTS before disposing to prevent Cancel() from
            // calling Cancel() on a disposed CTS on another thread.
            var cts = Interlocked.Exchange(ref _cancellationTokenSource, null);
            cts?.Dispose();

            RaiseCanExecuteChanged();
        }
    }

    public void Cancel()
    {
        // Capture to local to avoid a race where another thread disposes the CTS
        // between the null-check and the Cancel() call.
        var cts = _cancellationTokenSource;
        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // CTS was disposed concurrently; cancellation is no longer needed.
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
