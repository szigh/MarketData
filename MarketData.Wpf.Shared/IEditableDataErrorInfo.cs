using System.ComponentModel;

namespace MarketData.Wpf.Shared;

/// <summary>
/// Extends INotifyDataErrorInfo to provide methods for programmatically adding and clearing validation errors.
/// This allows behaviors and other external code to inject validation errors without using reflection.
/// </summary>
public interface IEditableDataErrorInfo : INotifyDataErrorInfo
{
    /// <summary>
    /// Adds a validation error for the specified property.
    /// If the error is null or empty, no action is taken.
    /// Multiple errors can be added for the same property.
    /// </summary>
    /// <param name="propertyName">The name of the property with the error.</param>
    /// <param name="error">The error message, or null to do nothing.</param>
    void AddError(string propertyName, string? error);

    /// <summary>
    /// Clears all validation errors for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property to clear errors for.</param>
    void ClearErrors(string propertyName);
}
