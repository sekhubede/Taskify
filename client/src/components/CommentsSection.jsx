import React, { useEffect, useState } from "react";

function CommentsSection({
  assignmentId,
  commentsForAssignment,
  loadingComments,
  filter,
  setCommentFilters,
  commentSortNewestFirst,
  setCommentSortNewestFirst,
  commentFlags,
  commentNotes,
  editingCommentNotes,
  setEditingCommentNotes,
  setCommentNotes,
  sortCommentsByDate,
  currentUser,
  getUserColor,
  formatCommentDate,
  handleToggleCommentFlag,
  handleUpdateCommentNote,
  handleDeleteCommentNote,
  onChecklistSummaryChange,
  newCommentText,
  setNewCommentText,
  handleAddComment
}) {
  const hexToRgba = (hex, opacity) => {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    if (!result) return hex;
    const r = parseInt(result[1], 16);
    const g = parseInt(result[2], 16);
    const b = parseInt(result[3], 16);
    return `rgba(${r}, ${g}, ${b}, ${opacity})`;
  };

  const allComments = commentsForAssignment || [];
  const activeFilter = filter || {};
  const [commentSubtasks, setCommentSubtasks] = useState({});
  const [loadingCommentSubtasks, setLoadingCommentSubtasks] = useState({});
  const [newCommentSubtaskText, setNewCommentSubtaskText] = useState({});
  const [editingChecklistTitles, setEditingChecklistTitles] = useState({});
  const [checklistTitleDrafts, setChecklistTitleDrafts] = useState({});
  const [draggedChecklistItem, setDraggedChecklistItem] = useState(null);
  const [dragOverChecklistItem, setDragOverChecklistItem] = useState(null);

  const summarizeChecklist = (subtaskMap) => {
    const all = allComments.flatMap((comment) => subtaskMap[comment.id] || []);
    return {
      total: all.length,
      completed: all.filter((item) => item.isCompleted).length
    };
  };

  const loadCommentNoteIfNeeded = (commentId) => {
    if (commentNotes[commentId]) return;

    fetch(`http://localhost:5000/api/comments/${commentId}/note`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data?.note) {
          setCommentNotes((prev) => ({
            ...prev,
            [commentId]: data.note
          }));
        }
      })
      .catch((err) => console.error("Error loading comment note:", err));
  };

  const loadCommentSubtasksIfNeeded = (commentId) => {
    if (commentSubtasks[commentId] || loadingCommentSubtasks[commentId]) return;

    setLoadingCommentSubtasks((prev) => ({ ...prev, [commentId]: true }));
    fetch(`http://localhost:5000/api/comments/${commentId}/subtasks`)
      .then((res) => (res.ok ? res.json() : []))
      .then((data) => {
        setCommentSubtasks((prev) => ({ ...prev, [commentId]: data || [] }));
      })
      .catch((err) => console.error("Error loading comment subtasks:", err))
      .finally(() => {
        setLoadingCommentSubtasks((prev) => ({ ...prev, [commentId]: false }));
      });
  };

  useEffect(() => {
    let cancelled = false;

    const loadAllChecklistData = async () => {
      if (!allComments.length) {
        setCommentSubtasks({});
        onChecklistSummaryChange?.({ total: 0, completed: 0 });
        return;
      }

      try {
        const entries = await Promise.all(
          allComments.map(async (comment) => {
            const res = await fetch(
              `http://localhost:5000/api/comments/${comment.id}/subtasks`
            );
            if (!res.ok) return [comment.id, []];
            const items = await res.json();
            return [comment.id, items || []];
          })
        );

        if (cancelled) return;

        const next = {};
        entries.forEach(([commentId, items]) => {
          next[commentId] = items;
        });
        setCommentSubtasks(next);
        onChecklistSummaryChange?.(summarizeChecklist(next));
      } catch (err) {
        if (!cancelled) {
          console.error("Error loading comment checklist data:", err);
        }
      }
    };

    loadAllChecklistData();
    return () => {
      cancelled = true;
    };
  }, [assignmentId, commentsForAssignment]);

  const handleAddCommentSubtask = async (commentId) => {
    const title = (newCommentSubtaskText[commentId] || "").trim();
    if (!title) return;

    try {
      const response = await fetch(
        `http://localhost:5000/api/comments/${commentId}/subtasks`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ title })
        }
      );

      if (!response.ok) throw new Error("Failed to add comment subtask");

      const created = await response.json();
      setCommentSubtasks((prev) => {
        const next = {
          ...prev,
          [commentId]: [...(prev[commentId] || []), created]
        };
        onChecklistSummaryChange?.(summarizeChecklist(next));
        return next;
      });
      setNewCommentSubtaskText((prev) => ({ ...prev, [commentId]: "" }));
    } catch (err) {
      console.error(err);
    }
  };

  const handleToggleCommentSubtask = async (commentId, subtaskId, isCompleted) => {
    try {
      const response = await fetch(
        `http://localhost:5000/api/comments/subtasks/${subtaskId}/toggle`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ isCompleted })
        }
      );
      if (!response.ok) throw new Error("Failed to toggle comment subtask");

      setCommentSubtasks((prev) => {
        const next = {
          ...prev,
          [commentId]: (prev[commentId] || []).map((item) =>
            item.id === subtaskId ? { ...item, isCompleted } : item
          )
        };
        onChecklistSummaryChange?.(summarizeChecklist(next));
        return next;
      });
    } catch (err) {
      console.error(err);
    }
  };

  const handleDeleteCommentSubtask = async (commentId, subtaskId) => {
    try {
      const response = await fetch(
        `http://localhost:5000/api/comments/subtasks/${subtaskId}`,
        { method: "DELETE" }
      );
      if (!response.ok) throw new Error("Failed to delete comment subtask");

      setCommentSubtasks((prev) => {
        const next = {
          ...prev,
          [commentId]: (prev[commentId] || []).filter(
            (item) => item.id !== subtaskId
          )
        };
        onChecklistSummaryChange?.(summarizeChecklist(next));
        return next;
      });
    } catch (err) {
      console.error(err);
    }
  };

  const handleStartChecklistEdit = (subtaskId, currentTitle) => {
    setEditingChecklistTitles((prev) => ({ ...prev, [subtaskId]: true }));
    setChecklistTitleDrafts((prev) => ({
      ...prev,
      [subtaskId]: currentTitle || ""
    }));
  };

  const handleCancelChecklistEdit = (subtaskId) => {
    setEditingChecklistTitles((prev) => ({ ...prev, [subtaskId]: false }));
    setChecklistTitleDrafts((prev) => {
      const next = { ...prev };
      delete next[subtaskId];
      return next;
    });
  };

  const handleSaveChecklistEdit = async (commentId, subtaskId) => {
    const title = (checklistTitleDrafts[subtaskId] || "").trim();
    if (!title) return;

    try {
      const response = await fetch(
        `http://localhost:5000/api/comments/subtasks/${subtaskId}/title`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ title })
        }
      );
      if (!response.ok) throw new Error("Failed to update checklist item");

      setCommentSubtasks((prev) => {
        const next = {
          ...prev,
          [commentId]: (prev[commentId] || []).map((item) =>
            item.id === subtaskId ? { ...item, title } : item
          )
        };
        onChecklistSummaryChange?.(summarizeChecklist(next));
        return next;
      });
      handleCancelChecklistEdit(subtaskId);
    } catch (err) {
      console.error(err);
    }
  };

  const handleChecklistDragStart = (commentId, subtaskId) => {
    setDraggedChecklistItem({ commentId, subtaskId });
  };

  const handleChecklistDragOver = (commentId, subtaskId) => {
    if (!draggedChecklistItem) return;
    if (draggedChecklistItem.commentId !== commentId) return;
    if (draggedChecklistItem.subtaskId === subtaskId) return;
    setDragOverChecklistItem({ commentId, subtaskId });
  };

  const handleChecklistDrop = async (commentId, targetSubtaskId) => {
    if (!draggedChecklistItem) return;
    if (draggedChecklistItem.commentId !== commentId) return;

    const sourceSubtaskId = draggedChecklistItem.subtaskId;
    const items = [...(commentSubtasks[commentId] || [])].sort(
      (a, b) => a.order - b.order
    );
    const sourceIndex = items.findIndex((i) => i.id === sourceSubtaskId);
    const targetIndex = items.findIndex((i) => i.id === targetSubtaskId);
    if (sourceIndex < 0 || targetIndex < 0) return;

    const reordered = [...items];
    const [moved] = reordered.splice(sourceIndex, 1);
    reordered.splice(targetIndex, 0, moved);

    const normalized = reordered.map((item, index) => ({ ...item, order: index }));
    const orderPayload = {};
    normalized.forEach((item, index) => {
      orderPayload[item.id] = index;
    });

    setCommentSubtasks((prev) => {
      const next = { ...prev, [commentId]: normalized };
      onChecklistSummaryChange?.(summarizeChecklist(next));
      return next;
    });

    try {
      await fetch(`http://localhost:5000/api/comments/${commentId}/subtasks/reorder`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ subtaskOrders: orderPayload })
      });
    } catch (err) {
      console.error("Error reordering checklist items:", err);
    } finally {
      setDraggedChecklistItem(null);
      setDragOverChecklistItem(null);
    }
  };

  let filteredComments = allComments;

  if (activeFilter.user) {
    filteredComments = filteredComments.filter(
      (c) => c.authorName === activeFilter.user
    );
  }

  if (activeFilter.date) {
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());

    filteredComments = filteredComments.filter((c) => {
      const commentDate = new Date(c.createdDate);
      const commentDateOnly = new Date(
        commentDate.getFullYear(),
        commentDate.getMonth(),
        commentDate.getDate()
      );

      switch (activeFilter.date) {
        case "today":
          return commentDateOnly.getTime() === today.getTime();
        case "week": {
          const weekAgo = new Date(today);
          weekAgo.setDate(weekAgo.getDate() - 7);
          return commentDateOnly >= weekAgo;
        }
        case "month": {
          const monthAgo = new Date(today);
          monthAgo.setMonth(monthAgo.getMonth() - 1);
          return commentDateOnly >= monthAgo;
        }
        default:
          return true;
      }
    });
  }

  if (activeFilter.flag) {
    if (activeFilter.flag === "flagged") {
      filteredComments = filteredComments.filter(
        (c) => commentFlags[c.id] === true
      );
    } else if (activeFilter.flag === "not-flagged") {
      filteredComments = filteredComments.filter((c) => !commentFlags[c.id]);
    }
  }

  if (activeFilter.note) {
    if (activeFilter.note === "has-note") {
      filteredComments = filteredComments.filter((c) => commentNotes[c.id]);
    } else if (activeFilter.note === "no-note") {
      filteredComments = filteredComments.filter((c) => !commentNotes[c.id]);
    }
  }

  if (activeFilter.checklist) {
    if (activeFilter.checklist === "has-checklist") {
      filteredComments = filteredComments.filter(
        (c) => (commentSubtasks[c.id] || []).length > 0
      );
    } else if (activeFilter.checklist === "no-checklist") {
      filteredComments = filteredComments.filter(
        (c) => (commentSubtasks[c.id] || []).length === 0
      );
    }
  }

  filteredComments = sortCommentsByDate(filteredComments);

  return (
    <div className="comments-section">
      <div className="comments-filters">
        <select
          className="comment-filter-select"
          value={activeFilter.user || ""}
          onChange={(e) =>
            setCommentFilters((prev) => ({
              ...prev,
              [assignmentId]: {
                ...prev[assignmentId],
                user: e.target.value || null
              }
            }))
          }
        >
          <option value="">All Users</option>
          {Array.from(new Set(allComments.map((c) => c.authorName)))
            .sort()
            .map((userName) => (
              <option key={userName} value={userName}>
                {userName}
              </option>
            ))}
        </select>
        <select
          className="comment-filter-select"
          value={activeFilter.date || ""}
          onChange={(e) =>
            setCommentFilters((prev) => ({
              ...prev,
              [assignmentId]: {
                ...prev[assignmentId],
                date: e.target.value || null
              }
            }))
          }
        >
          <option value="">All Dates</option>
          <option value="today">Today</option>
          <option value="week">This Week</option>
          <option value="month">This Month</option>
        </select>
        <select
          className="comment-filter-select"
          value={activeFilter.flag || ""}
          onChange={(e) =>
            setCommentFilters((prev) => ({
              ...prev,
              [assignmentId]: {
                ...prev[assignmentId],
                flag: e.target.value || null
              }
            }))
          }
        >
          <option value="">All Comments</option>
          <option value="flagged">Flagged</option>
          <option value="not-flagged">Not Flagged</option>
        </select>
        <select
          className="comment-filter-select"
          value={activeFilter.note || ""}
          onChange={(e) =>
            setCommentFilters((prev) => ({
              ...prev,
              [assignmentId]: {
                ...prev[assignmentId],
                note: e.target.value || null
              }
            }))
          }
        >
          <option value="">All Comments</option>
          <option value="has-note">Has Personal Note</option>
          <option value="no-note">No Personal Note</option>
        </select>
        <select
          className="comment-filter-select"
          value={activeFilter.checklist || ""}
          onChange={(e) =>
            setCommentFilters((prev) => ({
              ...prev,
              [assignmentId]: {
                ...prev[assignmentId],
                checklist: e.target.value || null
              }
            }))
          }
        >
          <option value="">All Checklists</option>
          <option value="has-checklist">Has Checklist</option>
          <option value="no-checklist">No Checklist</option>
        </select>
        <button
          className="comment-sort-toggle"
          onClick={() => setCommentSortNewestFirst(!commentSortNewestFirst)}
          title={
            commentSortNewestFirst
              ? "Switch to oldest comments first"
              : "Switch to newest comments first"
          }
        >
          {commentSortNewestFirst ? "Sort: Newest first" : "Sort: Oldest first"}
        </button>
        {(activeFilter.user ||
          activeFilter.date ||
          activeFilter.flag ||
          activeFilter.note ||
          activeFilter.checklist) && (
          <button
            className="comment-filter-clear"
            onClick={() =>
              setCommentFilters((prev) => ({
                ...prev,
                [assignmentId]: {
                  user: null,
                  date: null,
                  flag: null,
                  note: null,
                  checklist: null
                }
              }))
            }
          >
            Clear Filters
          </button>
        )}
      </div>

      <div className="comments-list">
        {loadingComments ? (
          <div className="comments-loading">Loading comments...</div>
        ) : filteredComments.length > 0 ? (
          filteredComments.map((comment) => {
            const isCurrentUser =
              currentUser && comment.authorName === currentUser;
            const userColor = getUserColor(comment.authorName);

            return (
              <div
                key={comment.id}
                className="comment-bubble"
                style={{
                  "--user-color": userColor,
                  backgroundColor: isCurrentUser
                    ? hexToRgba(userColor, 0.11)
                    : "rgba(255, 255, 255, 0.08)",
                  borderColor: isCurrentUser
                    ? hexToRgba(userColor, 0.22)
                    : "rgba(255, 255, 255, 0.1)"
                }}
              >
                <div className="comment-header">
                  <span className="comment-author" style={{ color: userColor }}>
                    {comment.authorName}
                  </span>
                  <span className="comment-date">
                    {formatCommentDate(comment.createdDate)}
                  </span>
                  <button
                    className={`comment-flag-button ${commentFlags[comment.id] ? "flagged" : ""}`}
                    onClick={() =>
                      handleToggleCommentFlag(
                        comment.id,
                        !commentFlags[comment.id]
                      )
                    }
                    title={
                      commentFlags[comment.id] ? "Unflag comment" : "Flag comment"
                    }
                  >
                    {commentFlags[comment.id] ? "🚩" : "🏳️"}
                  </button>
                  <button
                    className="comment-note-button"
                    onClick={() => {
                      setEditingCommentNotes((prev) => ({
                        ...prev,
                        [comment.id]: !prev[comment.id]
                      }));
                      loadCommentNoteIfNeeded(comment.id);
                      loadCommentSubtasksIfNeeded(comment.id);
                    }}
                    title={
                      commentNotes[comment.id]
                        ? "Edit personal note"
                        : "Add personal note"
                    }
                  >
                    {commentNotes[comment.id] ? "📝" : "📄"}
                  </button>
                </div>
                <div className="comment-content">{comment.content}</div>

                {editingCommentNotes[comment.id] && (
                  <div className="comment-note-editor">
                    <textarea
                      className="comment-note-input"
                      placeholder="Add a personal note about this comment..."
                      value={commentNotes[comment.id] || ""}
                      onChange={(e) =>
                        setCommentNotes((prev) => ({
                          ...prev,
                          [comment.id]: e.target.value
                        }))
                      }
                      rows={3}
                    />
                    <div className="comment-note-actions">
                      <button
                        className="comment-note-save"
                        onClick={() =>
                          handleUpdateCommentNote(comment.id, commentNotes[comment.id])
                        }
                      >
                        Save
                      </button>
                      <button
                        className="comment-note-cancel"
                        onClick={() =>
                          setEditingCommentNotes((prev) => ({
                            ...prev,
                            [comment.id]: false
                          }))
                        }
                      >
                        Cancel
                      </button>
                    </div>
                  </div>
                )}

                {!editingCommentNotes[comment.id] && commentNotes[comment.id] && (
                  <div className="comment-note-display">
                    <div className="comment-note-header">
                      <span className="comment-note-label">Personal Note:</span>
                      <button
                        className="comment-note-delete-button"
                        onClick={() => handleDeleteCommentNote(comment.id)}
                        title="Delete personal note"
                      >
                        🗑️
                      </button>
                    </div>
                    <span className="comment-note-text">
                      {commentNotes[comment.id]}
                    </span>
                  </div>
                )}

                <div className="comment-subtasks">
                  <div className="comment-subtasks-header">
                    <span className="comment-subtasks-label">Checklist</span>
                  </div>

                  {loadingCommentSubtasks[comment.id] ? (
                    <div className="comment-subtasks-loading">Loading checklist...</div>
                  ) : (
                    <>
                      {(commentSubtasks[comment.id] || []).length > 0 && (
                        <div className="comment-subtasks-list">
                          {(commentSubtasks[comment.id] || [])
                            .slice()
                            .sort((a, b) => a.order - b.order)
                            .map((item) => (
                              <div key={item.id} className="comment-subtask-item">
                                <label
                                  className="comment-subtask-checkbox-label"
                                  draggable
                                  onDragStart={() =>
                                    handleChecklistDragStart(comment.id, item.id)
                                  }
                                  onDragOver={(e) => {
                                    e.preventDefault();
                                    handleChecklistDragOver(comment.id, item.id);
                                  }}
                                  onDrop={(e) => {
                                    e.preventDefault();
                                    handleChecklistDrop(comment.id, item.id);
                                  }}
                                >
                                  <input
                                    type="checkbox"
                                    checked={item.isCompleted}
                                    onChange={(e) =>
                                      handleToggleCommentSubtask(
                                        comment.id,
                                        item.id,
                                        e.target.checked
                                      )
                                    }
                                  />
                                  {editingChecklistTitles[item.id] ? (
                                    <input
                                      className="comment-subtask-title-input"
                                      type="text"
                                      value={checklistTitleDrafts[item.id] || ""}
                                      onChange={(e) =>
                                        setChecklistTitleDrafts((prev) => ({
                                          ...prev,
                                          [item.id]: e.target.value
                                        }))
                                      }
                                      onKeyDown={(e) => {
                                        if (e.key === "Enter") {
                                          e.preventDefault();
                                          handleSaveChecklistEdit(comment.id, item.id);
                                        } else if (e.key === "Escape") {
                                          e.preventDefault();
                                          handleCancelChecklistEdit(item.id);
                                        }
                                      }}
                                      autoFocus
                                    />
                                  ) : (
                                    <span
                                      className={
                                        item.isCompleted
                                          ? "comment-subtask-title completed"
                                          : "comment-subtask-title"
                                      }
                                    >
                                      {item.title}
                                    </span>
                                  )}
                                </label>
                                {editingChecklistTitles[item.id] ? (
                                  <>
                                    <button
                                      className="comment-subtask-edit"
                                      onClick={() =>
                                        handleSaveChecklistEdit(comment.id, item.id)
                                      }
                                      title="Save checklist item"
                                    >
                                      ✓
                                    </button>
                                    <button
                                      className="comment-subtask-edit"
                                      onClick={() => handleCancelChecklistEdit(item.id)}
                                      title="Cancel edit"
                                    >
                                      ↺
                                    </button>
                                  </>
                                ) : (
                                  <button
                                    className="comment-subtask-edit"
                                    onClick={() =>
                                      handleStartChecklistEdit(item.id, item.title)
                                    }
                                    title="Edit checklist item"
                                  >
                                    ✎
                                  </button>
                                )}
                                <button
                                  className="comment-subtask-delete"
                                  onClick={() =>
                                    handleDeleteCommentSubtask(comment.id, item.id)
                                  }
                                  title="Delete checklist item"
                                >
                                  ✕
                                </button>
                              </div>
                            ))}
                        </div>
                      )}

                      <div className="comment-subtasks-add">
                        <input
                          className="comment-subtask-input"
                          type="text"
                          placeholder="Add checklist item..."
                          value={newCommentSubtaskText[comment.id] || ""}
                          onChange={(e) =>
                            setNewCommentSubtaskText((prev) => ({
                              ...prev,
                              [comment.id]: e.target.value
                            }))
                          }
                          onFocus={() => loadCommentSubtasksIfNeeded(comment.id)}
                          onKeyDown={(e) => {
                            if (e.key === "Enter") {
                              e.preventDefault();
                              handleAddCommentSubtask(comment.id);
                            }
                          }}
                        />
                        <button
                          className="comment-subtask-add-button"
                          onClick={() => handleAddCommentSubtask(comment.id)}
                          disabled={!newCommentSubtaskText[comment.id]?.trim()}
                        >
                          Add
                        </button>
                      </div>
                    </>
                  )}
                </div>
              </div>
            );
          })
        ) : (
          <div className="comments-empty">
            {allComments.length === 0
              ? "No comments yet. Be the first to comment!"
              : "No comments match the selected filters."}
          </div>
        )}
      </div>

      <div className="comment-input-container">
        <textarea
          className="comment-input"
          placeholder="Add a comment..."
          value={newCommentText || ""}
          onChange={(e) =>
            setNewCommentText((prev) => ({
              ...prev,
              [assignmentId]: e.target.value
            }))
          }
          onKeyDown={(e) => {
            if (e.key === "Enter" && (e.ctrlKey || e.metaKey)) {
              e.preventDefault();
              handleAddComment(assignmentId);
            }
          }}
          rows={3}
        />
        <button
          className="comment-submit-button"
          onClick={() => handleAddComment(assignmentId)}
          disabled={!newCommentText?.trim()}
        >
          Post
        </button>
      </div>
    </div>
  );
}

export default CommentsSection;
