namespace Taskify.Infrastructure.Mappers;

using MFilesAPI;
using Taskify.Domain.Entities;
using System.Linq;
using System.ComponentModel;
using Taskify.Domain.Interfaces;

public static class MFilesDataMapper
{
    public static Domain.Entities.Vault MapToDomainVault(MFilesAPI.Vault mfilesVault)
    {
        return new Domain.Entities.Vault(
            name: mfilesVault.Name,
            guid: mfilesVault.GetGUID()
        );
    }

    public static Assignment MapToDomainAssignment(MFilesAPI.Vault vault, ObjectVersion objVersion, ISubtaskRepository? subtaskRepository = null)
    {
        var properties = vault.ObjectPropertyOperations.GetProperties(objVersion.ObjVer);

        // Get subtasks if repository provided
        List<Subtask>? subtasks = null;
        if (subtaskRepository != null)
        {
            try
            {
                subtasks = subtaskRepository.GetSubtasksForAssignment(objVersion.ObjVer.ID);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load subtasks for assignment {objVersion.ObjVer.ID}: {ex.Message}");
            }
        }

        return new Assignment(
            id: objVersion.ObjVer.ID,
            title: GetPropertyValue<string>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle) ?? "Untitled",
            description: GetPropertyValue<string>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignmentDescription) ?? string.Empty,
            dueDate: GetPropertyValue<DateTime?>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefDeadline),
            status: MapAssignmentStatus(properties),
            assignedTo: GetAssignedToName(vault, properties),
            createdDate: GetPropertyValue<DateTime>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCreated),
            completedDate: null,
            subtasks: subtasks
        );
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
                // M-Files returns date as object, need to convert
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

    private static AssignmentStatus MapAssignmentStatus(PropertyValues properties)
    {
        // Check if assignment is complete
        var completed = GetPropertyValue<bool>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCompleted);
        if (completed)
            return AssignmentStatus.Completed;

        // You may need to check workflow state or other properties depending on your M-Files setup
        // For now, assume InProgress if not completed
        return AssignmentStatus.InProgress;
    }

    private static string GetAssignedToName(MFilesAPI.Vault vault, PropertyValues properties)
    {
        try
        {
            var assignedToProp = properties.SearchForProperty((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignedTo);
            if (assignedToProp == null || assignedToProp.TypedValue.IsNULL())
                return "Unassigned";

            var userName = assignedToProp.TypedValue.GetValueAsLookups()
                .Cast<Lookup>()
                .FirstOrDefault(a => int.Parse(a.DisplayID) == vault.CurrentLoggedInUserID)
                ?.DisplayValue;

            return userName ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}