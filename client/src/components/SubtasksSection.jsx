import React from "react";

function SubtasksSection({
  assignmentId,
  subtasksForAssignment,
  loadingSubtasks,
  draggedSubtaskId,
  dragOverSubtaskId,
  editingSubtasks,
  subtaskTitleDrafts,
  editingNotes,
  subtaskNotes,
  subtaskNoteTimestamps,
  handleDragStart,
  handleDragOver,
  handleDragLeave,
  handleDrop,
  handleDragEnd,
  handleToggleSubtask,
  setSubtaskTitleDrafts,
  handleSaveSubtaskTitle,
  handleCancelSubtaskEdit,
  handleStartSubtaskEdit,
  setEditingNotes,
  setSubtaskNotes,
  handleDeleteSubtask,
  handleUpdateSubtaskNote,
  handleDeleteSubtaskNote,
  newSubtaskText,
  setNewSubtaskText,
  handleAddSubtask
}) {
  const assignmentSubtasks = subtasksForAssignment || [];
  const formatLocalTimestamp = (value) => {
    if (!value) return "";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return "";
    return date.toLocaleString();
  };

  const formatAuditLabel = (createdDate, updatedDate) => {
    const created = formatLocalTimestamp(createdDate);
    const updated = formatLocalTimestamp(updatedDate);
    if (updated && created && updated !== created) {
      return `Added ${created} • Updated ${updated}`;
    }
    if (updated) return `Added ${updated}`;
    if (created) return `Added ${created}`;
    return "";
  };

  return (
    <div className="subtasks-section">
      <div className="subtasks-list">
        {loadingSubtasks ? (
          <div className="subtasks-loading">Loading subtasks...</div>
        ) : assignmentSubtasks.length > 0 ? (
          [...assignmentSubtasks]
            .sort((a, b) => a.isCompleted - b.isCompleted)
            .map((subtask) => (
              <div
                key={subtask.id}
                className={`subtask-item ${draggedSubtaskId === subtask.id ? "dragging" : ""} ${dragOverSubtaskId === subtask.id ? "drag-over" : ""}`}
                draggable
                onDragStart={(e) => handleDragStart(e, subtask.id)}
                onDragOver={(e) => handleDragOver(e, subtask.id)}
                onDragLeave={handleDragLeave}
                onDrop={(e) => handleDrop(e, subtask.id, assignmentId)}
                onDragEnd={handleDragEnd}
              >
                <div className="subtask-drag-handle">⋮⋮</div>
                <div className="subtask-content">
                  <div className="subtask-main">
                    <label className="subtask-checkbox-label">
                      <input
                        type="checkbox"
                        className="subtask-checkbox"
                        checked={subtask.isCompleted}
                        onChange={(e) =>
                          handleToggleSubtask(subtask.id, e.target.checked)
                        }
                        onMouseDown={(e) => e.stopPropagation()}
                      />
                      {editingSubtasks[subtask.id] ? (
                        <input
                          type="text"
                          className="subtask-title-input"
                          value={subtaskTitleDrafts[subtask.id] ?? ""}
                          onChange={(e) =>
                            setSubtaskTitleDrafts((prev) => ({
                              ...prev,
                              [subtask.id]: e.target.value
                            }))
                          }
                          onKeyDown={(e) => {
                            if (e.key === "Enter") {
                              e.preventDefault();
                              handleSaveSubtaskTitle(subtask.id);
                            } else if (e.key === "Escape") {
                              e.preventDefault();
                              handleCancelSubtaskEdit(subtask.id);
                            }
                          }}
                          onMouseDown={(e) => e.stopPropagation()}
                          onClick={(e) => e.stopPropagation()}
                          autoFocus
                        />
                      ) : (
                        <div className="subtask-title-block">
                          <span
                            className={`subtask-title ${subtask.isCompleted ? "completed" : ""}`}
                          >
                            {subtask.title}
                          </span>
                          <span className="item-audit-stamp">
                            {formatAuditLabel(
                              subtask.createdDate,
                              subtask.updatedDate
                            )}
                          </span>
                        </div>
                      )}
                    </label>
                    {editingSubtasks[subtask.id] ? (
                      <>
                        <button
                          className="subtask-edit-save"
                          onMouseDown={(e) => e.stopPropagation()}
                          onClick={() => handleSaveSubtaskTitle(subtask.id)}
                          title="Save title"
                        >
                          Save
                        </button>
                        <button
                          className="subtask-edit-cancel"
                          onMouseDown={(e) => e.stopPropagation()}
                          onClick={() => handleCancelSubtaskEdit(subtask.id)}
                          title="Cancel edit"
                        >
                          Cancel
                        </button>
                      </>
                    ) : (
                      <button
                        className="subtask-edit-button"
                        onMouseDown={(e) => e.stopPropagation()}
                        onClick={() => handleStartSubtaskEdit(subtask)}
                        title="Edit subtask"
                      >
                        ✏️
                      </button>
                    )}
                    <button
                      className="subtask-note-button"
                      onMouseDown={(e) => e.stopPropagation()}
                      onClick={() => {
                        setEditingNotes((prev) => ({
                          ...prev,
                          [subtask.id]: !prev[subtask.id]
                        }));
                        if (!subtaskNotes[subtask.id] && subtask.personalNote) {
                          setSubtaskNotes((prev) => ({
                            ...prev,
                            [subtask.id]: subtask.personalNote
                          }));
                        }
                      }}
                      title={
                        subtaskNotes[subtask.id] || subtask.personalNote
                          ? "Edit note"
                          : "Add note"
                      }
                    >
                      {subtaskNotes[subtask.id] || subtask.personalNote
                        ? "📝"
                        : "📄"}
                    </button>
                    <button
                      className="subtask-delete-button"
                      onMouseDown={(e) => e.stopPropagation()}
                      onClick={() => {
                        if (
                          window.confirm(
                            "Are you sure you want to delete this subtask?"
                          )
                        ) {
                          handleDeleteSubtask(subtask.id, assignmentId);
                        }
                      }}
                      title="Delete subtask"
                    >
                      🗑️
                    </button>
                  </div>
                  {editingNotes[subtask.id] && (
                    <div className="subtask-note-editor">
                      <textarea
                        className="subtask-note-input"
                        placeholder="Add a personal note..."
                        value={
                          subtaskNotes[subtask.id] ?? subtask.personalNote ?? ""
                        }
                        onChange={(e) =>
                          setSubtaskNotes((prev) => ({
                            ...prev,
                            [subtask.id]: e.target.value
                          }))
                        }
                        rows={3}
                      />
                      <div className="subtask-note-actions">
                        <button
                          className="subtask-note-save"
                          onClick={() =>
                            handleUpdateSubtaskNote(
                              subtask.id,
                              subtaskNotes[subtask.id]
                            )
                          }
                        >
                          Save
                        </button>
                        <button
                          className="subtask-note-cancel"
                          onClick={() => {
                            setEditingNotes((prev) => ({
                              ...prev,
                              [subtask.id]: false
                            }));
                            setSubtaskNotes((prev) => {
                              const updated = { ...prev };
                              if (subtask.personalNote) {
                                updated[subtask.id] = subtask.personalNote;
                              } else {
                                delete updated[subtask.id];
                              }
                              return updated;
                            });
                          }}
                        >
                          Cancel
                        </button>
                      </div>
                    </div>
                  )}
                  {!editingNotes[subtask.id] &&
                    (subtaskNotes[subtask.id] || subtask.personalNote) && (
                      <div className="subtask-note-display">
                        <div className="subtask-note-header">
                          <span className="subtask-note-label">Note:</span>
                          <button
                            className="subtask-note-delete-button"
                            onClick={() => handleDeleteSubtaskNote(subtask.id)}
                            title="Delete personal note"
                          >
                            🗑️
                          </button>
                        </div>
                        <span className="subtask-note-text">
                          {subtaskNotes[subtask.id] || subtask.personalNote}
                        </span>
                        {subtaskNoteTimestamps?.[subtask.id] && (
                          <span className="item-audit-stamp">
                            {formatAuditLabel(
                              subtaskNoteTimestamps[subtask.id].createdDate,
                              subtaskNoteTimestamps[subtask.id].updatedDate
                            )}
                          </span>
                        )}
                      </div>
                    )}
                </div>
              </div>
            ))
        ) : (
          <div className="subtasks-empty">No subtasks yet. Add one below!</div>
        )}
      </div>

      <div className="subtask-input-container">
        <input
          type="text"
          className="subtask-input"
          placeholder="Add a subtask..."
          value={newSubtaskText || ""}
          onChange={(e) =>
            setNewSubtaskText((prev) => ({
              ...prev,
              [assignmentId]: e.target.value
            }))
          }
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault();
              handleAddSubtask(assignmentId);
            }
          }}
        />
        <button
          className="subtask-submit-button"
          onClick={() => handleAddSubtask(assignmentId)}
          disabled={!newSubtaskText?.trim()}
        >
          Add
        </button>
      </div>
    </div>
  );
}

export default SubtasksSection;
