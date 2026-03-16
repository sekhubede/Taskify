import React, { useState } from "react";

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
      setCommentSubtasks((prev) => ({
        ...prev,
        [commentId]: [...(prev[commentId] || []), created]
      }));
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

      setCommentSubtasks((prev) => ({
        ...prev,
        [commentId]: (prev[commentId] || []).map((item) =>
          item.id === subtaskId ? { ...item, isCompleted } : item
        )
      }));
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

      setCommentSubtasks((prev) => ({
        ...prev,
        [commentId]: (prev[commentId] || []).filter((item) => item.id !== subtaskId)
      }));
    } catch (err) {
      console.error(err);
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
          activeFilter.note) && (
          <button
            className="comment-filter-clear"
            onClick={() =>
              setCommentFilters((prev) => ({
                ...prev,
                [assignmentId]: {
                  user: null,
                  date: null,
                  flag: null,
                  note: null
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
                                <label className="comment-subtask-checkbox-label">
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
                                  <span
                                    className={
                                      item.isCompleted
                                        ? "comment-subtask-title completed"
                                        : "comment-subtask-title"
                                    }
                                  >
                                    {item.title}
                                  </span>
                                </label>
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
