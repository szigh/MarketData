using MarketData.Wpf.Shared;
using System.Collections;
using System.ComponentModel;
using System.Text;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument.Steps;

public abstract class AddInstrumentViewModelBase : ViewModelBase, IEditableDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    protected AddInstrumentViewModelBase()
    {
        // Automatically re-validate whenever any property changes
        PropertyChanged += (_, e) =>
        {
            // Don't re-validate when ValidationMessage or HasErrors changes (avoid infinite loop)
            if (e.PropertyName != nameof(ValidationMessage) && e.PropertyName != nameof(HasErrors))
            {
                UpdateValidationErrors();
            }
        };
    }

    /// <summary>
    /// Returns a summary of all validation errors as a single string.
    /// </summary>
    public string ValidationMessage
    {
        get
        {
            if (!HasErrors)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var kvp in _errors)
            {
                foreach (var error in kvp.Value)
                {
                    if (sb.Length > 0)
                        sb.AppendLine();
                    sb.Append(error);
                }
            }
            return sb.ToString();
        }
    }

    #region INotifyDataErrorInfo Implementation

    public bool HasErrors => _errors.Count > 0;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            // Return all errors
            return _errors.Values.SelectMany(e => e);
        }

        return _errors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<string>();
    }

    public void AddError(string propertyName, string? error)
    {
        if (string.IsNullOrEmpty(error))
            return;

        if (!_errors.ContainsKey(propertyName))
        {
            _errors[propertyName] = new List<string>();
        }

        if (_errors.TryGetValue(propertyName, out var errors) && errors.Contains(error))
        {
            // Avoid adding duplicate errors for the same property
            return;
        }

        _errors[propertyName].Add(error);
        OnErrorsChanged(propertyName);
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(ValidationMessage));
    }

    public void ClearErrors(string propertyName)
    {
        if (_errors.Remove(propertyName))
        {
            OnErrorsChanged(propertyName);
            OnPropertyChanged(nameof(HasErrors));
            OnPropertyChanged(nameof(ValidationMessage));
        }
    }

    protected void ClearAllErrors()
    {
        var propertyNames = _errors.Keys.ToList();
        _errors.Clear();

        foreach (var propertyName in propertyNames)
        {
            OnErrorsChanged(propertyName);
        }

        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(ValidationMessage));
    }

    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    #endregion

    /// <summary>
    /// Override this method to implement validation logic.
    /// Use AddError() and ClearAllErrors() to set validation errors.
    /// </summary>
    protected abstract void UpdateValidationErrors();
}
