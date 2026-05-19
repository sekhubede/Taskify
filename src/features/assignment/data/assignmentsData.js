/**
 * @typedef {object} Assignment
 * @property {number} id - M-Files Object ID
 * @property {string} name - Assignment name
 * @property {string} description - Assignment description
 * @property {string} client - O'Neil client name
 * @property {number} priority - 1=Critical, 2=High, 3=Medium, 4=Low, 5=Not Determined
 * @property {string} status - M-Files workflow state. See ASSIGNMENT_STATES
 * @property {string} assignee - Assigned team member
 * @property {string} deadline - ISO 8601 date string
 */

export const PRIORITY_LABELS = {
  1: "Critical",
  2: "High",
  3: "Medium",
  4: "Low",
  5: "Not Determined Yet"
};

export const ASSIGNMENTS_STATES = {
  ASSIGNED: "Assigned",
  // User reviews description, moves to In Progress when understood
  // TODO: V2 - AI breakdown of assignment into actionables

  IN_PROGRESS: "In Progress",
  // User is actively working on the assignment

  ON_HOLD: "On Hold",
  // Blocked - waiting on client response or external dependency

  UPDATE_REQUIRED: "Update Required",
  // Manager/HOD needs an update from assignee
  // TODO: v2 - UI alert to replace current email notification (email unreliable)

  AWAITING_REVIEW: "Awaiting Review",
  // User submits for review, Approval Assignment created and linked
  // Both team lead and HOD must approve
  // On rejection: returns to In Progress, user checks assignment + approval for comments

  APPROVED: "Approved",
  // Dual approval complete, user can mark as complete

  COMPLETED: "Completed",
  // Assignment marked complete by user

  BILLED: "Billed"
  // Completed and billed, archive state
};

export const assignments = [
  {
    id: 1,
    name: "Assignment Name",
    description: "Assignment Description",
    client: "O'Neil Client",
    priority: 3,
    status: ASSIGNMENTS_STATES.IN_PROGRESS,
    assignee: "John Doe",
    deadline: "2025-06-01"
  }
];
