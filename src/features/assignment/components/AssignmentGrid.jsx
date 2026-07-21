import AssignmentCard from "./AssignmentCard";
import { ASSIGNMENTS } from "../data/data";
import { useState } from 'react';
import DetailPanel from './DetailPanel';

function AssignmentGrid() {
  const [selectedAssignment, setSelectedAssignment] = useState(null);
  const [isPanelOpen, setIsPanelOpen] = useState(false);

  const handleCardClick = (assignment) => {
    setSelectedAssignment(assignment);
    setIsPanelOpen(true);
  }

  const handleClosePanel = () => {
    setIsPanelOpen(false);
    setTimeout(() => setSelectedAssignment(null), 300);
  }

  return (
    <div>
      <ul className="assignment-grid">
        {ASSIGNMENTS.map((assignment) => (
          <AssignmentCard 
            key={assignment.id} 
            assignment={assignment} 
            onCardClick={handleCardClick}
          />
        ))}
      </ul>

      <DetailPanel 
        assignment={selectedAssignment}
        isOpen={isPanelOpen}
        onClose={handleClosePanel}
      />
    </div>
  );
}

export default AssignmentGrid;
