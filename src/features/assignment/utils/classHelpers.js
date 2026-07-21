export const getPriorityClass = (priority) => {
  const priorityMap = {
    Critical: "priority-critical",
    High: "priority-high",
    Medium: "priority-medium",
    Low: "priority-low",
    "Not Determined Yet": "priority-not-determined"
  };

  return priorityMap[priority] || "";
};

export const getStatusClass = (status) => {
  const statusMap = {
    Assigned: "status-assigned",
    "In Progress": "status-in-progress",
    "On Hold": "status-on-hold",
    "Update Required": "status-update-required",
    "Awaiting Review": "status-awaiting-review",
    Completed: "status-completed",
    Approved: "status-approved",
    Billed: "status-billed"
  };

  return statusMap[status] || "";
};
