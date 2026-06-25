export const getPriorityClass = (priority) => {
  const priorityMap = {
    Critical: "assignment-card__priority--critical",
    High: "assignment-card__priority--high",
    Medium: "assignment-card__priority--medium",
    Low: "assignment-card__priority--low",
    "Not Determined Yet": "assignment-card__priority--not-determined-yet"
  };

  return priorityMap[priority] || "";
};

export const getStatusClass = (status) => {
  const statusMap = {
    Assigned: "assignment-card__status--assigned",
    "In Progress": "assignment-card__status--in-progress",
    "On Hold": "assignment-card__status--on-hold",
    "Update Required": "assignment-card__status--update-required",
    "Awaiting Review": "assignment-card__status--awaiting-review",
    Completed: "assignment-card__status--completed",
    Approved: "assignment-card__status--approved",
    Billed: "assignment-card__status--billed"
  };

  return statusMap[status] || "";
};
