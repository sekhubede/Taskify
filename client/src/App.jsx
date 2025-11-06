import React, { useEffect, useState } from "react";
import "./App.css";

const ASSIGNMENTS_API_URL = "http://localhost:5000/api/assignments";

function App() {
  const [assignments, setAssignments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [openComments, setOpenComments] = useState({});
  const [comments, setComments] = useState({});
  const [loadingComments, setLoadingComments] = useState({});
  const [newCommentText, setNewCommentText] = useState({});
  const [currentUser, setCurrentUser] = useState(null);
  const [commentCounts, setCommentCounts] = useState({});
  const [commentFilters, setCommentFilters] = useState({});
  const [openSubtasks, setOpenSubtasks] = useState({});
  const [subtasks, setSubtasks] = useState({});
  const [loadingSubtasks, setLoadingSubtasks] = useState({});
  const [newSubtaskText, setNewSubtaskText] = useState({});
  const [editingNotes, setEditingNotes] = useState({});
  const [subtaskNotes, setSubtaskNotes] = useState({});
  const [editingCommentNotes, setEditingCommentNotes] = useState({});
  const [commentNotes, setCommentNotes] = useState({});
  const [commentFlags, setCommentFlags] = useState({});

  useEffect(() => {
    // Fetch current user
    fetch("http://localhost:5000/api/user/current")
      .then((res) => {
        if (!res.ok) throw new Error("Failed to fetch current user");
        return res.json();
      })
      .then((data) => {
        setCurrentUser(data.userName);
      })
      .catch((err) => {
        console.error("Error fetching current user:", err);
      });

    // Fetch assignments
    fetch(ASSIGNMENTS_API_URL)
      .then((res) => {
        if (!res.ok) throw new Error("Failed to fetch assignments");
        return res.json();
      })
      .then((data) => {
        setAssignments(data);
        setLoading(false);
        
        // Fetch comment counts for all assignments
        fetch("http://localhost:5000/api/assignments/comments/counts")
          .then((res) => {
            if (!res.ok) throw new Error("Failed to fetch comment counts");
            return res.json();
          })
          .then((counts) => {
            setCommentCounts(counts);
          })
          .catch((err) => {
            console.error("Error fetching comment counts:", err);
          });
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

  const toggleComments = async (assignmentId) => {
    const isOpen = openComments[assignmentId];
    setOpenComments(prev => ({
      ...prev,
      [assignmentId]: !isOpen
    }));

    // Fetch comments if opening for the first time
    if (!isOpen && !comments[assignmentId]) {
      setLoadingComments(prev => ({ ...prev, [assignmentId]: true }));
      try {
        const response = await fetch(`${ASSIGNMENTS_API_URL}/${assignmentId}/comments`);
        if (!response.ok) throw new Error('Failed to fetch comments');
        const data = await response.json();
        setComments(prev => ({ ...prev, [assignmentId]: data }));
        // Update comment count if it's different
        setCommentCounts(prev => ({
          ...prev,
          [assignmentId]: data.length
        }));
        // Load comment notes and flags for all comments
        const notesPromises = data.map(comment =>
          fetch(`http://localhost:5000/api/comments/${comment.id}/note`)
            .then(res => res.ok ? res.json() : null)
            .then(noteData => noteData?.note ? { id: comment.id, note: noteData.note } : null)
            .catch(() => null)
        );
        const flagsPromises = data.map(comment =>
          fetch(`http://localhost:5000/api/comments/${comment.id}/flag`)
            .then(res => res.ok ? res.json() : null)
            .then(flagData => flagData?.isFlagged ? { id: comment.id, isFlagged: flagData.isFlagged } : null)
            .catch(() => null)
        );
        Promise.all([...notesPromises, ...flagsPromises]).then(results => {
          const notesState = {};
          const flagsState = {};
          results.forEach(result => {
            if (result) {
              if ('note' in result) {
                notesState[result.id] = result.note;
              } else if ('isFlagged' in result) {
                flagsState[result.id] = result.isFlagged;
              }
            }
          });
          setCommentNotes(prev => ({ ...prev, ...notesState }));
          setCommentFlags(prev => ({ ...prev, ...flagsState }));
        });
      } catch (err) {
        setError(err.toString());
      } finally {
        setLoadingComments(prev => ({ ...prev, [assignmentId]: false }));
      }
    }
  };

  const handleAddComment = async (assignmentId) => {
    const text = newCommentText[assignmentId]?.trim();
    if (!text) return;

    try {
      const response = await fetch(`${ASSIGNMENTS_API_URL}/${assignmentId}/comments`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ content: text }),
      });

      if (!response.ok) {
        throw new Error('Failed to add comment');
      }

      const newComment = await response.json();
      setComments(prev => ({
        ...prev,
        [assignmentId]: [...(prev[assignmentId] || []), newComment]
      }));
      setNewCommentText(prev => ({ ...prev, [assignmentId]: '' }));
      // Update comment count
      setCommentCounts(prev => ({
        ...prev,
        [assignmentId]: (prev[assignmentId] || 0) + 1
      }));
    } catch (err) {
      setError(err.toString());
    }
  };

  const toggleSubtasks = async (assignmentId) => {
    const isOpen = openSubtasks[assignmentId];
    setOpenSubtasks(prev => ({
      ...prev,
      [assignmentId]: !isOpen
    }));

    // Fetch subtasks if opening for the first time
    if (!isOpen && !subtasks[assignmentId]) {
      setLoadingSubtasks(prev => ({ ...prev, [assignmentId]: true }));
      try {
        const response = await fetch(`${ASSIGNMENTS_API_URL}/${assignmentId}/subtasks`);
        if (!response.ok) throw new Error('Failed to fetch subtasks');
        const data = await response.json();
        setSubtasks(prev => ({ ...prev, [assignmentId]: data }));
        // Initialize notes state from API response
        const notesState = {};
        data.forEach(subtask => {
          if (subtask.personalNote) {
            notesState[subtask.id] = subtask.personalNote;
          }
        });
        setSubtaskNotes(prev => {
          const updated = { ...prev };
          Object.keys(notesState).forEach(id => {
            updated[id] = notesState[id];
          });
          return updated;
        });
      } catch (err) {
        setError(err.toString());
      } finally {
        setLoadingSubtasks(prev => ({ ...prev, [assignmentId]: false }));
      }
    }
  };

  const handleToggleSubtask = async (subtaskId, isCompleted) => {
    // Find which assignment this subtask belongs to
    const assignmentId = Object.keys(subtasks).find(aId => 
      subtasks[aId]?.some(s => s.id === subtaskId)
    ) || null;

    // Optimistic update - update UI immediately
    setSubtasks(prev => {
      const updated = {};
      Object.keys(prev).forEach(aId => {
        updated[aId] = prev[aId].map(subtask =>
          subtask.id === subtaskId
            ? { ...subtask, isCompleted }
            : subtask
        );
      });
      return updated;
    });

    try {
      const response = await fetch(`http://localhost:5000/api/subtasks/${subtaskId}/toggle`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ isCompleted }),
      });

      if (!response.ok) {
        throw new Error('Failed to toggle subtask');
      }

      // Refetch subtasks to ensure we have the latest data from server
      if (assignmentId) {
        try {
          const fetchResponse = await fetch(`${ASSIGNMENTS_API_URL}/${assignmentId}/subtasks`);
          if (fetchResponse.ok) {
            const data = await fetchResponse.json();
            setSubtasks(prev => ({
              ...prev,
              [assignmentId]: data
            }));
            // Update notes state if needed
            const notesState = {};
            data.forEach(subtask => {
              if (subtask.personalNote) {
                notesState[subtask.id] = subtask.personalNote;
              }
            });
            setSubtaskNotes(prev => {
              const updated = { ...prev };
              Object.keys(notesState).forEach(id => {
                updated[id] = notesState[id];
              });
              return updated;
            });
          }
        } catch (fetchErr) {
          console.error('Error refetching subtasks:', fetchErr);
        }
      }
    } catch (err) {
      // Revert optimistic update on error
      setSubtasks(prev => {
        const updated = {};
        Object.keys(prev).forEach(aId => {
          updated[aId] = prev[aId].map(subtask =>
            subtask.id === subtaskId
              ? { ...subtask, isCompleted: !isCompleted }
              : subtask
          );
        });
        return updated;
      });
      setError(err.toString());
    }
  };

  const handleAddSubtask = async (assignmentId) => {
    const text = newSubtaskText[assignmentId]?.trim();
    if (!text) return;

    try {
      const response = await fetch(`${ASSIGNMENTS_API_URL}/${assignmentId}/subtasks`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ title: text }),
      });

      if (!response.ok) {
        throw new Error('Failed to add subtask');
      }

      const newSubtask = await response.json();
      setSubtasks(prev => ({
        ...prev,
        [assignmentId]: [...(prev[assignmentId] || []), newSubtask]
      }));
      setNewSubtaskText(prev => ({ ...prev, [assignmentId]: '' }));
    } catch (err) {
      setError(err.toString());
    }
  };

  const handleUpdateSubtaskNote = async (subtaskId, note) => {
    try {
      const response = await fetch(`http://localhost:5000/api/subtasks/${subtaskId}/note`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ note: note || null }),
      });

      if (!response.ok) {
        throw new Error('Failed to update note');
      }

      setSubtaskNotes(prev => ({
        ...prev,
        [subtaskId]: note || null
      }));
      setEditingNotes(prev => ({
        ...prev,
        [subtaskId]: false
      }));
    } catch (err) {
      setError(err.toString());
    }
  };

  const handleUpdateCommentNote = async (commentId, note) => {
    try {
      const response = await fetch(`http://localhost:5000/api/comments/${commentId}/note`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ note: note || null }),
      });

      if (!response.ok) {
        throw new Error('Failed to update comment note');
      }

      setCommentNotes(prev => ({
        ...prev,
        [commentId]: note || null
      }));
      setEditingCommentNotes(prev => ({
        ...prev,
        [commentId]: false
      }));
    } catch (err) {
      setError(err.toString());
    }
  };

  const handleToggleCommentFlag = async (commentId, isFlagged) => {
    try {
      const response = await fetch(`http://localhost:5000/api/comments/${commentId}/flag`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ isFlagged }),
      });

      if (!response.ok) {
        throw new Error('Failed to toggle comment flag');
      }

      setCommentFlags(prev => ({
        ...prev,
        [commentId]: isFlagged
      }));
    } catch (err) {
      setError(err.toString());
    }
  };

  const formatCommentDate = (dateString) => {
    if (!dateString) return '';
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return formatDate(dateString);
  };

  // Generate a deterministic color for a user based on their name
  const getUserColor = (userName) => {
    if (!userName) return '#6b7280';
    
    // Generate a hash from the username
    let hash = 0;
    for (let i = 0; i < userName.length; i++) {
      hash = userName.charCodeAt(i) + ((hash << 5) - hash);
    }
    
    // Use a palette of distinct colors
    const colors = [
      '#3b82f6', // blue
      '#10b981', // green
      '#f59e0b', // amber
      '#8b5cf6', // purple
      '#ec4899', // pink
      '#6366f1', // indigo
      '#14b8a6', // teal
      '#f97316', // orange
      '#06b6d4', // cyan
      '#a855f7', // violet
    ];
    
    return colors[Math.abs(hash) % colors.length];
  };

  // Convert hex color to rgba with opacity
  const hexToRgba = (hex, opacity) => {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    if (!result) return hex;
    const r = parseInt(result[1], 16);
    const g = parseInt(result[2], 16);
    const b = parseInt(result[3], 16);
    return `rgba(${r}, ${g}, ${b}, ${opacity})`;
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
                    {(() => {
                      const assignmentSubtasks = subtasks[assignment.id] || assignment.subtasks || [];
                      if (assignmentSubtasks.length > 0) {
                        const completedCount = assignmentSubtasks.filter(s => s.isCompleted).length;
                        return (
                          <span className="subtask-count">
                            {completedCount} / {assignmentSubtasks.length} subtasks
                          </span>
                        );
                      }
                      return null;
                    })()}
                    <button
                      className="comments-button"
                      onClick={() => toggleComments(assignment.id)}
                    >
                      💬 {commentCounts[assignment.id] || 0} {openComments[assignment.id] ? 'Hide' : 'Comments'}
                    </button>
                    <button
                      className="subtasks-button"
                      onClick={() => toggleSubtasks(assignment.id)}
                    >
                      ✓ {subtasks[assignment.id]?.length ?? assignment.subtasks?.length ?? 0} {openSubtasks[assignment.id] ? 'Hide' : 'Subtasks'}
                    </button>
                    {assignment.status !== 2 && (
                      <button
                        className="complete-button"
                        onClick={() => handleCompleteAssignment(assignment.id)}
                      >
                        Complete
                      </button>
                    )}
                  </div>

                  {openComments[assignment.id] && (
                    <div className="comments-section">
                      <div className="comments-filters">
                        <select
                          className="comment-filter-select"
                          value={commentFilters[assignment.id]?.user || ''}
                          onChange={(e) => setCommentFilters(prev => ({
                            ...prev,
                            [assignment.id]: { ...prev[assignment.id], user: e.target.value || null }
                          }))}
                        >
                          <option value="">All Users</option>
                          {comments[assignment.id] && Array.from(new Set(comments[assignment.id].map(c => c.authorName)))
                            .sort()
                            .map(userName => (
                              <option key={userName} value={userName}>{userName}</option>
                            ))}
                        </select>
                        <select
                          className="comment-filter-select"
                          value={commentFilters[assignment.id]?.date || ''}
                          onChange={(e) => setCommentFilters(prev => ({
                            ...prev,
                            [assignment.id]: { ...prev[assignment.id], date: e.target.value || null }
                          }))}
                        >
                          <option value="">All Dates</option>
                          <option value="today">Today</option>
                          <option value="week">This Week</option>
                          <option value="month">This Month</option>
                        </select>
                        <select
                          className="comment-filter-select"
                          value={commentFilters[assignment.id]?.flag || ''}
                          onChange={(e) => setCommentFilters(prev => ({
                            ...prev,
                            [assignment.id]: { ...prev[assignment.id], flag: e.target.value || null }
                          }))}
                        >
                          <option value="">All Comments</option>
                          <option value="flagged">Flagged</option>
                          <option value="not-flagged">Not Flagged</option>
                        </select>
                        <select
                          className="comment-filter-select"
                          value={commentFilters[assignment.id]?.note || ''}
                          onChange={(e) => setCommentFilters(prev => ({
                            ...prev,
                            [assignment.id]: { ...prev[assignment.id], note: e.target.value || null }
                          }))}
                        >
                          <option value="">All Comments</option>
                          <option value="has-note">Has Personal Note</option>
                          <option value="no-note">No Personal Note</option>
                        </select>
                        {(commentFilters[assignment.id]?.user || commentFilters[assignment.id]?.date || commentFilters[assignment.id]?.flag || commentFilters[assignment.id]?.note) && (
                          <button
                            className="comment-filter-clear"
                            onClick={() => setCommentFilters(prev => ({
                              ...prev,
                              [assignment.id]: { user: null, date: null, flag: null, note: null }
                            }))}
                          >
                            Clear Filters
                          </button>
                        )}
                      </div>
                      <div className="comments-list">
                        {loadingComments[assignment.id] ? (
                          <div className="comments-loading">Loading comments...</div>
                        ) : (() => {
                          const allComments = comments[assignment.id] || [];
                          const filter = commentFilters[assignment.id] || {};
                          
                          let filteredComments = allComments;
                          
                          // Filter by user
                          if (filter.user) {
                            filteredComments = filteredComments.filter(c => c.authorName === filter.user);
                          }
                          
                          // Filter by date
                          if (filter.date) {
                            const now = new Date();
                            const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
                            
                            filteredComments = filteredComments.filter(c => {
                              const commentDate = new Date(c.createdDate);
                              const commentDateOnly = new Date(commentDate.getFullYear(), commentDate.getMonth(), commentDate.getDate());
                              
                              switch (filter.date) {
                                case 'today': {
                                  return commentDateOnly.getTime() === today.getTime();
                                }
                                case 'week': {
                                  const weekAgo = new Date(today);
                                  weekAgo.setDate(weekAgo.getDate() - 7);
                                  return commentDateOnly >= weekAgo;
                                }
                                case 'month': {
                                  const monthAgo = new Date(today);
                                  monthAgo.setMonth(monthAgo.getMonth() - 1);
                                  return commentDateOnly >= monthAgo;
                                }
                                default:
                                  return true;
                              }
                            });
                          }
                          
                          // Filter by flag
                          if (filter.flag) {
                            if (filter.flag === 'flagged') {
                              filteredComments = filteredComments.filter(c => commentFlags[c.id] === true);
                            } else if (filter.flag === 'not-flagged') {
                              filteredComments = filteredComments.filter(c => !commentFlags[c.id]);
                            }
                          }
                          
                          // Filter by note
                          if (filter.note) {
                            if (filter.note === 'has-note') {
                              filteredComments = filteredComments.filter(c => commentNotes[c.id]);
                            } else if (filter.note === 'no-note') {
                              filteredComments = filteredComments.filter(c => !commentNotes[c.id]);
                            }
                          }
                          
                          return filteredComments.length > 0 ? (
                            filteredComments.map((comment) => {
                            const isCurrentUser = currentUser && comment.authorName === currentUser;
                            const userColor = getUserColor(comment.authorName);
                            return (
                              <div 
                                key={comment.id} 
                                className="comment-bubble"
                                style={{ 
                                  '--user-color': userColor,
                                  backgroundColor: isCurrentUser ? hexToRgba(userColor, 0.2) : 'rgba(255, 255, 255, 0.08)',
                                  borderColor: isCurrentUser ? hexToRgba(userColor, 0.4) : 'rgba(255, 255, 255, 0.1)'
                                }}
                              >
                                <div className="comment-header">
                                  <span className="comment-author" style={{ color: userColor }}>
                                    {comment.authorName}
                                  </span>
                                  <span className="comment-date">{formatCommentDate(comment.createdDate)}</span>
                                  <button
                                    className={`comment-flag-button ${commentFlags[comment.id] ? 'flagged' : ''}`}
                                    onClick={() => handleToggleCommentFlag(comment.id, !commentFlags[comment.id])}
                                    title={commentFlags[comment.id] ? 'Unflag comment' : 'Flag comment'}
                                  >
                                    {commentFlags[comment.id] ? '🚩' : '🏳️'}
                                  </button>
                                  <button
                                    className="comment-note-button"
                                    onClick={() => {
                                      setEditingCommentNotes(prev => ({
                                        ...prev,
                                        [comment.id]: !prev[comment.id]
                                      }));
                                      // Load note if not already loaded
                                      if (!commentNotes[comment.id]) {
                                        fetch(`http://localhost:5000/api/comments/${comment.id}/note`)
                                          .then(res => res.ok ? res.json() : null)
                                          .then(data => {
                                            if (data?.note) {
                                              setCommentNotes(prev => ({
                                                ...prev,
                                                [comment.id]: data.note
                                              }));
                                            }
                                          })
                                          .catch(err => console.error('Error loading comment note:', err));
                                      }
                                    }}
                                    title={commentNotes[comment.id] ? 'Edit personal note' : 'Add personal note'}
                                  >
                                    {commentNotes[comment.id] ? '📝' : '📄'}
                                  </button>
                                </div>
                                <div className="comment-content">{comment.content}</div>
                                {editingCommentNotes[comment.id] && (
                                  <div className="comment-note-editor">
                                    <textarea
                                      className="comment-note-input"
                                      placeholder="Add a personal note about this comment..."
                                      value={commentNotes[comment.id] || ''}
                                      onChange={(e) => setCommentNotes(prev => ({
                                        ...prev,
                                        [comment.id]: e.target.value
                                      }))}
                                      rows={3}
                                    />
                                    <div className="comment-note-actions">
                                      <button
                                        className="comment-note-save"
                                        onClick={() => handleUpdateCommentNote(comment.id, commentNotes[comment.id])}
                                      >
                                        Save
                                      </button>
                                      <button
                                        className="comment-note-cancel"
                                        onClick={() => {
                                          setEditingCommentNotes(prev => ({
                                            ...prev,
                                            [comment.id]: false
                                          }));
                                        }}
                                      >
                                        Cancel
                                      </button>
                                    </div>
                                  </div>
                                )}
                                {!editingCommentNotes[comment.id] && commentNotes[comment.id] && (
                                  <div className="comment-note-display">
                                    <span className="comment-note-label">Personal Note:</span>
                                    <span className="comment-note-text">{commentNotes[comment.id]}</span>
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
                          );
                        })()}
                      </div>
                      <div className="comment-input-container">
                        <textarea
                          className="comment-input"
                          placeholder="Add a comment..."
                          value={newCommentText[assignment.id] || ''}
                          onChange={(e) => setNewCommentText(prev => ({ ...prev, [assignment.id]: e.target.value }))}
                          onKeyDown={(e) => {
                            if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
                              e.preventDefault();
                              handleAddComment(assignment.id);
                            }
                          }}
                          rows={3}
                        />
                        <button
                          className="comment-submit-button"
                          onClick={() => handleAddComment(assignment.id)}
                          disabled={!newCommentText[assignment.id]?.trim()}
                        >
                          Post
                        </button>
                      </div>
                    </div>
                  )}

                  {openSubtasks[assignment.id] && (
                    <div className="subtasks-section">
                      <div className="subtasks-list">
                        {loadingSubtasks[assignment.id] ? (
                          <div className="subtasks-loading">Loading subtasks...</div>
                        ) : (() => {
                          const assignmentSubtasks = subtasks[assignment.id] || [];
                          
                          return assignmentSubtasks.length > 0 ? (
                            assignmentSubtasks.map((subtask) => (
                              <div key={subtask.id} className="subtask-item">
                                <div className="subtask-main">
                                  <label className="subtask-checkbox-label">
                                    <input
                                      type="checkbox"
                                      className="subtask-checkbox"
                                      checked={subtask.isCompleted}
                                      onChange={(e) => handleToggleSubtask(subtask.id, e.target.checked)}
                                    />
                                    <span className={`subtask-title ${subtask.isCompleted ? 'completed' : ''}`}>
                                      {subtask.title}
                                    </span>
                                  </label>
                                  <button
                                    className="subtask-note-button"
                                    onClick={() => {
                                      setEditingNotes(prev => ({
                                        ...prev,
                                        [subtask.id]: !prev[subtask.id]
                                      }));
                                      // Initialize note in state if not already set
                                      if (!subtaskNotes[subtask.id] && subtask.personalNote) {
                                        setSubtaskNotes(prev => ({
                                          ...prev,
                                          [subtask.id]: subtask.personalNote
                                        }));
                                      }
                                    }}
                                    title={(subtaskNotes[subtask.id] || subtask.personalNote) ? 'Edit note' : 'Add note'}
                                  >
                                    {(subtaskNotes[subtask.id] || subtask.personalNote) ? '📝' : '📄'}
                                  </button>
                                </div>
                                {editingNotes[subtask.id] && (
                                  <div className="subtask-note-editor">
                                    <textarea
                                      className="subtask-note-input"
                                      placeholder="Add a personal note..."
                                      value={subtaskNotes[subtask.id] ?? subtask.personalNote ?? ''}
                                      onChange={(e) => setSubtaskNotes(prev => ({
                                        ...prev,
                                        [subtask.id]: e.target.value
                                      }))}
                                      rows={3}
                                    />
                                    <div className="subtask-note-actions">
                                      <button
                                        className="subtask-note-save"
                                        onClick={() => handleUpdateSubtaskNote(subtask.id, subtaskNotes[subtask.id])}
                                      >
                                        Save
                                      </button>
                                      <button
                                        className="subtask-note-cancel"
                                        onClick={() => {
                                          setEditingNotes(prev => ({
                                            ...prev,
                                            [subtask.id]: false
                                          }));
                                          // Restore original note
                                          setSubtaskNotes(prev => {
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
                                {!editingNotes[subtask.id] && (subtaskNotes[subtask.id] || subtask.personalNote) && (
                                  <div className="subtask-note-display">
                                    <span className="subtask-note-label">Note:</span>
                                    <span className="subtask-note-text">{subtaskNotes[subtask.id] || subtask.personalNote}</span>
                                  </div>
                                )}
                              </div>
                            ))
                          ) : (
                            <div className="subtasks-empty">
                              No subtasks yet. Add one below!
                            </div>
                          );
                        })()}
                      </div>
                      <div className="subtask-input-container">
                        <input
                          type="text"
                          className="subtask-input"
                          placeholder="Add a subtask..."
                          value={newSubtaskText[assignment.id] || ''}
                          onChange={(e) => setNewSubtaskText(prev => ({ ...prev, [assignment.id]: e.target.value }))}
                          onKeyPress={(e) => {
                            if (e.key === 'Enter' && !e.shiftKey) {
                              e.preventDefault();
                              handleAddSubtask(assignment.id);
                            }
                          }}
                        />
                        <button
                          className="subtask-submit-button"
                          onClick={() => handleAddSubtask(assignment.id)}
                          disabled={!newSubtaskText[assignment.id]?.trim()}
                        >
                          Add
                        </button>
                      </div>
                    </div>
                  )}
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