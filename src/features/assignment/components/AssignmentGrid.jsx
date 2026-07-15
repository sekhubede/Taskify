import AssignmentCard from "./AssignmentCard";
import { ASSIGNMENTS } from "../data/data";

function AssignmentGrid() {
  return (
    <div>
      <ul className="assignment-grid">
        {ASSIGNMENTS.map((assignment) => (
          <AssignmentCard key={assignment.id} assignment={assignment} />
        ))}
      </ul>
    </div>
  );
}

export default AssignmentGrid;
