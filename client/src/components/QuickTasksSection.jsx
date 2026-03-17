import React, { useEffect, useMemo, useState } from "react";

const QUICK_TASKS_API_URL = "http://localhost:5000/api/quick-tasks";

function QuickTasksSection() {
  const [tasks, setTasks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [newTaskTitle, setNewTaskTitle] = useState("");
  const [openTaskDetails, setOpenTaskDetails] = useState({});
  const [newComments, setNewComments] = useState({});
  const [newChecklistItems, setNewChecklistItems] = useState({});
  const [editingChecklistTitles, setEditingChecklistTitles] = useState({});
  const [checklistDrafts, setChecklistDrafts] = useState({});
  const [draggedChecklistItem, setDraggedChecklistItem] = useState(null);

  const sortedTasks = useMemo(
    () =>
      [...tasks].sort((a, b) => {
        if (a.isCompleted !== b.isCompleted) {
          return Number(a.isCompleted) - Number(b.isCompleted);
        }
        return new Date(b.createdDate) - new Date(a.createdDate);
      }),
    [tasks]
  );

  useEffect(() => {
    refreshTasks();
  }, []);

  const refreshTasks = async () => {
    try {
      const response = await fetch(QUICK_TASKS_API_URL);
      if (!response.ok) throw new Error("Failed to fetch quick tasks");
      const data = await response.json();
      setTasks(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleAddTask = async () => {
    const title = newTaskTitle.trim();
    if (!title) return;

    try {
      const response = await fetch(QUICK_TASKS_API_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ title })
      });
      if (!response.ok) throw new Error("Failed to add quick task");
      const created = await response.json();
      setTasks((prev) => [created, ...prev]);
      setNewTaskTitle("");
    } catch (err) {
      console.error(err);
    }
  };

  const handleToggleTask = async (task, isCompleted) => {
    try {
      const response = await fetch(
        `${QUICK_TASKS_API_URL}/${task.id}/toggle`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ isCompleted })
        }
      );
      if (!response.ok) throw new Error("Failed to toggle quick task");

      setTasks((prev) =>
        prev.map((t) => (t.id === task.id ? { ...t, isCompleted } : t))
      );
    } catch (err) {
      console.error(err);
    }
  };

  const handleDeleteTask = async (taskId) => {
    try {
      const response = await fetch(`${QUICK_TASKS_API_URL}/${taskId}`, {
        method: "DELETE"
      });
      if (!response.ok) throw new Error("Failed to delete quick task");
      setTasks((prev) => prev.filter((t) => t.id !== taskId));
    } catch (err) {
      console.error(err);
    }
  };

  const handleAddComment = async (taskId) => {
    const content = (newComments[taskId] || "").trim();
    if (!content) return;

    try {
      const response = await fetch(`${QUICK_TASKS_API_URL}/${taskId}/comments`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ content })
      });
      if (!response.ok) throw new Error("Failed to add quick task comment");
      const created = await response.json();

      setTasks((prev) =>
        prev.map((task) =>
          task.id === taskId
            ? {
                ...task,
                comments: [...(task.comments || []), created]
              }
            : task
        )
      );
      setNewComments((prev) => ({ ...prev, [taskId]: "" }));
    } catch (err) {
      console.error(err);
    }
  };

  const handleAddChecklistItem = async (taskId) => {
    const title = (newChecklistItems[taskId] || "").trim();
    if (!title) return;

    try {
      const response = await fetch(`${QUICK_TASKS_API_URL}/${taskId}/checklist`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ title })
      });
      if (!response.ok) throw new Error("Failed to add checklist item");
      const created = await response.json();

      setTasks((prev) =>
        prev.map((task) =>
          task.id === taskId
            ? {
                ...task,
                checklist: [...(task.checklist || []), created].sort(
                  (a, b) => a.order - b.order
                )
              }
            : task
        )
      );
      setNewChecklistItems((prev) => ({ ...prev, [taskId]: "" }));
    } catch (err) {
      console.error(err);
    }
  };

  const handleToggleChecklistItem = async (taskId, itemId, isCompleted) => {
    try {
      const response = await fetch(
        `http://localhost:5000/api/quick-task-checklist/${itemId}/toggle`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ isCompleted })
        }
      );
      if (!response.ok) throw new Error("Failed to toggle checklist item");

      setTasks((prev) =>
        prev.map((task) =>
          task.id === taskId
            ? {
                ...task,
                checklist: (task.checklist || []).map((item) =>
                  item.id === itemId ? { ...item, isCompleted } : item
                )
              }
            : task
        )
      );
    } catch (err) {
      console.error(err);
    }
  };

  const handleDeleteChecklistItem = async (taskId, itemId) => {
    try {
      const response = await fetch(
        `http://localhost:5000/api/quick-task-checklist/${itemId}`,
        { method: "DELETE" }
      );
      if (!response.ok) throw new Error("Failed to delete checklist item");

      setTasks((prev) =>
        prev.map((task) =>
          task.id === taskId
            ? {
                ...task,
                checklist: (task.checklist || []).filter((item) => item.id !== itemId)
              }
            : task
        )
      );
    } catch (err) {
      console.error(err);
    }
  };

  const beginChecklistEdit = (itemId, currentTitle) => {
    setEditingChecklistTitles((prev) => ({ ...prev, [itemId]: true }));
    setChecklistDrafts((prev) => ({ ...prev, [itemId]: currentTitle || "" }));
  };

  const cancelChecklistEdit = (itemId) => {
    setEditingChecklistTitles((prev) => ({ ...prev, [itemId]: false }));
    setChecklistDrafts((prev) => {
      const next = { ...prev };
      delete next[itemId];
      return next;
    });
  };

  const saveChecklistEdit = async (taskId, itemId) => {
    const title = (checklistDrafts[itemId] || "").trim();
    if (!title) return;

    try {
      const response = await fetch(
        `http://localhost:5000/api/quick-task-checklist/${itemId}/title`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ title })
        }
      );
      if (!response.ok) throw new Error("Failed to update checklist title");

      setTasks((prev) =>
        prev.map((task) =>
          task.id === taskId
            ? {
                ...task,
                checklist: (task.checklist || []).map((item) =>
                  item.id === itemId ? { ...item, title } : item
                )
              }
            : task
        )
      );
      cancelChecklistEdit(itemId);
    } catch (err) {
      console.error(err);
    }
  };

  const handleChecklistDrop = async (taskId, targetItemId) => {
    if (!draggedChecklistItem) return;
    if (draggedChecklistItem.taskId !== taskId) return;
    if (draggedChecklistItem.itemId === targetItemId) return;

    const task = tasks.find((t) => t.id === taskId);
    if (!task) return;

    const items = [...(task.checklist || [])].sort((a, b) => a.order - b.order);
    const sourceIndex = items.findIndex((i) => i.id === draggedChecklistItem.itemId);
    const targetIndex = items.findIndex((i) => i.id === targetItemId);
    if (sourceIndex < 0 || targetIndex < 0) return;

    const reordered = [...items];
    const [moved] = reordered.splice(sourceIndex, 1);
    reordered.splice(targetIndex, 0, moved);
    const normalized = reordered.map((item, idx) => ({ ...item, order: idx }));

    setTasks((prev) =>
      prev.map((t) => (t.id === taskId ? { ...t, checklist: normalized } : t))
    );

    const checklistOrders = {};
    normalized.forEach((item, idx) => {
      checklistOrders[item.id] = idx;
    });

    try {
      await fetch(`${QUICK_TASKS_API_URL}/${taskId}/checklist/reorder`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ checklistOrders })
      });
    } catch (err) {
      console.error("Failed to reorder checklist", err);
    } finally {
      setDraggedChecklistItem(null);
    }
  };

  if (loading) {
    return (
      <section className="quick-tasks-panel">
        <h2>Quick Tasks</h2>
        <p className="quick-tasks-loading">Loading quick tasks...</p>
      </section>
    );
  }

  return (
    <section className="quick-tasks-panel">
      <div className="quick-tasks-header">
        <h2>Quick Tasks</h2>
        <span className="quick-tasks-count">{sortedTasks.length}</span>
      </div>

      <div className="quick-task-add-row">
        <input
          type="text"
          className="quick-task-input"
          placeholder="Add a quick task..."
          value={newTaskTitle}
          onChange={(e) => setNewTaskTitle(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault();
              handleAddTask();
            }
          }}
        />
        <button className="quick-task-add-btn" onClick={handleAddTask}>
          Add
        </button>
      </div>

      <div className="quick-task-list">
        {sortedTasks.length === 0 && (
          <p className="quick-tasks-empty">No quick tasks yet.</p>
        )}

        {sortedTasks.map((task) => {
          const comments = task.comments || [];
          const checklist = [...(task.checklist || [])].sort(
            (a, b) => a.order - b.order
          );
          const completedChecklist = checklist.filter((i) => i.isCompleted).length;

          return (
            <article key={task.id} className="quick-task-card">
              <div className="quick-task-row">
                <label className="quick-task-title-row">
                  <input
                    type="checkbox"
                    checked={task.isCompleted}
                    onChange={(e) => handleToggleTask(task, e.target.checked)}
                  />
                  <span className={task.isCompleted ? "quick-task-title done" : "quick-task-title"}>
                    {task.title}
                  </span>
                </label>

                <div className="quick-task-meta">
                  <button
                    className="quick-task-expand-btn"
                    onClick={() =>
                      setOpenTaskDetails((prev) => ({
                        ...prev,
                        [task.id]: !prev[task.id]
                      }))
                    }
                  >
                    {openTaskDetails[task.id] ? "Hide" : "Details"}
                  </button>
                  <button
                    className="quick-task-delete-btn"
                    onClick={() => handleDeleteTask(task.id)}
                  >
                    Delete
                  </button>
                </div>
              </div>

              <div className="quick-task-summary">
                💬 {comments.length} comments · ☑ {completedChecklist}/{checklist.length}
              </div>

              {openTaskDetails[task.id] && (
                <div className="quick-task-details">
                  <div className="quick-task-comments">
                    <h4>Comments</h4>
                    <div className="quick-task-items-list">
                      {comments.map((comment) => (
                        <div key={comment.id} className="quick-task-comment-item">
                          {comment.content}
                        </div>
                      ))}
                    </div>
                    <div className="quick-task-add-inline">
                      <input
                        type="text"
                        placeholder="Add comment..."
                        value={newComments[task.id] || ""}
                        onChange={(e) =>
                          setNewComments((prev) => ({
                            ...prev,
                            [task.id]: e.target.value
                          }))
                        }
                        onKeyDown={(e) => {
                          if (e.key === "Enter") {
                            e.preventDefault();
                            handleAddComment(task.id);
                          }
                        }}
                      />
                      <button onClick={() => handleAddComment(task.id)}>Add</button>
                    </div>
                  </div>

                  <div className="quick-task-checklist">
                    <h4>Checklist</h4>
                    <div className="quick-task-items-list">
                      {checklist.map((item) => (
                        <div
                          key={item.id}
                          className="quick-task-checklist-item"
                          draggable
                          onDragStart={() =>
                            setDraggedChecklistItem({ taskId: task.id, itemId: item.id })
                          }
                          onDragOver={(e) => e.preventDefault()}
                          onDrop={(e) => {
                            e.preventDefault();
                            handleChecklistDrop(task.id, item.id);
                          }}
                        >
                          <input
                            type="checkbox"
                            checked={item.isCompleted}
                            onChange={(e) =>
                              handleToggleChecklistItem(task.id, item.id, e.target.checked)
                            }
                          />
                          {editingChecklistTitles[item.id] ? (
                            <input
                              className="quick-task-checklist-edit-input"
                              value={checklistDrafts[item.id] || ""}
                              onChange={(e) =>
                                setChecklistDrafts((prev) => ({
                                  ...prev,
                                  [item.id]: e.target.value
                                }))
                              }
                              onKeyDown={(e) => {
                                if (e.key === "Enter") {
                                  e.preventDefault();
                                  saveChecklistEdit(task.id, item.id);
                                } else if (e.key === "Escape") {
                                  e.preventDefault();
                                  cancelChecklistEdit(item.id);
                                }
                              }}
                              autoFocus
                            />
                          ) : (
                            <span className={item.isCompleted ? "done" : ""}>{item.title}</span>
                          )}
                          {editingChecklistTitles[item.id] ? (
                            <>
                              <button
                                onClick={() => saveChecklistEdit(task.id, item.id)}
                                title="Save"
                              >
                                ✓
                              </button>
                              <button onClick={() => cancelChecklistEdit(item.id)} title="Cancel">
                                ↺
                              </button>
                            </>
                          ) : (
                            <button
                              onClick={() => beginChecklistEdit(item.id, item.title)}
                              title="Edit"
                            >
                              ✎
                            </button>
                          )}
                          <button
                            onClick={() => handleDeleteChecklistItem(task.id, item.id)}
                            title="Delete"
                          >
                            ✕
                          </button>
                        </div>
                      ))}
                    </div>
                    <div className="quick-task-add-inline">
                      <input
                        type="text"
                        placeholder="Add checklist item..."
                        value={newChecklistItems[task.id] || ""}
                        onChange={(e) =>
                          setNewChecklistItems((prev) => ({
                            ...prev,
                            [task.id]: e.target.value
                          }))
                        }
                        onKeyDown={(e) => {
                          if (e.key === "Enter") {
                            e.preventDefault();
                            handleAddChecklistItem(task.id);
                          }
                        }}
                      />
                      <button onClick={() => handleAddChecklistItem(task.id)}>Add</button>
                    </div>
                  </div>
                </div>
              )}
            </article>
          );
        })}
      </div>
    </section>
  );
}

export default QuickTasksSection;
