using System.Windows;
using System.Windows.Controls;

namespace MarketData.Wpf.Shared.Behaviours;

/// <summary>
/// Attached behavior that synchronizes WPF validation errors (from ValidatesOnExceptions)
/// with IEditableDataErrorInfo on the ViewModel.
/// </summary>
public static class SyncValidationErrorsBehavior
{
    public static readonly DependencyProperty EnableSyncProperty =
        DependencyProperty.RegisterAttached(
            "EnableSync",
            typeof(bool),
            typeof(SyncValidationErrorsBehavior),
            new PropertyMetadata(false, OnEnableSyncChanged));

    public static bool GetEnableSync(DependencyObject obj)
    {
        return (bool)obj.GetValue(EnableSyncProperty);
    }

    public static void SetEnableSync(DependencyObject obj, bool value)
    {
        obj.SetValue(EnableSyncProperty, value);
    }

    private static void OnEnableSyncChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if ((bool)e.NewValue)
        {
            Validation.AddErrorHandler(element, OnValidationError);
            element.Unloaded += OnElementUnloaded;
        }
        else
        {
            Validation.RemoveErrorHandler(element, OnValidationError);
            element.Unloaded -= OnElementUnloaded;
        }
    }

    private static void OnElementUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            Validation.RemoveErrorHandler(element, OnValidationError);
            element.Unloaded -= OnElementUnloaded;
        }
    }

    private static void OnValidationError(object? sender, ValidationErrorEventArgs e)
    {
        if (sender is not FrameworkElement element)
            return;

        // Only sync conversion errors (ExceptionValidationRule), not INotifyDataErrorInfo errors
        // INotifyDataErrorInfo errors are already in the ViewModel!
        if (e.Error.RuleInError is not ExceptionValidationRule)
            return;

        // Get the binding expression to find the property name
        var bindingExpression = element.GetBindingExpression(TextBox.TextProperty) 
                              ?? element.GetBindingExpression(ComboBox.SelectedItemProperty);

        if (bindingExpression?.ParentBinding.Path?.Path is not string propertyName)
            return;

        if (element.DataContext is not IEditableDataErrorInfo viewModel)
            return;

        if (e.Action == ValidationErrorEventAction.Added)
        {
            string errorMessage = e.Error.ErrorContent?.ToString() ?? "Invalid value";

            viewModel.AddError(propertyName, errorMessage);
        }
        else if (e.Action == ValidationErrorEventAction.Removed)
        {
            viewModel.ClearErrors(propertyName);
        }
    }
}
