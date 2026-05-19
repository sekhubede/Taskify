import AssignmentCard from './AssignmentCard';
import { assignments } from '../data/assignmentsData';

  function AssignmentList() {
    return (
        <div>
            <h2>Assignments</h2>
            {assignments.map((assignment) => (
                <AssignmentCard key={assignment.id} assignment={assignment}/>
            ))}
        </div>
    )
  }

  export default AssignmentList;