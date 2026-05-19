import { PRIORITY_LABELS } from '../data/assignmentsData';

function AssignmentCard({assignment}) {
   return (
    <div>
      <h3>{assignment.name}</h3>
      <p>{assignment.description}</p>
      <p>{assignment.client}</p>
      <p>{PRIORITY_LABELS[assignment.priority]}</p>
      <p>{assignment.status}</p>
      <p>{assignment.assignee}</p>
      <p>{assignment.deadline}</p>
    </div>
  );
}

export default AssignmentCard;
