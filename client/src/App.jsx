import React, { useEffect, useState } from "react";
import "./App.css";

const ASSIGNMENTS_API_URL = "http://localhost:5000/api/assignments";

function App() {
  const [assignments, setAssignments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetch(ASSIGNMENTS_API_URL)
      .then((res) => {
        if (!res.ok) throw new Error("Failed to fetch assignments");
        return res.json();
      })
      .then((data) => {
        setAssignments(data);
        setLoading(false);
      })
      .catch((err) => {
        setError(err.toString());
        setLoading(false);
      });
  }, []);

  const formatDate = (dateString) => {
    if (!dateString) return null;
    const date = new Date(dateString);
    return date.toLocaleDateString("en-US", { 
      month: "short", 
      day: "numeric",
      year: date.getFullYear() !== new Date().getFullYear() ? "numeric" : undefined
    });
  };

  const getStatusColor = (status) => {
    const statusMap = {
      0: "#6b7280", // NotStarted - gray
      1: "#3b82f6", // InProgress - blue
      2: "#10b981", // Completed - green
      3: "#f59e0b", // OnHold - amber
      4: "#8b5cf6", // WaitingForInformation - purple
      5: "#ec4899", // WaitingForFeedback - pink
      6: "#6366f1", // WaitingForReview - indigo
    };
    return statusMap[status] || "#6b7280";
  };

  const getStatusLabel = (status) => {
    const statusMap = {
      0: "Not Started",
      1: "In Progress",
      2: "Completed",
      3: "On Hold",
      4: "Waiting for Information",
      5: "Waiting for Feedback",
      6: "Waiting for Review",
    };
    return statusMap[status] || "Unknown";
  };

  const isOverdue = (dueDate, status) => {
    if (!dueDate || status === 2) return false;
    return new Date(dueDate) < new Date();
  };

  const handleCompleteAssignment = async (id) => {
    try {
      const response = await fetch(`${ASSIGNMENTS_API_URL}/${id}/complete`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to mark assignment as complete');
      }

      // Update the assignment in local state
      setAssignments(prevAssignments =>
        prevAssignments.map(assignment =>
          assignment.id === id
            ? { ...assignment, status: 2 } // 2 = Completed
            : assignment
        )
      );
    } catch (err) {
      setError(err.toString());
    }
  };

  return (
    <div className="app">
      <header className="app-header">
        <h1>Taskify</h1>
        <p className="subtitle">Your assignments</p>
      </header>

      {loading && (
        <div className="loading">
          <p>Loading assignments...</p>
        </div>
      )}

      {error && (
        <div className="error">
          <p>Error: {error}</p>
        </div>
      )}

      {!loading && !error && (
        <div className="assignments-container">
          {assignments.length === 0 ? (
            <div className="empty-state">
              <p>No assignments found</p>
            </div>
          ) : (
            <div className="assignments-list">
              {assignments.map((assignment) => (
                <div key={assignment.id} className="assignment-card">
                  <div className="assignment-header">
                    <h2 className="assignment-title">{assignment.title}</h2>
                    <span 
                      className="status-badge"
                      style={{ backgroundColor: getStatusColor(assignment.status) }}
                    >
                      {getStatusLabel(assignment.status)}
                    </span>
                  </div>
                  
                  {assignment.description && (
                    <p className="assignment-description">{assignment.description}</p>
                  )}

                  <div className="assignment-meta">
                    {assignment.dueDate && (
                      <span className={`due-date ${isOverdue(assignment.dueDate, assignment.status) ? 'overdue' : ''}`}>
                        {isOverdue(assignment.dueDate, assignment.status) ? '⚠ ' : ''}
                        Due {formatDate(assignment.dueDate)}
                      </span>
                    )}
                    {assignment.subtasks && assignment.subtasks.length > 0 && (
                      <span className="subtask-count">
                        {assignment.subtasks.filter(s => s.isCompleted).length} / {assignment.subtasks.length} subtasks
                      </span>
                    )}
                    {assignment.status !== 2 && (
                      <button
                        className="complete-button"
                        onClick={() => handleCompleteAssignment(assignment.id)}
                      >
                        Complete
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export default App;