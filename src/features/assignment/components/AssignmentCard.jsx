import { PRIORITY_LABELS, ASSIGNMENT_STATES } from "../data/data";
import { getPriorityClass, getStatusClass } from "../utils/classHelpers";
import { parseDDMMYYYY, formatDate } from "../utils/dateUtils";

function AssignmentCard({ assignment }) {
  const assignmentDeadline = parseDDMMYYYY(assignment.deadline);

  const isOverdue =
    assignmentDeadline !== null &&
    assignmentDeadline < new Date() &&
    assignment.status !== ASSIGNMENT_STATES.COMPLETED &&
    assignment.status !== ASSIGNMENT_STATES.BILLED;

  const deadlineClass = isOverdue ? "assignment-card__deadline--overdue" : "";
  const deadlineDate = assignmentDeadline
    ? formatDate(assignmentDeadline)
    : "No deadline set";

  return (
    <li
      className={`assignment-card ${getPriorityClass(PRIORITY_LABELS[assignment.priority])}`}
    >
      <h3 className="assignment-card__title">{assignment.title}</h3>
      <p className="assignment-card__description">{assignment.description}</p>
      <p
        className={`assignment-card__status ${getStatusClass(assignment.status)}`}
      >
        {assignment.status}
      </p>
      <div className="assignment-card__info">
        <p>{assignment.assignee}</p>
        <p className="assignment-card__client">{assignment.client}</p>
        <p className={`assignment-card__deadline ${deadlineClass}`}>
          Due: {deadlineDate}
        </p>
      </div>
    </li>
  );
}

export default AssignmentCard;
