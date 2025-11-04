using MFilesAPI;

namespace Taskify.Infrastructure.Helpers;

public static class PropertyValueHelper
{
    /// <summary>
    /// Updates or adds a property value in a PropertyValues collection.
    /// </summary>
    public static void SetOrUpdateProperty(PropertyValues properties, PropertyValue newValue)
    {
        var existingProperty = properties.SearchForProperty(newValue.PropertyDef);

        if (existingProperty != null)
        {
            // Property exists, update it
            existingProperty.TypedValue = newValue.TypedValue;
        }
        else
        {
            // Property doesn't exist, add it
            properties.Add(-1, newValue);
        }
    }

    /// <summary>
    /// Appends text to an existing multi-line text property.
    /// </summary>
    public static void AppendToMultiLineTextProperty(
        PropertyValues properties,
        int propertyDef,
        string textToAppend,
        string separator = "\n\n")
    {
        var propertyValue = new PropertyValue
        {
            PropertyDef = propertyDef
        };

        if (properties.IndexOf(propertyDef) == -1)
            properties.Add(-1, propertyValue);

        var existingProperty = properties.SearchForProperty(propertyDef);

        string existingText = string.Empty;
        if (existingProperty != null && !existingProperty.TypedValue.IsNULL())
        {
            existingText = existingProperty.TypedValue.Value?.ToString() ?? string.Empty;
        }

        var newText = string.IsNullOrEmpty(existingText)
            ? textToAppend
            : $"{existingText}{separator}{textToAppend}";

        propertyValue.TypedValue.SetValue(MFDataType.MFDatatypeMultiLineText, newText);

        SetOrUpdateProperty(properties, propertyValue);
    }
}