import React from "react";

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
