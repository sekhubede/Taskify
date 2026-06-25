import AssignmentCard from "./AssignmentCard";
import { ASSIGNMENTS } from "../data/data";

function AssignmentList() {
  return (
    <div>
      <h2>Assignments</h2>
      <ul className="assignment-list">
        {ASSIGNMENTS.map((assignment) => (
          <AssignmentCard key={assignment.id} assignment={assignment} />
        ))}
      </ul>
    </div>
  );
}

export default AssignmentList;
