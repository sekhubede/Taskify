using MFilesAPI;

namespace Taskify.Connectors.MFiles;

/// <summary>
/// Translates M-Files objects into Taskify's TaskDTO model.
/// Every source system gets its own mapper. The messiness stays here.
/// </summary>
public static class MFilesTaskMapper
{
    public static TaskDTO Map(Vault vault, ObjectVersion objVersion)
    {
        var properties = vault.ObjectPropertyOperations.GetProperties(objVersion.ObjVer);

        return new TaskDTO
        {
            Id = objVersion.ObjVer.ID.ToString(),
            Title = GetPropertyValue<string>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle) ?? "Untitled",
            Description = GetPropertyValue<string>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignmentDescription) ?? string.Empty,
            DueDate = GetPropertyValue<DateTime?>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefDeadline),
            Status = MapStatus(properties),
            AssigneeId = GetAssignedToId(properties),
            AssigneeName = GetAssignedToName(properties),
            CreatedAt = GetPropertyValue<DateTime>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCreated),
            LastUpdatedAt = GetPropertyValue<DateTime>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefLastModified),
            CompletedAt = null,
            SourceSystem = "MFiles",
            SourceId = objVersion.ObjVer.ID.ToString()
        };
    }

    public static IReadOnlyList<TaskDTO> MapAll(Vault vault, ObjectSearchResults searchResults)
    {
        var tasks = new List<TaskDTO>();

        for (int i = 1; i <= searchResults.Count; i++)
        {
            try
            {
                var objVersion = searchResults[i];
                tasks.Add(Map(vault, objVersion));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to map assignment {searchResults[i].ObjVer.ID}: {ex.Message}");
            }
        }

        return tasks;
    }

    private static TaskItemStatus MapStatus(PropertyValues properties)
    {
        var completed = GetPropertyValue<bool>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCompleted);
        if (completed)
            return TaskItemStatus.Completed;

        return TaskItemStatus.InProgress;
    }

    private static string GetAssignedToName(PropertyValues properties)
    {
        try
        {
            var assignedToProp = properties.SearchForProperty((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignedTo);
            if (assignedToProp == null || assignedToProp.TypedValue.IsNULL())
                return "Unassigned";

            var lookups = assignedToProp.TypedValue.GetValueAsLookups();
            var names = lookups
                .Cast<Lookup>()
                .Select(l => l.DisplayValue)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return names.Count > 0 ? string.Join(", ", names) : "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetAssignedToId(PropertyValues properties)
    {
        try
        {
            var assignedToProp = properties.SearchForProperty((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignedTo);
            if (assignedToProp == null || assignedToProp.TypedValue.IsNULL())
                return string.Empty;

            var lookups = assignedToProp.TypedValue.GetValueAsLookups();
            var ids = lookups
                .Cast<Lookup>()
                .Select(l => l.Item.ToString())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct()
                .ToList();

            return string.Join(",", ids);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static T? GetPropertyValue<T>(PropertyValues properties, int propertyDef)
    {
        try
        {
            var propValue = properties.SearchForProperty(propertyDef);
            if (propValue == null || propValue.TypedValue.IsNULL() || propValue.TypedValue.IsUninitialized())
                return default;

            var value = propValue.TypedValue.Value;

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                if (value is DateTime dt)
                    return (T)(object)dt;
                return default;
            }

            return (T)value;
        }
        catch
        {
            return default;
        }
    }
}
