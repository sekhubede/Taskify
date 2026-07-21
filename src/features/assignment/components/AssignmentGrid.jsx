import AssignmentCard from "./AssignmentCard";
import { ASSIGNMENTS, TABS } from "../data/data";
import { useState } from 'react';
import DetailPanel from './DetailPanel';

function AssignmentGrid() {
  const [activeTab, setActiveTab] = useState("today");
  const [selectedAssignment, setSelectedAssignment] = useState(null);
  const [isPanelOpen, setIsPanelOpen] = useState(false);

  const getFilteredAssignments = () => {
    if (activeTab === "today") {
      return ASSIGNMENTS.filter((a) => a.today);
    } else if (activeTab === "week") {
      return ASSIGNMENTS.filter((a) => a.thisWeek);
    } else {
      return ASSIGNMENTS;
    }
  };

  const handleCardClick = (assignment) => {
    setSelectedAssignment(assignment);
    setIsPanelOpen(true);
  };

  const handleClosePanel = () => {
    setIsPanelOpen(false);
    setTimeout(() => setSelectedAssignment(null), 300);
  };

  const filtered = getFilteredAssignments();

  return (
    <div>
      <div className="tabs">
        <button
          className={`tab-btn ${activeTab === TABS.TODAY ? "active" : ""}`}
          onClick={() => setActiveTab(TABS.TODAY)}
        >
          Today
          <span className="tab-count">
            {ASSIGNMENTS.filter((a) => a.today).length}
          </span>
        </button>
        <button
          className={`tab-btn ${activeTab === TABS.THIS_WEEK ? "active" : ""}`}
          onClick={() => setActiveTab(TABS.THIS_WEEK)}
        >
          This Week
          <span className="tab-count">
            {ASSIGNMENTS.filter((a) => a.thisWeek).length}
          </span>
        </button>
        <button
          className={`tab-btn ${activeTab === TABS.ALL ? "active" : ""}`}
          onClick={() => setActiveTab(TABS.ALL)}
        >
          All Assignments
          <span className="tab-count">{ASSIGNMENTS.length}</span>
        </button>

      </div>

      <ul className="assignment-grid">
        {filtered.map((assignment) => (
          <AssignmentCard 
            key={assignment.id} 
            assignment={assignment} 
            onCardClick={handleCardClick}
          />
        ))}
      </ul>

      {filtered.length === 0 && (
        <p className="tab-empty-state">No assignments for this period</p>
      )}

      <DetailPanel 
        assignment={selectedAssignment}
        isOpen={isPanelOpen}
        onClose={handleClosePanel}
      />
    </div>
  );
}

export default AssignmentGrid;
