function AssignmentCard({assignment}) {
   return (
    <div>
      <h3>{assignment.title}</h3>
      <p>{assignment.description}</p>
      <p>{assignment.dueDate}</p>
      <p>{assignment.status}</p>
    </div>
  );
}

export default AssignmentCard;
