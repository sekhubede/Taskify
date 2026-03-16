import React from "react";

function WorkingOnSection({ assignments, renderCard }) {
  if (!assignments.length) return null;

  return (
    <div className="working-on-section">
      <div className="working-on-header">
        <h3 className="working-on-title">🔥 Hot Zone</h3>
        <span className="working-on-count">{assignments.length}</span>
      </div>
      {assignments.map((assignment) => renderCard(assignment))}
    </div>
  );
}

export default WorkingOnSection;
