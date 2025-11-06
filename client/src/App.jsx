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
                    {assignment.subtasks && assignment.subtasks.length > 0 && (
                      <span className="subtask-count">
                        {assignment.subtasks.filter(s => s.isCompleted).length} / {assignment.subtasks.length} subtasks
                      </span>
                    )}
                    <button
                      className="comments-button"
                      onClick={() => toggleComments(assignment.id)}
                    >
                      💬 {commentCounts[assignment.id] || 0} {openComments[assignment.id] ? 'Hide' : 'Comments'}
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
                        {(commentFilters[assignment.id]?.user || commentFilters[assignment.id]?.date) && (
                          <button
                            className="comment-filter-clear"
                            onClick={() => setCommentFilters(prev => ({
                              ...prev,
                              [assignment.id]: { user: null, date: null }
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
                                </div>
                                <div className="comment-content">{comment.content}</div>
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
                        <input
                          type="text"
                          className="comment-input"
                          placeholder="Add a comment..."
                          value={newCommentText[assignment.id] || ''}
                          onChange={(e) => setNewCommentText(prev => ({ ...prev, [assignment.id]: e.target.value }))}
                          onKeyPress={(e) => {
                            if (e.key === 'Enter' && !e.shiftKey) {
                              e.preventDefault();
                              handleAddComment(assignment.id);
                            }
                          }}
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