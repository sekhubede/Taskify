import AssignmentCard from './AssignmentCard';

 const assignmentList = [
    {
      id: 1,
      title: "Assignment 1",
      description: "Description 1",
      dueDate: "2026-01-01",
      status: "In Progress"
    },
    {
      id: 2,
      title: "Assignment 2",
      description: "Description 2",
      dueDate: "2026-01-02",
      status: "Pending"
    },

    {
      id: 3,
      title: "Assignment 3",
      description: "Description 3",
      dueDate: "2026-01-03",
      status: "Done"
    }
  ];

  function AssignmentList() {
    return (
        <div>
            <h2>Assignments</h2>
            {assignmentList.map((assignment) => (
                <AssignmentCard key={assignment.id} assignment={assignment}/>
            ))}
        </div>
    )
  }

  export default AssignmentList;