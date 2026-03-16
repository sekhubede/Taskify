import React, { useCallback, useEffect, useState } from "react";
import "./App.css";

const ASSIGNMENTS_API_URL = "http://localhost:5000/api/assignments";
const LAST_SEEN_COMMENT_COUNTS_KEY = "lastSeenCommentCounts";
const SHOW_ONLY_UNREAD_ASSIGNMENTS_KEY = "showOnlyUnreadAssignments";
const SORT_UNREAD_TO_TOP_KEY = "sortUnreadToTop";
const COMMENT_SORT_NEWEST_FIRST_KEY = "commentSortNewestFirst";
const AUTO_REFRESH_INTERVAL_SECONDS_KEY = "assignmentAutoRefreshSeconds";

function App() {
  const [assignments, setAssignments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [openComments, setOpenComments] = useState({});
  const [comments, setComments] = useState({});
  const [loadingComments, setLoadingComments] = useState({});
  const [openAttachments, setOpenAttachments] = useState({});
  const [attachments, setAttachments] = useState({});
  const [attachmentCounts, setAttachmentCounts] = useState({});
  const [loadingAttachments, setLoadingAttachments] = useState({});
  const [uploadingAttachment, setUploadingAttachment] = useState({});
  const [previewingAttachment, setPreviewingAttachment] = useState(false);
  const [attachmentPreview, setAttachmentPreview] = useState(null);
  const [newCommentText, setNewCommentText] = useState({});
  const [currentUser, setCurrentUser] = useState(null);
  const [commentCounts, setCommentCounts] = useState({});
  const [commentFilters, setCommentFilters] = useState({});
  const [lastSeenCommentCounts, setLastSeenCommentCounts] = useState(() => {
    try {
      const raw = localStorage.getItem(LAST_SEEN_COMMENT_COUNTS_KEY);
      return raw ? JSON.parse(raw) : {};
    } catch {
      return {};
    }
  });
  const [openSubtasks, setOpenSubtasks] = useState({});
  const [subtasks, setSubtasks] = useState({});
  const [loadingSubtasks, setLoadingSubtasks] = useState({});
  const [newSubtaskText, setNewSubtaskText] = useState({});
  const [editingNotes, setEditingNotes] = useState({});
  const [subtaskNotes, setSubtaskNotes] = useState({});
  const [editingSubtasks, setEditingSubtasks] = useState({});
  const [subtaskTitleDrafts, setSubtaskTitleDrafts] = useState({});
  const [editingCommentNotes, setEditingCommentNotes] = useState({});
  const [commentNotes, setCommentNotes] = useState({});
  const [commentFlags, setCommentFlags] = useState({});
  const [draggedSubtaskId, setDraggedSubtaskId] = useState(null);
  const [dragOverSubtaskId, setDragOverSubtaskId] = useState(null);
  const [workingOn, setWorkingOn] = useState(new Set()); // assignmentIds that are marked as "working on"
  const [allAssignmentsCollapsed, setAllAssignmentsCollapsed] = useState(false);
  const [assignmentSearch, setAssignmentSearch] = useState("");
  const [showOnlyUnreadAssignments, setShowOnlyUnreadAssignments] = useState(
    () => {
      try {
        return (
          localStorage.getItem(SHOW_ONLY_UNREAD_ASSIGNMENTS_KEY) === "true"
        );
      } catch {
        return false;
      }
    }
  );
  const [sortUnreadToTop, setSortUnreadToTop] = useState(() => {
    try {
      return localStorage.getItem(SORT_UNREAD_TO_TOP_KEY) === "true";
    } catch {
      return false;
    }
  });
  const [commentSortNewestFirst, setCommentSortNewestFirst] = useState(() => {
    try {
      const raw = localStorage.getItem(COMMENT_SORT_NEWEST_FIRST_KEY);
      return raw === null ? true : raw === "true";
    } catch {
      return true;
    }
  });
  const [autoRefreshIntervalSeconds, setAutoRefreshIntervalSeconds] = useState(
    () => {
      try {
        const raw = localStorage.getItem(AUTO_REFRESH_INTERVAL_SECONDS_KEY);
        const parsed = Number(raw);
        return Number.isFinite(parsed) && parsed >= 0 ? parsed : 0;
      } catch {
        return 0;
      }
    }
  );
  const [lastRefreshedAt, setLastRefreshedAt] = useState(null);

  const refreshData = useCallback(async (isInitialLoad = false) => {
    try {
      const [userRes, assignmentsRes, countsRes, workingOnRes] =
        await Promise.all([
          fetch("http://localhost:5000/api/user/current"),
          fetch(ASSIGNMENTS_API_URL),
          fetch("http://localhost:5000/api/assignments/comments/counts"),
          fetch("http://localhost:5000/api/assignments/working-on")
        ]);

      if (userRes.ok) {
        const userData = await userRes.json();
        setCurrentUser(userData.userName);
      }

      if (!assignmentsRes.ok) throw new Error("Failed to fetch assignments");
      const assignmentsData = await assignmentsRes.json();
      setAssignments(assignmentsData);

      // Preload attachment counts so badges are correct without opening panels.
      const attachmentSettled = await Promise.allSettled(
        assignmentsData.map(async (assignment) => {
          const response = await fetch(
            `${ASSIGNMENTS_API_URL}/${assignment.id}/attachments`
          );
          if (!response.ok) {
            throw new Error(`Failed to fetch attachments for ${assignment.id}`);
          }
          const list = await response.json();
          return {
            assignmentId: assignment.id,
            list
          };
        })
      );

      const nextAttachmentCounts = {};
      const nextAttachmentLists = {};
      attachmentSettled.forEach((result) => {
        if (result.status === "fulfilled") {
          const { assignmentId, list } = result.value;
          nextAttachmentCounts[assignmentId] = list.length;
          nextAttachmentLists[assignmentId] = list;
        }
      });

      setAttachmentCounts(nextAttachmentCounts);
      setAttachments((prev) => ({ ...prev, ...nextAttachmentLists }));

      if (countsRes.ok) {
        const counts = await countsRes.json();
        setCommentCounts(counts);
      }

      if (workingOnRes.ok) {
        const workingOnData = await workingOnRes.json();
        setWorkingOn(new Set(workingOnData));
      }

      setLastRefreshedAt(new Date().toISOString());
      setError(null);
    } catch (err) {
      if (isInitialLoad) {
        setError(err.toString());
      } else {
        console.error("Error refreshing assignment data:", err);
      }
    } finally {
      if (isInitialLoad) {
        setLoading(false);
      }
    }
  }, []);

  useEffect(() => {
    refreshData(true);
  }, [refreshData]);

  useEffect(() => {
    try {
      localStorage.setItem(
        LAST_SEEN_COMMENT_COUNTS_KEY,
        JSON.stringify(lastSeenCommentCounts)
      );
    } catch {
      // ignore storage write failures
    }
  }, [lastSeenCommentCounts]);

  useEffect(() => {
    try {
      localStorage.setItem(
        SHOW_ONLY_UNREAD_ASSIGNMENTS_KEY,
        String(showOnlyUnreadAssignments)
      );
    } catch {
      // ignore storage write failures
    }
  }, [showOnlyUnreadAssignments]);

  useEffect(() => {
    try {
      localStorage.setItem(SORT_UNREAD_TO_TOP_KEY, String(sortUnreadToTop));
    } catch {
      // ignore storage write failures
    }
  }, [sortUnreadToTop]);

  useEffect(() => {
    try {
      localStorage.setItem(
        COMMENT_SORT_NEWEST_FIRST_KEY,
        String(commentSortNewestFirst)
      );
    } catch {
      // ignore storage write failures
    }
  }, [commentSortNewestFirst]);

  useEffect(() => {
    try {
      localStorage.setItem(
        AUTO_REFRESH_INTERVAL_SECONDS_KEY,
        String(autoRefreshIntervalSeconds)
      );
    } catch {
      // ignore storage write failures
    }
  }, [autoRefreshIntervalSeconds]);

  useEffect(() => {
    if (!autoRefreshIntervalSeconds) return undefined;

    const intervalId = setInterval(() => {
      refreshData(false);
    }, autoRefreshIntervalSeconds * 1000);

    return () => clearInterval(intervalId);
  }, [autoRefreshIntervalSeconds, refreshData]);

  useEffect(() => {
    return () => {
      if (attachmentPreview?.objectUrl) {
        window.URL.revokeObjectURL(attachmentPreview.objectUrl);
      }
    };
  }, [attachmentPreview]);

  useEffect(() => {
    // Initialize unseen assignments so badges only represent comments
    // that arrived after the user has a baseline.
    setLastSeenCommentCounts((prev) => {
      const updated = { ...prev };
      let changed = false;

      Object.entries(commentCounts).forEach(([assignmentId, count]) => {
        if (updated[assignmentId] === undefined) {
          updated[assignmentId] = count;
          changed = true;
        }
      });

      return changed ? updated : prev;
    });
  }, [commentCounts]);

  const formatDate = (dateString) => {
    if (!dateString) return null;
    const date = new Date(dateString);
    return date.toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      year:
        date.getFullYear() !== new Date().getFullYear() ? "numeric" : undefined
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
      6: "#6366f1" // WaitingForReview - indigo
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
      6: "Waiting for Review"
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
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        }
      });

      if (!response.ok) {
        throw new Error("Failed to mark assignment as complete");
      }

      // Update the assignment in local state
      setAssignments((prevAssignments) =>
        prevAssignments.map((assignment) =>
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

    if (isOpen) {
      // Closing the thread marks current comments as seen.
      // This avoids jarring resort/filter jumps immediately after opening.
      setLastSeenCommentCounts((prev) => ({
        ...prev,
        [assignmentId]:
          commentCounts[assignmentId] ??
          comments[assignmentId]?.length ??
          0
      }));
    }

    setOpenComments((prev) => ({
      ...prev,
      [assignmentId]: !isOpen
    }));

    // Fetch comments if opening for the first time
    if (!isOpen && !comments[assignmentId]) {
      setLoadingComments((prev) => ({ ...prev, [assignmentId]: true }));
      try {
        const response = await fetch(
          `${ASSIGNMENTS_API_URL}/${assignmentId}/comments`
        );
        if (!response.ok) throw new Error("Failed to fetch comments");
        const data = await response.json();
        setComments((prev) => ({ ...prev, [assignmentId]: data }));
        // Update comment count if it's different
        setCommentCounts((prev) => ({
          ...prev,
          [assignmentId]: data.length
        }));
        // Load comment notes and flags for all comments
        const notesPromises = data.map((comment) =>
          fetch(`http://localhost:5000/api/comments/${comment.id}/note`)
            .then((res) => (res.ok ? res.json() : null))
            .then((noteData) =>
              noteData?.note ? { id: comment.id, note: noteData.note } : null
            )
            .catch(() => null)
        );
        const flagsPromises = data.map((comment) =>
          fetch(`http://localhost:5000/api/comments/${comment.id}/flag`)
            .then((res) => (res.ok ? res.json() : null))
            .then((flagData) =>
              flagData?.isFlagged
                ? { id: comment.id, isFlagged: flagData.isFlagged }
                : null
            )
            .catch(() => null)
        );
        Promise.all([...notesPromises, ...flagsPromises]).then((results) => {
          const notesState = {};
          const flagsState = {};
          results.forEach((result) => {
            if (result) {
              if ("note" in result) {
                notesState[result.id] = result.note;
              } else if ("isFlagged" in result) {
                flagsState[result.id] = result.isFlagged;
              }
            }
          });
          setCommentNotes((prev) => ({ ...prev, ...notesState }));
          setCommentFlags((prev) => ({ ...prev, ...flagsState }));
        });
      } catch (err) {
        setError(err.toString());
      } finally {
        setLoadingComments((prev) => ({ ...prev, [assignmentId]: false }));
      }
    }
  };

  const handleAddComment = async (assignmentId) => {
    const text = newCommentText[assignmentId]?.trim();
    if (!text) return;

    try {
      const response = await fetch(
        `${ASSIGNMENTS_API_URL}/${assignmentId}/comments`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({ content: text })
        }
      );

      if (!response.ok) {
        throw new Error("Failed to add comment");
      }

      const newComment = await response.json();
      const baseCount =
        commentCounts[assignmentId] ?? comments[assignmentId]?.length ?? 0;
      const nextCount = baseCount + 1;

      setComments((prev) => ({
        ...prev,
        [assignmentId]: [...(prev[assignmentId] || []), newComment]
      }));
      setNewCommentText((prev) => ({ ...prev, [assignmentId]: "" }));
      // Update comment count
      setCommentCounts((prev) => ({
        ...prev,
        [assignmentId]: nextCount
      }));
      // Comments authored in-app are considered seen immediately.
      setLastSeenCommentCounts((prev) => ({
        ...prev,
        [assignmentId]: nextCount
      }));
    } catch (err) {
      setError(err.toString());
    }
  };

  const toggleAttachments = async (assignmentId) => {
    const isOpen = openAttachments[assignmentId];
    setOpenAttachments((prev) => ({
      ...prev,
      [assignmentId]: !isOpen
    }));

    if (!isOpen && !attachments[assignmentId]) {
      setLoadingAttachments((prev) => ({ ...prev, [assignmentId]: true }));
      try {
        const response = await fetch(
          `${ASSIGNMENTS_API_URL}/${assignmentId}/attachments`
        );
        if (!response.ok) throw new Error("Failed to fetch attachments");
        const data = await response.json();
        setAttachments((prev) => ({ ...prev, [assignmentId]: data }));
        setAttachmentCounts((prev) => ({ ...prev, [assignmentId]: data.length }));
      } catch (err) {
        setError(err.toString());
      } finally {
        setLoadingAttachments((prev) => ({ ...prev, [assignmentId]: false }));
      }
    }
  };

  const handleUploadAttachment = async (assignmentId, file) => {
    if (!file) return;

    setUploadingAttachment((prev) => ({ ...prev, [assignmentId]: true }));
    try {
      const formData = new FormData();
      formData.append("file", file);

      const response = await fetch(
        `${ASSIGNMENTS_API_URL}/${assignmentId}/attachments`,
        {
          method: "POST",
          body: formData
        }
      );

      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || "Failed to upload attachment");
      }

      const uploaded = await response.json();
      setAttachments((prev) => ({
        ...prev,
        [assignmentId]: [...(prev[assignmentId] || []), uploaded]
      }));
      setAttachmentCounts((prev) => ({
        ...prev,
        [assignmentId]: (prev[assignmentId] ?? 0) + 1
      }));
    } catch (err) {
      setError(err.toString());
    } finally {
      setUploadingAttachment((prev) => ({ ...prev, [assignmentId]: false }));
    }
  };

  const handleDownloadAttachment = async (assignmentId, attachment) => {
    try {
      const response = await fetch(
        `${ASSIGNMENTS_API_URL}/${assignmentId}/attachments/${attachment.id}/download`
      );

      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || "Failed to download attachment");
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = attachment.fileName || "attachment";
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      setError(err.toString());
    }
  };

  const getAttachmentExtension = (fileName = "") => {
    const parts = fileName.toLowerCase().split(".");
    return parts.length > 1 ? parts.pop() : "";
  };

  const getPreviewKind = (fileName, contentType = "") => {
    const ext = getAttachmentExtension(fileName);
    const normalizedType = contentType.toLowerCase();

    if (normalizedType.startsWith("image/")) return "image";
    if (normalizedType === "application/pdf" || ext === "pdf") return "pdf";
    if (
      normalizedType.startsWith("text/") ||
      ["txt", "md", "json", "xml", "csv", "log"].includes(ext)
    ) {
      return "text";
    }
    if (
      ext === "docx" ||
      normalizedType ===
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ) {
      return "docx";
    }
    return "unsupported";
  };

  const closeAttachmentPreview = () => {
    setAttachmentPreview((prev) => {
      if (prev?.objectUrl) {
        window.URL.revokeObjectURL(prev.objectUrl);
      }
      return null;
    });
  };

  const handlePreviewAttachment = async (assignmentId, attachment) => {
    try {
      setPreviewingAttachment(true);

      const response = await fetch(
        `${ASSIGNMENTS_API_URL}/${assignmentId}/attachments/${attachment.id}/download`
      );

      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || "Failed to preview attachment");
      }

      const blob = await response.blob();
      const contentType =
        response.headers.get("content-type") ||
        attachment.contentType ||
        blob.type ||
        "";
      const kind = getPreviewKind(attachment.fileName, contentType);

      setAttachmentPreview((prev) => {
        if (prev?.objectUrl) {
          window.URL.revokeObjectURL(prev.objectUrl);
        }
        return prev;
      });

      if (kind === "image" || kind === "pdf") {
        const objectUrl = window.URL.createObjectURL(blob);
        setAttachmentPreview({
          fileName: attachment.fileName,
          kind,
          contentType,
          objectUrl,
          text: "",
          html: "",
          error: null
        });
        return;
      }

      if (kind === "text") {
        const text = await blob.text();
        setAttachmentPreview({
          fileName: attachment.fileName,
          kind,
          contentType,
          objectUrl: "",
          text,
          html: "",
          error: null
        });
        return;
      }

      if (kind === "docx") {
        const arrayBuffer = await blob.arrayBuffer();

        let mammothModule = null;
        try {
          mammothModule = await import("mammoth/mammoth.browser");
        } catch {
          mammothModule = await import("mammoth");
        }

        const mammothApi = mammothModule.default ?? mammothModule;
        const result = await mammothApi.convertToHtml({ arrayBuffer });

        setAttachmentPreview({
          fileName: attachment.fileName,
          kind,
          contentType,
          objectUrl: "",
          text: "",
          html: result.value || "",
          error: null
        });
        return;
      }

      setAttachmentPreview({
        fileName: attachment.fileName,
        kind: "unsupported",
        contentType,
        objectUrl: "",
        text: "",
        html: "",
        error:
          "Preview is not available for this file type yet. Use Download instead."
      });
    } catch (err) {
      setError(err.toString());
      setAttachmentPreview({
        fileName: attachment.fileName,
        kind: "unsupported",
        contentType: "",
        objectUrl: "",
        text: "",
        html: "",
        error: `Preview failed: ${err.toString()}`
      });
    } finally {
      setPreviewingAttachment(false);
    }
  };

  const toggleSubtasks = async (assignmentId) => {
    const isOpen = openSubtasks[assignmentId];
    setOpenSubtasks((prev) => ({
      ...prev,
      [assignmentId]: !isOpen
    }));

    // Fetch subtasks if opening for the first time
    if (!isOpen && !subtasks[assignmentId]) {
      setLoadingSubtasks((prev) => ({ ...prev, [assignmentId]: true }));
      try {
        const response = await fetch(
          `${ASSIGNMENTS_API_URL}/${assignmentId}/subtasks`
        );
        if (!response.ok) throw new Error("Failed to fetch subtasks");
        const data = await response.json();
        setSubtasks((prev) => ({ ...prev, [assignmentId]: data }));
        // Initialize notes state from API response
        const notesState = {};
        data.forEach((subtask) => {
          if (subtask.personalNote) {
            notesState[subtask.id] = subtask.personalNote;
          }
        });
        setSubtaskNotes((prev) => {
          const updated = { ...prev };
          Object.keys(notesState).forEach((id) => {
            updated[id] = notesState[id];
          });
          return updated;
        });
      } catch (err) {
        setError(err.toString());
      } finally {
        setLoadingSubtasks((prev) => ({ ...prev, [assignmentId]: false }));
      }
    }
  };

  const handleToggleSubtask = async (subtaskId, isCompleted) => {
    // Find which assignment this subtask belongs to
    const assignmentId =
      Object.keys(subtasks).find((aId) =>
        subtasks[aId]?.some((s) => s.id === subtaskId)
      ) || null;

    // Optimistic update - update UI immediately
    setSubtasks((prev) => {
      const updated = {};
      Object.keys(prev).forEach((aId) => {
        updated[aId] = prev[aId].map((subtask) =>
          subtask.id === subtaskId ? { ...subtask, isCompleted } : subtask
        );
      });
      return updated;
    });

    try {
      const response = await fetch(
        `http://localhost:5000/api/subtasks/${subtaskId}/toggle`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({ isCompleted })
        }
      );

      if (!response.ok) {
        throw new Error("Failed to toggle subtask");
      }

      // Refetch subtasks to ensure we have the latest data from server
      if (assignmentId) {
        try {
          const fetchResponse = await fetch(
            `${ASSIGNMENTS_API_URL}/${assignmentId}/subtasks`
          );
          if (fetchResponse.ok) {
            const data = await fetchResponse.json();
            setSubtasks((prev) => ({
              ...prev,
              [assignmentId]: data
            }));
            // Update notes state if needed
            const notesState = {};
            data.forEach((subtask) => {
              if (subtask.personalNote) {
                notesState[subtask.id] = subtask.personalNote;
              }
            });
            setSubtaskNotes((prev) => {
              const updated = { ...prev };
              Object.keys(notesState).forEach((id) => {
                updated[id] = notesState[id];
              });
              return updated;
            });
          }
        } catch (fetchErr) {
          console.error("Error refetching subtasks:", fetchErr);
        }
      }
    } catch (err) {
      // Revert optimistic update on error
      setSubtasks((prev) => {
        const updated = {};
        Object.keys(prev).forEach((aId) => {
          updated[aId] = prev[aId].map((subtask) =>
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
      const response = await fetch(
        `${ASSIGNMENTS_API_URL}/${assignmentId}/subtasks`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({ title: text })
        }
      );

      if (!response.ok) {
        throw new Error("Failed to add subtask");
      }

      const newSubtask = await response.json();
      setSubtasks((prev) => ({
        ...prev,
        [assignmentId]: [...(prev[assignmentId] || []), newSubtask]
      }));
      setNewSubtaskText((prev) => ({ ...prev, [assignmentId]: "" }));
    } catch (err) {
      setError(err.toString());
    }
  };

  const handleDeleteSubtask = async (subtaskId, assignmentId) => {
    try {
      const response = await fetch(
        `http://localhost:5000/api/subtasks/${subtaskId}`,
        {
          method: "DELETE"
        }
      );

      if (!response.ok) {
        throw new Error("Failed to delete subtask");
      }

      // Remove subtask from local state
      setSubtasks((prev) => ({
        ...prev,
        [assignmentId]: (prev[assignmentId] || []).filter(
          (s) => s.id !== subtaskId
        )
      }));

      // Also remove note from state if it exists
      setSubtaskNotes((prev) => {
        const updated = { ...prev };
        delete updated[subtaskId];
        return updated;
      });
    } catch (err) {
      setError(err.toString());
    }
  };

  const handleReorderSubtasks = async (assignmentId, newOrder) => {
    try {
      const response = await fetch(
        `${ASSIGNMENTS_API_URL}/${assignmentId}/subtasks/reorder`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({ subtaskOrders: newOrder })
        }
      );

      if (!response.ok) {
        throw new Error("Failed to reorder subtasks");
      }

      // Refetch subtasks to get updated order
      const fetchResponse = await fetch(
        `${ASSIGNMENTS_API_URL}/${assignmentId}/subtasks`
      );
      if (fetchResponse.ok) {
        const data = await fetchResponse.json();
        setSubtasks((prev) => ({
          ...prev,
          [assignmentId]: data
        }));
        // Update notes state if needed
        const notesState = {};
        data.forEach((subtask) => {
          if (subtask.personalNote) {
            notesState[subtask.id] = subtask.personalNote;
          }
        });
        setSubtaskNotes((prev) => {
          const updated = { ...prev };
          Object.keys(notesState).forEach((id) => {
            updated[id] = notesState[id];
          });
          return updated;
        });
      }
    } catch (err) {
      setError(err.toString());
    }
  };

  const handleDragStart = (e, subtaskId) => {
    setDraggedSubtaskId(subtaskId);
    e.dataTransfer.effectAllowed = "move";
    e.dataTransfer.setData("text/html", subtaskId);
  };

  const handleDragOver = (e, subtaskId) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = "move";
    if (subtaskId !== draggedSubtaskId) {
      setDragOverSubtaskId(subtaskId);
    }
  };

  const handleDragLeave = () => {
    setDragOverSubtaskId(null);
  };

  const handleDrop = (e, targetSubtaskId, assignmentId) => {
    e.preventDefault();
    setDragOverSubtaskId(null);

    if (!draggedSubtaskId || draggedSubtaskId === targetSubtaskId) {
      setDraggedSubtaskId(null);
      return;
    }

    const assignmentSubtasks = subtasks[assignmentId] || [];
    const draggedIndex = assignmentSubtasks.findIndex(
      (s) => s.id === draggedSubtaskId
    );
    const targetIndex = assignmentSubtasks.findIndex(
      (s) => s.id === targetSubtaskId
    );

    if (draggedIndex === -1 || targetIndex === -1) {
      setDraggedSubtaskId(null);
      return;
    }

    // Create new order mapping
    const newOrder = {};
    const reordered = [...assignmentSubtasks];
    const [draggedItem] = reordered.splice(draggedIndex, 1);
    reordered.splice(targetIndex, 0, draggedItem);

    // Assign new order values (0, 1, 2, ...)
    reordered.forEach((subtask, index) => {
      newOrder[subtask.id] = index;
    });

    // Optimistically update UI
    setSubtasks((prev) => ({
      ...prev,
      [assignmentId]: reordered
    }));

    // Update backend
    handleReorderSubtasks(assignmentId, newOrder);
    setDraggedSubtaskId(null);
  };

  const handleDragEnd = () => {
    setDraggedSubtaskId(null);
    setDragOverSubtaskId(null);
  };

  const handleToggleWorkingOn = async (assignmentId, isWorkingOn) => {
    try {
      const response = await fetch(
        `http://localhost:5000/api/assignments/${assignmentId}/working-on`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({ isWorkingOn })
        }
      );

      if (!response.ok) {
        throw new Error("Failed to update working on flag");
      }

      setWorkingOn((prev) => {
        const updated = new Set(prev);
        if (isWorkingOn) {
          updated.add(assignmentId);
        } else {
          updated.delete(assignmentId);
        }
        return updated;
      });
    } catch (err) {
      setError(err.toString());
    }
  };

  const handleUpdateSubtaskNote = async (subtaskId, note) => {
    try {
      const response = await fetch(
        `http://localhost:5000/api/subtasks/${subtaskId}/note`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({ note: note || null })
        }
      );

      if (!response.ok) {
        throw new Error("Failed to update note");
      }

      setSubtaskNotes((prev) => ({
        ...prev,
        [subtaskId]: note || null
      }));
      setEditingNotes((prev) => ({
        ...prev,
        [subtaskId]: false
      }));
    } catch (err) {
      setError(err.toString());
    }
  };

  const handleDeleteSubtaskNote = async (subtaskId) => {
    if (window.confirm("Are you sure you want to delete this personal note?")) {
      await handleUpdateSubtaskNote(subtaskId, null);
    }
  };

  const handleStartSubtaskEdit = (subtask) => {
    setEditingSubtasks((prev) => ({
      ...prev,
      [subtask.id]: true
    }));
    setSubtaskTitleDrafts((prev) => ({
      ...prev,
      [subtask.id]: subtask.title
    }));
  };

  const handleCancelSubtaskEdit = (subtaskId) => {
    setEditingSubtasks((prev) => ({
      ...prev,
      [subtaskId]: false
    }));
    setSubtaskTitleDrafts((prev) => {
      const updated = { ...prev };
      delete updated[subtaskId];
      return updated;
    });
  };

  const handleSaveSubtaskTitle = async (subtaskId) => {
    const draftTitle = (subtaskTitleDrafts[subtaskId] || "").trim();
    if (!draftTitle) {
      setError("Subtask title cannot be empty");
      return;
    }

    try {
      const response = await fetch(
        `http://localhost:5000/api/subtasks/${subtaskId}/title`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({ title: draftTitle })
        }
      );

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || "Failed to update subtask title");
      }

      setSubtasks((prev) => {
        const updated = {};
        Object.keys(prev).forEach((assignmentId) => {
          updated[assignmentId] = prev[assignmentId].map((subtask) =>
            subtask.id === subtaskId
              ? { ...subtask, title: draftTitle }
              : subtask
          );
        });
        return updated;
      });

      handleCancelSubtaskEdit(subtaskId);
    } catch (err) {
      setError(err.toString());
    }
  };

  const handleUpdateCommentNote = async (commentId, note) => {
    try {
      const response = await fetch(
        `http://localhost:5000/api/comments/${commentId}/note`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({ note: note || null })
        }
      );

      if (!response.ok) {
        throw new Error("Failed to update comment note");
      }

      setCommentNotes((prev) => ({
        ...prev,
        [commentId]: note || null
      }));
      setEditingCommentNotes((prev) => ({
        ...prev,
        [commentId]: false
      }));
    } catch (err) {
      setError(err.toString());
    }
  };

  const handleDeleteCommentNote = async (commentId) => {
    if (window.confirm("Are you sure you want to delete this personal note?")) {
      await handleUpdateCommentNote(commentId, null);
    }
  };

  const handleToggleCommentFlag = async (commentId, isFlagged) => {
    try {
      const response = await fetch(
        `http://localhost:5000/api/comments/${commentId}/flag`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({ isFlagged })
        }
      );

      if (!response.ok) {
        throw new Error("Failed to toggle comment flag");
      }

      setCommentFlags((prev) => ({
        ...prev,
        [commentId]: isFlagged
      }));
    } catch (err) {
      setError(err.toString());
    }
  };

  const hasUnreadComments = (assignmentId) => {
    const currentCount = commentCounts[assignmentId] || 0;
    const seenCount = lastSeenCommentCounts[assignmentId];
    if (seenCount === undefined || seenCount === null) return false;
    return currentCount > seenCount;
  };

  const sortCommentsByDate = (items) => {
    const sorted = [...items].sort((a, b) => {
      const dateA = new Date(a.createdDate).getTime();
      const dateB = new Date(b.createdDate).getTime();
      if (Number.isNaN(dateA) || Number.isNaN(dateB)) return 0;
      return dateB - dateA;
    });
    return commentSortNewestFirst ? sorted : sorted.reverse();
  };

  const formatCommentDate = (dateString) => {
    if (!dateString) return "";
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "Just now";
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return formatDate(dateString);
  };

  // Generate a deterministic color for a user based on their name
  const getUserColor = (userName) => {
    if (!userName) return "#6b7280";

    // Generate a hash from the username
    let hash = 0;
    for (let i = 0; i < userName.length; i++) {
      hash = userName.charCodeAt(i) + ((hash << 5) - hash);
    }

    // Use a palette of distinct colors
    const colors = [
      "#3b82f6", // blue
      "#10b981", // green
      "#f59e0b", // amber
      "#8b5cf6", // purple
      "#ec4899", // pink
      "#6366f1", // indigo
      "#14b8a6", // teal
      "#f97316", // orange
      "#06b6d4", // cyan
      "#a855f7" // violet
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
        <p className="subtitle">Connect. Organize. Complete.</p>
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
          <div className="assignment-controls">
            <div className="assignment-search-container">
              <input
                type="text"
                className="assignment-search-input"
                placeholder="Search assignments..."
                value={assignmentSearch}
                onChange={(e) => setAssignmentSearch(e.target.value)}
              />
            </div>
            <div className="assignment-filter-controls">
              <button
                className={`assignment-filter-toggle ${showOnlyUnreadAssignments ? "active" : ""}`}
                onClick={() =>
                  setShowOnlyUnreadAssignments(!showOnlyUnreadAssignments)
                }
                title="Show only assignments with unread comments"
              >
                {showOnlyUnreadAssignments
                  ? "Showing: Unread only"
                  : "Filter: Unread only"}
              </button>
              <button
                className={`assignment-filter-toggle ${sortUnreadToTop ? "active" : ""}`}
                onClick={() => setSortUnreadToTop(!sortUnreadToTop)}
                title="Sort assignments with unread comments to the top"
              >
                {sortUnreadToTop ? "Sort: Unread first" : "Sort: Default"}
              </button>
              <button
                className="assignment-refresh-button"
                onClick={() => refreshData(false)}
                title="Refresh assignments"
              >
                Refresh
              </button>
              <select
                className="assignment-refresh-interval"
                value={autoRefreshIntervalSeconds}
                onChange={(e) =>
                  setAutoRefreshIntervalSeconds(Number(e.target.value))
                }
                title="Auto-refresh interval"
              >
                <option value={0}>Auto-refresh: Off</option>
                <option value={30}>Auto-refresh: 30s</option>
                <option value={60}>Auto-refresh: 60s</option>
                <option value={120}>Auto-refresh: 120s</option>
              </select>
            </div>
            <div className="assignment-refresh-meta">
              Last refreshed:{" "}
              {lastRefreshedAt
                ? new Date(lastRefreshedAt).toLocaleTimeString()
                : "Not yet"}
            </div>
          </div>
          {assignments.length === 0 ? (
            <div className="empty-state">
              <p>No assignments found</p>
            </div>
          ) : (
            (() => {
              const normalizedSearch = assignmentSearch.trim().toLowerCase();
              const filteredAssignments = normalizedSearch
                ? assignments.filter((assignment) =>
                    [
                      assignment.title,
                      assignment.description,
                      assignment.assignedTo,
                      assignment.assigneeName
                    ]
                      .filter(Boolean)
                      .some((value) =>
                        value.toLowerCase().includes(normalizedSearch)
                      )
                  )
                : assignments;

              if (filteredAssignments.length === 0) {
                return (
                  <div className="empty-state">
                    <p>No assignments match "{assignmentSearch}"</p>
                  </div>
                );
              }

              const unreadFilteredAssignments = showOnlyUnreadAssignments
                ? filteredAssignments.filter((a) => hasUnreadComments(a.id))
                : filteredAssignments;

              if (unreadFilteredAssignments.length === 0) {
                return (
                  <div className="empty-state">
                    <p>
                      No assignments match the current filters.
                      {showOnlyUnreadAssignments
                        ? " Try turning off \"Unread only\"."
                        : ""}
                    </p>
                  </div>
                );
              }

              const orderedAssignments = sortUnreadToTop
                ? [...unreadFilteredAssignments].sort(
                    (a, b) =>
                      Number(hasUnreadComments(b.id)) -
                      Number(hasUnreadComments(a.id))
                  )
                : unreadFilteredAssignments;

              // Separate working on and other assignments
              const workingOnAssignments = orderedAssignments.filter((a) =>
                workingOn.has(a.id)
              );
              const otherAssignments = orderedAssignments.filter(
                (a) => !workingOn.has(a.id)
              );
              const unreadInAllAssignments = otherAssignments.filter((a) =>
                hasUnreadComments(a.id)
              ).length;

              return (
                <div className="assignments-list">
                  {workingOnAssignments.length > 0 && (
                    <div className="working-on-section">
                      <div className="working-on-header">
                        <h3 className="working-on-title">🔥 Hot Zone</h3>
                        <span className="working-on-count">
                          {workingOnAssignments.length}
                        </span>
                      </div>
                      {workingOnAssignments.map((assignment) => {
                        // Render full assignment card - we'll create a reusable component inline
                        return (
                          <div
                            key={assignment.id}
                            className={`assignment-card working-on ${hasUnreadComments(assignment.id) ? "has-unread-comments" : ""}`}
                          >
                            <div className="assignment-header">
                              <div className="assignment-title-row">
                                <button
                                  className="working-on-toggle active"
                                  onClick={() =>
                                    handleToggleWorkingOn(assignment.id, false)
                                  }
                                  title="Remove from Hot Zone"
                                >
                                  🔥
                                </button>
                                <h2 className="assignment-title">
                                  {assignment.title}
                                </h2>
                                {hasUnreadComments(assignment.id) && (
                                  <span className="assignment-unread-comments-badge">
                                    New comments
                                  </span>
                                )}
                              </div>
                              <span
                                className="status-badge"
                                style={{
                                  backgroundColor: getStatusColor(
                                    assignment.status
                                  )
                                }}
                              >
                                {getStatusLabel(assignment.status)}
                              </span>
                            </div>

                            {assignment.description && (
                              <p className="assignment-description">
                                {assignment.description}
                              </p>
                            )}

                            <div className="assignment-meta">
                              {assignment.dueDate && (
                                <span
                                  className={`due-date ${isOverdue(assignment.dueDate, assignment.status) ? "overdue" : ""}`}
                                >
                                  {isOverdue(
                                    assignment.dueDate,
                                    assignment.status
                                  )
                                    ? "⚠ "
                                    : ""}
                                  Due {formatDate(assignment.dueDate)}
                                </span>
                              )}
                              {(() => {
                                const assignmentSubtasks =
                                  subtasks[assignment.id] ||
                                  assignment.subtasks ||
                                  [];
                                if (assignmentSubtasks.length > 0) {
                                  const completedCount =
                                    assignmentSubtasks.filter(
                                      (s) => s.isCompleted
                                    ).length;
                                  return (
                                    <span className="subtask-count">
                                      {completedCount} /{" "}
                                      {assignmentSubtasks.length} subtasks
                                    </span>
                                  );
                                }
                                return null;
                              })()}
                              <button
                                className="comments-button"
                                onClick={() => toggleComments(assignment.id)}
                              >
                                💬 {commentCounts[assignment.id] || 0}{" "}
                                {openComments[assignment.id]
                                  ? "Hide"
                                  : "Comments"}
                                {!openComments[assignment.id] &&
                                  hasUnreadComments(assignment.id) && (
                                    <span className="comments-new-indicator">
                                      New
                                    </span>
                                  )}
                              </button>
                              <button
                                className="subtasks-button"
                                onClick={() => toggleSubtasks(assignment.id)}
                              >
                                ✓{" "}
                                {subtasks[assignment.id]?.length ??
                                  assignment.subtasks?.length ??
                                  0}{" "}
                                {openSubtasks[assignment.id]
                                  ? "Hide"
                                  : "Subtasks"}
                              </button>
                              <button
                                className="attachments-button"
                                onClick={() => toggleAttachments(assignment.id)}
                              >
                                📎{" "}
                                {attachmentCounts[assignment.id] ??
                                  attachments[assignment.id]?.length ??
                                  0}{" "}
                                {openAttachments[assignment.id]
                                  ? "Hide"
                                  : "Attachments"}
                              </button>
                              {assignment.status !== 2 && (
                                <button
                                  className="complete-button"
                                  onClick={() =>
                                    handleCompleteAssignment(assignment.id)
                                  }
                                >
                                  Complete
                                </button>
                              )}
                            </div>

                            {/* Comments Section */}
                            {openComments[assignment.id] && (
                              <div className="comments-section">
                                <div className="comments-filters">
                                  <select
                                    className="comment-filter-select"
                                    value={
                                      commentFilters[assignment.id]?.user || ""
                                    }
                                    onChange={(e) =>
                                      setCommentFilters((prev) => ({
                                        ...prev,
                                        [assignment.id]: {
                                          ...prev[assignment.id],
                                          user: e.target.value || null
                                        }
                                      }))
                                    }
                                  >
                                    <option value="">All Users</option>
                                    {comments[assignment.id] &&
                                      Array.from(
                                        new Set(
                                          comments[assignment.id].map(
                                            (c) => c.authorName
                                          )
                                        )
                                      )
                                        .sort()
                                        .map((userName) => (
                                          <option
                                            key={userName}
                                            value={userName}
                                          >
                                            {userName}
                                          </option>
                                        ))}
                                  </select>
                                  <select
                                    className="comment-filter-select"
                                    value={
                                      commentFilters[assignment.id]?.date || ""
                                    }
                                    onChange={(e) =>
                                      setCommentFilters((prev) => ({
                                        ...prev,
                                        [assignment.id]: {
                                          ...prev[assignment.id],
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
                                    value={
                                      commentFilters[assignment.id]?.flag || ""
                                    }
                                    onChange={(e) =>
                                      setCommentFilters((prev) => ({
                                        ...prev,
                                        [assignment.id]: {
                                          ...prev[assignment.id],
                                          flag: e.target.value || null
                                        }
                                      }))
                                    }
                                  >
                                    <option value="">All Comments</option>
                                    <option value="flagged">Flagged</option>
                                    <option value="not-flagged">
                                      Not Flagged
                                    </option>
                                  </select>
                                  <select
                                    className="comment-filter-select"
                                    value={
                                      commentFilters[assignment.id]?.note || ""
                                    }
                                    onChange={(e) =>
                                      setCommentFilters((prev) => ({
                                        ...prev,
                                        [assignment.id]: {
                                          ...prev[assignment.id],
                                          note: e.target.value || null
                                        }
                                      }))
                                    }
                                  >
                                    <option value="">All Comments</option>
                                    <option value="has-note">
                                      Has Personal Note
                                    </option>
                                    <option value="no-note">
                                      No Personal Note
                                    </option>
                                  </select>
                                  <button
                                    className="comment-sort-toggle"
                                    onClick={() =>
                                      setCommentSortNewestFirst(
                                        !commentSortNewestFirst
                                      )
                                    }
                                    title={
                                      commentSortNewestFirst
                                        ? "Switch to oldest comments first"
                                        : "Switch to newest comments first"
                                    }
                                  >
                                    {commentSortNewestFirst
                                      ? "Sort: Newest first"
                                      : "Sort: Oldest first"}
                                  </button>
                                  {(commentFilters[assignment.id]?.user ||
                                    commentFilters[assignment.id]?.date ||
                                    commentFilters[assignment.id]?.flag ||
                                    commentFilters[assignment.id]?.note) && (
                                    <button
                                      className="comment-filter-clear"
                                      onClick={() =>
                                        setCommentFilters((prev) => ({
                                          ...prev,
                                          [assignment.id]: {
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
                                  {loadingComments[assignment.id] ? (
                                    <div className="comments-loading">
                                      Loading comments...
                                    </div>
                                  ) : (
                                    (() => {
                                      const allComments =
                                        comments[assignment.id] || [];
                                      const filter =
                                        commentFilters[assignment.id] || {};

                                      let filteredComments = allComments;

                                      if (filter.user) {
                                        filteredComments =
                                          filteredComments.filter(
                                            (c) => c.authorName === filter.user
                                          );
                                      }

                                      if (filter.date) {
                                        const now = new Date();
                                        const today = new Date(
                                          now.getFullYear(),
                                          now.getMonth(),
                                          now.getDate()
                                        );

                                        filteredComments =
                                          filteredComments.filter((c) => {
                                            const commentDate = new Date(
                                              c.createdDate
                                            );
                                            const commentDateOnly = new Date(
                                              commentDate.getFullYear(),
                                              commentDate.getMonth(),
                                              commentDate.getDate()
                                            );

                                            switch (filter.date) {
                                              case "today": {
                                                return (
                                                  commentDateOnly.getTime() ===
                                                  today.getTime()
                                                );
                                              }
                                              case "week": {
                                                const weekAgo = new Date(today);
                                                weekAgo.setDate(
                                                  weekAgo.getDate() - 7
                                                );
                                                return (
                                                  commentDateOnly >= weekAgo
                                                );
                                              }
                                              case "month": {
                                                const monthAgo = new Date(
                                                  today
                                                );
                                                monthAgo.setMonth(
                                                  monthAgo.getMonth() - 1
                                                );
                                                return (
                                                  commentDateOnly >= monthAgo
                                                );
                                              }
                                              default:
                                                return true;
                                            }
                                          });
                                      }

                                      if (filter.flag) {
                                        if (filter.flag === "flagged") {
                                          filteredComments =
                                            filteredComments.filter(
                                              (c) => commentFlags[c.id] === true
                                            );
                                        } else if (
                                          filter.flag === "not-flagged"
                                        ) {
                                          filteredComments =
                                            filteredComments.filter(
                                              (c) => !commentFlags[c.id]
                                            );
                                        }
                                      }

                                      if (filter.note) {
                                        if (filter.note === "has-note") {
                                          filteredComments =
                                            filteredComments.filter(
                                              (c) => commentNotes[c.id]
                                            );
                                        } else if (filter.note === "no-note") {
                                          filteredComments =
                                            filteredComments.filter(
                                              (c) => !commentNotes[c.id]
                                            );
                                        }
                                      }

                                      filteredComments =
                                        sortCommentsByDate(filteredComments);

                                      return filteredComments.length > 0 ? (
                                        filteredComments.map((comment) => {
                                          const isCurrentUser =
                                            currentUser &&
                                            comment.authorName === currentUser;
                                          const userColor = getUserColor(
                                            comment.authorName
                                          );
                                          return (
                                            <div
                                              key={comment.id}
                                              className="comment-bubble"
                                              style={{
                                                "--user-color": userColor,
                                                backgroundColor: isCurrentUser
                                                  ? hexToRgba(userColor, 0.2)
                                                  : "rgba(255, 255, 255, 0.08)",
                                                borderColor: isCurrentUser
                                                  ? hexToRgba(userColor, 0.4)
                                                  : "rgba(255, 255, 255, 0.1)"
                                              }}
                                            >
                                              <div className="comment-header">
                                                <span
                                                  className="comment-author"
                                                  style={{ color: userColor }}
                                                >
                                                  {comment.authorName}
                                                </span>
                                                <span className="comment-date">
                                                  {formatCommentDate(
                                                    comment.createdDate
                                                  )}
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
                                                    commentFlags[comment.id]
                                                      ? "Unflag comment"
                                                      : "Flag comment"
                                                  }
                                                >
                                                  {commentFlags[comment.id]
                                                    ? "🚩"
                                                    : "🏳️"}
                                                </button>
                                                <button
                                                  className="comment-note-button"
                                                  onClick={() => {
                                                    setEditingCommentNotes(
                                                      (prev) => ({
                                                        ...prev,
                                                        [comment.id]:
                                                          !prev[comment.id]
                                                      })
                                                    );
                                                    if (
                                                      !commentNotes[comment.id]
                                                    ) {
                                                      fetch(
                                                        `http://localhost:5000/api/comments/${comment.id}/note`
                                                      )
                                                        .then((res) =>
                                                          res.ok
                                                            ? res.json()
                                                            : null
                                                        )
                                                        .then((data) => {
                                                          if (data?.note) {
                                                            setCommentNotes(
                                                              (prev) => ({
                                                                ...prev,
                                                                [comment.id]:
                                                                  data.note
                                                              })
                                                            );
                                                          }
                                                        })
                                                        .catch((err) =>
                                                          console.error(
                                                            "Error loading comment note:",
                                                            err
                                                          )
                                                        );
                                                    }
                                                  }}
                                                  title={
                                                    commentNotes[comment.id]
                                                      ? "Edit personal note"
                                                      : "Add personal note"
                                                  }
                                                >
                                                  {commentNotes[comment.id]
                                                    ? "📝"
                                                    : "📄"}
                                                </button>
                                              </div>
                                              <div className="comment-content">
                                                {comment.content}
                                              </div>
                                              {editingCommentNotes[
                                                comment.id
                                              ] && (
                                                <div className="comment-note-editor">
                                                  <textarea
                                                    className="comment-note-input"
                                                    placeholder="Add a personal note about this comment..."
                                                    value={
                                                      commentNotes[
                                                        comment.id
                                                      ] || ""
                                                    }
                                                    onChange={(e) =>
                                                      setCommentNotes(
                                                        (prev) => ({
                                                          ...prev,
                                                          [comment.id]:
                                                            e.target.value
                                                        })
                                                      )
                                                    }
                                                    rows={3}
                                                  />
                                                  <div className="comment-note-actions">
                                                    <button
                                                      className="comment-note-save"
                                                      onClick={() =>
                                                        handleUpdateCommentNote(
                                                          comment.id,
                                                          commentNotes[
                                                            comment.id
                                                          ]
                                                        )
                                                      }
                                                    >
                                                      Save
                                                    </button>
                                                    <button
                                                      className="comment-note-cancel"
                                                      onClick={() => {
                                                        setEditingCommentNotes(
                                                          (prev) => ({
                                                            ...prev,
                                                            [comment.id]: false
                                                          })
                                                        );
                                                      }}
                                                    >
                                                      Cancel
                                                    </button>
                                                  </div>
                                                </div>
                                              )}
                                              {!editingCommentNotes[
                                                comment.id
                                              ] &&
                                                commentNotes[comment.id] && (
                                                  <div className="comment-note-display">
                                                    <div className="comment-note-header">
                                                      <span className="comment-note-label">
                                                        Personal Note:
                                                      </span>
                                                      <button
                                                        className="comment-note-delete-button"
                                                        onClick={() =>
                                                          handleDeleteCommentNote(
                                                            comment.id
                                                          )
                                                        }
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
                                      );
                                    })()
                                  )}
                                </div>
                                <div className="comment-input-container">
                                  <textarea
                                    className="comment-input"
                                    placeholder="Add a comment..."
                                    value={newCommentText[assignment.id] || ""}
                                    onChange={(e) =>
                                      setNewCommentText((prev) => ({
                                        ...prev,
                                        [assignment.id]: e.target.value
                                      }))
                                    }
                                    onKeyDown={(e) => {
                                      if (
                                        e.key === "Enter" &&
                                        (e.ctrlKey || e.metaKey)
                                      ) {
                                        e.preventDefault();
                                        handleAddComment(assignment.id);
                                      }
                                    }}
                                    rows={3}
                                  />
                                  <button
                                    className="comment-submit-button"
                                    onClick={() =>
                                      handleAddComment(assignment.id)
                                    }
                                    disabled={
                                      !newCommentText[assignment.id]?.trim()
                                    }
                                  >
                                    Post
                                  </button>
                                </div>
                              </div>
                            )}

                            {/* Attachments Section */}
                            {openAttachments[assignment.id] && (
                              <div className="attachments-section">
                                <div className="attachments-list">
                                  {loadingAttachments[assignment.id] ? (
                                    <div className="attachments-loading">
                                      Loading attachments...
                                    </div>
                                  ) : (attachments[assignment.id] || []).length >
                                    0 ? (
                                    (attachments[assignment.id] || []).map(
                                      (attachment) => (
                                        <div
                                          key={attachment.id}
                                          className="attachment-item"
                                        >
                                          <div className="attachment-actions">
                                            <button
                                              className="attachment-link"
                                              onClick={() =>
                                                handleDownloadAttachment(
                                                  assignment.id,
                                                  attachment
                                                )
                                              }
                                              title={`Download ${attachment.fileName}`}
                                            >
                                              {attachment.fileName}
                                            </button>
                                            <button
                                              className="attachment-preview-button"
                                              onClick={() =>
                                                handlePreviewAttachment(
                                                  assignment.id,
                                                  attachment
                                                )
                                              }
                                              disabled={previewingAttachment}
                                              title={`Preview ${attachment.fileName}`}
                                            >
                                              {previewingAttachment
                                                ? "Opening..."
                                                : "Preview"}
                                            </button>
                                          </div>
                                          <span className="attachment-size">
                                            {Math.max(
                                              1,
                                              Math.round(
                                                attachment.sizeBytes / 1024
                                              )
                                            )}{" "}
                                            KB
                                          </span>
                                        </div>
                                      )
                                    )
                                  ) : (
                                    <div className="attachments-empty">
                                      No attachments yet.
                                    </div>
                                  )}
                                </div>
                                <div className="attachment-upload-container">
                                  <input
                                    type="file"
                                    className="attachment-upload-input"
                                    onChange={(e) => {
                                      const file = e.target.files?.[0];
                                      if (file) {
                                        handleUploadAttachment(
                                          assignment.id,
                                          file
                                        );
                                        e.target.value = "";
                                      }
                                    }}
                                    disabled={uploadingAttachment[assignment.id]}
                                  />
                                  {uploadingAttachment[assignment.id] && (
                                    <span className="attachment-uploading">
                                      Uploading...
                                    </span>
                                  )}
                                </div>
                              </div>
                            )}

                            {/* Subtasks Section */}
                            {openSubtasks[assignment.id] && (
                              <div className="subtasks-section">
                                <div className="subtasks-list">
                                  {loadingSubtasks[assignment.id] ? (
                                    <div className="subtasks-loading">
                                      Loading subtasks...
                                    </div>
                                  ) : (
                                    (() => {
                                      const assignmentSubtasks =
                                        subtasks[assignment.id] || [];

                                      return assignmentSubtasks.length > 0 ? (
                                        [...assignmentSubtasks]
                                          .sort(
                                            (a, b) =>
                                              a.isCompleted - b.isCompleted
                                          )
                                          .map((subtask) => (
                                            <div
                                              key={subtask.id}
                                              className={`subtask-item ${draggedSubtaskId === subtask.id ? "dragging" : ""} ${dragOverSubtaskId === subtask.id ? "drag-over" : ""}`}
                                              draggable
                                              onDragStart={(e) =>
                                                handleDragStart(e, subtask.id)
                                              }
                                              onDragOver={(e) =>
                                                handleDragOver(e, subtask.id)
                                              }
                                              onDragLeave={handleDragLeave}
                                              onDrop={(e) =>
                                                handleDrop(
                                                  e,
                                                  subtask.id,
                                                  assignment.id
                                                )
                                              }
                                              onDragEnd={handleDragEnd}
                                            >
                                              <div className="subtask-drag-handle">
                                                ⋮⋮
                                              </div>
                                              <div className="subtask-content">
                                                <div className="subtask-main">
                                                  <label className="subtask-checkbox-label">
                                                    <input
                                                      type="checkbox"
                                                      className="subtask-checkbox"
                                                      checked={
                                                        subtask.isCompleted
                                                      }
                                                      onChange={(e) =>
                                                        handleToggleSubtask(
                                                          subtask.id,
                                                          e.target.checked
                                                        )
                                                      }
                                                      onMouseDown={(e) =>
                                                        e.stopPropagation()
                                                      }
                                                    />
                                                    {editingSubtasks[
                                                      subtask.id
                                                    ] ? (
                                                      <input
                                                        type="text"
                                                        className="subtask-title-input"
                                                        value={
                                                          subtaskTitleDrafts[
                                                            subtask.id
                                                          ] ?? ""
                                                        }
                                                        onChange={(e) =>
                                                          setSubtaskTitleDrafts(
                                                            (prev) => ({
                                                              ...prev,
                                                              [subtask.id]:
                                                                e.target.value
                                                            })
                                                          )
                                                        }
                                                        onKeyDown={(e) => {
                                                          if (e.key === "Enter") {
                                                            e.preventDefault();
                                                            handleSaveSubtaskTitle(
                                                              subtask.id
                                                            );
                                                          } else if (
                                                            e.key === "Escape"
                                                          ) {
                                                            e.preventDefault();
                                                            handleCancelSubtaskEdit(
                                                              subtask.id
                                                            );
                                                          }
                                                        }}
                                                        onMouseDown={(e) =>
                                                          e.stopPropagation()
                                                        }
                                                        onClick={(e) =>
                                                          e.stopPropagation()
                                                        }
                                                        autoFocus
                                                      />
                                                    ) : (
                                                      <span
                                                        className={`subtask-title ${subtask.isCompleted ? "completed" : ""}`}
                                                      >
                                                        {subtask.title}
                                                      </span>
                                                    )}
                                                  </label>
                                                  {editingSubtasks[
                                                    subtask.id
                                                  ] ? (
                                                    <>
                                                      <button
                                                        className="subtask-edit-save"
                                                        onMouseDown={(e) =>
                                                          e.stopPropagation()
                                                        }
                                                        onClick={() =>
                                                          handleSaveSubtaskTitle(
                                                            subtask.id
                                                          )
                                                        }
                                                        title="Save title"
                                                      >
                                                        Save
                                                      </button>
                                                      <button
                                                        className="subtask-edit-cancel"
                                                        onMouseDown={(e) =>
                                                          e.stopPropagation()
                                                        }
                                                        onClick={() =>
                                                          handleCancelSubtaskEdit(
                                                            subtask.id
                                                          )
                                                        }
                                                        title="Cancel edit"
                                                      >
                                                        Cancel
                                                      </button>
                                                    </>
                                                  ) : (
                                                    <button
                                                      className="subtask-edit-button"
                                                      onMouseDown={(e) =>
                                                        e.stopPropagation()
                                                      }
                                                      onClick={() =>
                                                        handleStartSubtaskEdit(
                                                          subtask
                                                        )
                                                      }
                                                      title="Edit subtask"
                                                    >
                                                      ✏️
                                                    </button>
                                                  )}
                                                  <button
                                                    className="subtask-note-button"
                                                    onMouseDown={(e) =>
                                                      e.stopPropagation()
                                                    }
                                                    onClick={() => {
                                                      setEditingNotes(
                                                        (prev) => ({
                                                          ...prev,
                                                          [subtask.id]:
                                                            !prev[subtask.id]
                                                        })
                                                      );
                                                      if (
                                                        !subtaskNotes[
                                                          subtask.id
                                                        ] &&
                                                        subtask.personalNote
                                                      ) {
                                                        setSubtaskNotes(
                                                          (prev) => ({
                                                            ...prev,
                                                            [subtask.id]:
                                                              subtask.personalNote
                                                          })
                                                        );
                                                      }
                                                    }}
                                                    title={
                                                      subtaskNotes[
                                                        subtask.id
                                                      ] || subtask.personalNote
                                                        ? "Edit note"
                                                        : "Add note"
                                                    }
                                                  >
                                                    {subtaskNotes[subtask.id] ||
                                                    subtask.personalNote
                                                      ? "📝"
                                                      : "📄"}
                                                  </button>
                                                  <button
                                                    className="subtask-delete-button"
                                                    onMouseDown={(e) =>
                                                      e.stopPropagation()
                                                    }
                                                    onClick={() => {
                                                      if (
                                                        window.confirm(
                                                          "Are you sure you want to delete this subtask?"
                                                        )
                                                      ) {
                                                        handleDeleteSubtask(
                                                          subtask.id,
                                                          assignment.id
                                                        );
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
                                                        subtaskNotes[
                                                          subtask.id
                                                        ] ??
                                                        subtask.personalNote ??
                                                        ""
                                                      }
                                                      onChange={(e) =>
                                                        setSubtaskNotes(
                                                          (prev) => ({
                                                            ...prev,
                                                            [subtask.id]:
                                                              e.target.value
                                                          })
                                                        )
                                                      }
                                                      rows={3}
                                                    />
                                                    <div className="subtask-note-actions">
                                                      <button
                                                        className="subtask-note-save"
                                                        onClick={() =>
                                                          handleUpdateSubtaskNote(
                                                            subtask.id,
                                                            subtaskNotes[
                                                              subtask.id
                                                            ]
                                                          )
                                                        }
                                                      >
                                                        Save
                                                      </button>
                                                      <button
                                                        className="subtask-note-cancel"
                                                        onClick={() => {
                                                          setEditingNotes(
                                                            (prev) => ({
                                                              ...prev,
                                                              [subtask.id]: false
                                                            })
                                                          );
                                                          setSubtaskNotes(
                                                            (prev) => {
                                                              const updated = {
                                                                ...prev
                                                              };
                                                              if (
                                                                subtask.personalNote
                                                              ) {
                                                                updated[
                                                                  subtask.id
                                                                ] =
                                                                  subtask.personalNote;
                                                              } else {
                                                                delete updated[
                                                                  subtask.id
                                                                ];
                                                              }
                                                              return updated;
                                                            }
                                                          );
                                                        }}
                                                      >
                                                        Cancel
                                                      </button>
                                                    </div>
                                                  </div>
                                                )}
                                                {!editingNotes[subtask.id] &&
                                                  (subtaskNotes[subtask.id] ||
                                                    subtask.personalNote) && (
                                                    <div className="subtask-note-display">
                                                      <div className="subtask-note-header">
                                                        <span className="subtask-note-label">
                                                          Note:
                                                        </span>
                                                        <button
                                                          className="subtask-note-delete-button"
                                                          onClick={() =>
                                                            handleDeleteSubtaskNote(
                                                              subtask.id
                                                            )
                                                          }
                                                          title="Delete personal note"
                                                        >
                                                          🗑️
                                                        </button>
                                                      </div>
                                                      <span className="subtask-note-text">
                                                        {subtaskNotes[
                                                          subtask.id
                                                        ] ||
                                                          subtask.personalNote}
                                                      </span>
                                                    </div>
                                                  )}
                                              </div>
                                            </div>
                                          ))
                                      ) : (
                                        <div className="subtasks-empty">
                                          No subtasks yet. Add one below!
                                        </div>
                                      );
                                    })()
                                  )}
                                </div>
                                <div className="subtask-input-container">
                                  <input
                                    type="text"
                                    className="subtask-input"
                                    placeholder="Add a subtask..."
                                    value={newSubtaskText[assignment.id] || ""}
                                    onChange={(e) =>
                                      setNewSubtaskText((prev) => ({
                                        ...prev,
                                        [assignment.id]: e.target.value
                                      }))
                                    }
                                    onKeyDown={(e) => {
                                      if (e.key === "Enter") {
                                        e.preventDefault();
                                        handleAddSubtask(assignment.id);
                                      }
                                    }}
                                  />
                                  <button
                                    className="subtask-submit-button"
                                    onClick={() =>
                                      handleAddSubtask(assignment.id)
                                    }
                                    disabled={
                                      !newSubtaskText[assignment.id]?.trim()
                                    }
                                  >
                                    Add
                                  </button>
                                </div>
                              </div>
                            )}
                          </div>
                        );
                      })}
                    </div>
                  )}
                  {workingOnAssignments.length > 0 &&
                    otherAssignments.length > 0 && (
                      <div className="assignments-divider">
                        <button
                          className="assignments-section-toggle"
                          onClick={() =>
                            setAllAssignmentsCollapsed(!allAssignmentsCollapsed)
                          }
                          title={
                            allAssignmentsCollapsed
                              ? "Expand all assignments"
                              : "Collapse all assignments"
                          }
                        >
                          <span className="section-toggle-icon">
                            {allAssignmentsCollapsed ? "▶" : "▼"}
                          </span>
                          <span className="divider-text">All Assignments</span>
                          <span className="section-count">
                            ({otherAssignments.length})
                          </span>
                          {unreadInAllAssignments > 0 && (
                            <span className="all-assignments-unread-summary">
                              {unreadInAllAssignments} new
                            </span>
                          )}
                        </button>
                      </div>
                    )}
                  {!allAssignmentsCollapsed &&
                    otherAssignments.map((assignment) => (
                      <div
                        key={assignment.id}
                        className={`assignment-card ${hasUnreadComments(assignment.id) ? "has-unread-comments" : ""}`}
                      >
                        <div className="assignment-header">
                          <div className="assignment-title-row">
                            <button
                              className="working-on-toggle"
                              onClick={() =>
                                handleToggleWorkingOn(assignment.id, true)
                              }
                              title="Add to Hot Zone"
                            >
                              ⭐
                            </button>
                            <h2 className="assignment-title">
                              {assignment.title}
                            </h2>
                            {hasUnreadComments(assignment.id) && (
                              <span className="assignment-unread-comments-badge">
                                New comments
                              </span>
                            )}
                          </div>
                          <span
                            className="status-badge"
                            style={{
                              backgroundColor: getStatusColor(assignment.status)
                            }}
                          >
                            {getStatusLabel(assignment.status)}
                          </span>
                        </div>

                        {assignment.description && (
                          <p className="assignment-description">
                            {assignment.description}
                          </p>
                        )}

                        <div className="assignment-meta">
                          {assignment.dueDate && (
                            <span
                              className={`due-date ${isOverdue(assignment.dueDate, assignment.status) ? "overdue" : ""}`}
                            >
                              {isOverdue(assignment.dueDate, assignment.status)
                                ? "⚠ "
                                : ""}
                              Due {formatDate(assignment.dueDate)}
                            </span>
                          )}
                          {(() => {
                            const assignmentSubtasks =
                              subtasks[assignment.id] ||
                              assignment.subtasks ||
                              [];
                            if (assignmentSubtasks.length > 0) {
                              const completedCount = assignmentSubtasks.filter(
                                (s) => s.isCompleted
                              ).length;
                              return (
                                <span className="subtask-count">
                                  {completedCount} / {assignmentSubtasks.length}{" "}
                                  subtasks
                                </span>
                              );
                            }
                            return null;
                          })()}
                          <button
                            className="comments-button"
                            onClick={() => toggleComments(assignment.id)}
                          >
                            💬 {commentCounts[assignment.id] || 0}{" "}
                            {openComments[assignment.id] ? "Hide" : "Comments"}
                            {!openComments[assignment.id] &&
                              hasUnreadComments(assignment.id) && (
                                <span className="comments-new-indicator">
                                  New
                                </span>
                              )}
                          </button>
                          <button
                            className="subtasks-button"
                            onClick={() => toggleSubtasks(assignment.id)}
                          >
                            ✓{" "}
                            {subtasks[assignment.id]?.length ??
                              assignment.subtasks?.length ??
                              0}{" "}
                            {openSubtasks[assignment.id] ? "Hide" : "Subtasks"}
                          </button>
                          <button
                            className="attachments-button"
                            onClick={() => toggleAttachments(assignment.id)}
                          >
                            📎{" "}
                            {attachmentCounts[assignment.id] ??
                              attachments[assignment.id]?.length ??
                              0}{" "}
                            {openAttachments[assignment.id]
                              ? "Hide"
                              : "Attachments"}
                          </button>
                          {assignment.status !== 2 && (
                            <button
                              className="complete-button"
                              onClick={() =>
                                handleCompleteAssignment(assignment.id)
                              }
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
                                value={
                                  commentFilters[assignment.id]?.user || ""
                                }
                                onChange={(e) =>
                                  setCommentFilters((prev) => ({
                                    ...prev,
                                    [assignment.id]: {
                                      ...prev[assignment.id],
                                      user: e.target.value || null
                                    }
                                  }))
                                }
                              >
                                <option value="">All Users</option>
                                {comments[assignment.id] &&
                                  Array.from(
                                    new Set(
                                      comments[assignment.id].map(
                                        (c) => c.authorName
                                      )
                                    )
                                  )
                                    .sort()
                                    .map((userName) => (
                                      <option key={userName} value={userName}>
                                        {userName}
                                      </option>
                                    ))}
                              </select>
                              <select
                                className="comment-filter-select"
                                value={
                                  commentFilters[assignment.id]?.date || ""
                                }
                                onChange={(e) =>
                                  setCommentFilters((prev) => ({
                                    ...prev,
                                    [assignment.id]: {
                                      ...prev[assignment.id],
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
                                value={
                                  commentFilters[assignment.id]?.flag || ""
                                }
                                onChange={(e) =>
                                  setCommentFilters((prev) => ({
                                    ...prev,
                                    [assignment.id]: {
                                      ...prev[assignment.id],
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
                                value={
                                  commentFilters[assignment.id]?.note || ""
                                }
                                onChange={(e) =>
                                  setCommentFilters((prev) => ({
                                    ...prev,
                                    [assignment.id]: {
                                      ...prev[assignment.id],
                                      note: e.target.value || null
                                    }
                                  }))
                                }
                              >
                                <option value="">All Comments</option>
                                <option value="has-note">
                                  Has Personal Note
                                </option>
                                <option value="no-note">
                                  No Personal Note
                                </option>
                              </select>
                              <button
                                className="comment-sort-toggle"
                                onClick={() =>
                                  setCommentSortNewestFirst(
                                    !commentSortNewestFirst
                                  )
                                }
                                title={
                                  commentSortNewestFirst
                                    ? "Switch to oldest comments first"
                                    : "Switch to newest comments first"
                                }
                              >
                                {commentSortNewestFirst
                                  ? "Sort: Newest first"
                                  : "Sort: Oldest first"}
                              </button>
                              {(commentFilters[assignment.id]?.user ||
                                commentFilters[assignment.id]?.date ||
                                commentFilters[assignment.id]?.flag ||
                                commentFilters[assignment.id]?.note) && (
                                <button
                                  className="comment-filter-clear"
                                  onClick={() =>
                                    setCommentFilters((prev) => ({
                                      ...prev,
                                      [assignment.id]: {
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
                              {loadingComments[assignment.id] ? (
                                <div className="comments-loading">
                                  Loading comments...
                                </div>
                              ) : (
                                (() => {
                                  const allComments =
                                    comments[assignment.id] || [];
                                  const filter =
                                    commentFilters[assignment.id] || {};

                                  let filteredComments = allComments;

                                  // Filter by user
                                  if (filter.user) {
                                    filteredComments = filteredComments.filter(
                                      (c) => c.authorName === filter.user
                                    );
                                  }

                                  // Filter by date
                                  if (filter.date) {
                                    const now = new Date();
                                    const today = new Date(
                                      now.getFullYear(),
                                      now.getMonth(),
                                      now.getDate()
                                    );

                                    filteredComments = filteredComments.filter(
                                      (c) => {
                                        const commentDate = new Date(
                                          c.createdDate
                                        );
                                        const commentDateOnly = new Date(
                                          commentDate.getFullYear(),
                                          commentDate.getMonth(),
                                          commentDate.getDate()
                                        );

                                        switch (filter.date) {
                                          case "today": {
                                            return (
                                              commentDateOnly.getTime() ===
                                              today.getTime()
                                            );
                                          }
                                          case "week": {
                                            const weekAgo = new Date(today);
                                            weekAgo.setDate(
                                              weekAgo.getDate() - 7
                                            );
                                            return commentDateOnly >= weekAgo;
                                          }
                                          case "month": {
                                            const monthAgo = new Date(today);
                                            monthAgo.setMonth(
                                              monthAgo.getMonth() - 1
                                            );
                                            return commentDateOnly >= monthAgo;
                                          }
                                          default:
                                            return true;
                                        }
                                      }
                                    );
                                  }

                                  // Filter by flag
                                  if (filter.flag) {
                                    if (filter.flag === "flagged") {
                                      filteredComments =
                                        filteredComments.filter(
                                          (c) => commentFlags[c.id] === true
                                        );
                                    } else if (filter.flag === "not-flagged") {
                                      filteredComments =
                                        filteredComments.filter(
                                          (c) => !commentFlags[c.id]
                                        );
                                    }
                                  }

                                  // Filter by note
                                  if (filter.note) {
                                    if (filter.note === "has-note") {
                                      filteredComments =
                                        filteredComments.filter(
                                          (c) => commentNotes[c.id]
                                        );
                                    } else if (filter.note === "no-note") {
                                      filteredComments =
                                        filteredComments.filter(
                                          (c) => !commentNotes[c.id]
                                        );
                                    }
                                  }

                                  filteredComments =
                                    sortCommentsByDate(filteredComments);

                                  return filteredComments.length > 0 ? (
                                    filteredComments.map((comment) => {
                                      const isCurrentUser =
                                        currentUser &&
                                        comment.authorName === currentUser;
                                      const userColor = getUserColor(
                                        comment.authorName
                                      );
                                      return (
                                        <div
                                          key={comment.id}
                                          className="comment-bubble"
                                          style={{
                                            "--user-color": userColor,
                                            backgroundColor: isCurrentUser
                                              ? hexToRgba(userColor, 0.2)
                                              : "rgba(255, 255, 255, 0.08)",
                                            borderColor: isCurrentUser
                                              ? hexToRgba(userColor, 0.4)
                                              : "rgba(255, 255, 255, 0.1)"
                                          }}
                                        >
                                          <div className="comment-header">
                                            <span
                                              className="comment-author"
                                              style={{ color: userColor }}
                                            >
                                              {comment.authorName}
                                            </span>
                                            <span className="comment-date">
                                              {formatCommentDate(
                                                comment.createdDate
                                              )}
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
                                                commentFlags[comment.id]
                                                  ? "Unflag comment"
                                                  : "Flag comment"
                                              }
                                            >
                                              {commentFlags[comment.id]
                                                ? "🚩"
                                                : "🏳️"}
                                            </button>
                                            <button
                                              className="comment-note-button"
                                              onClick={() => {
                                                setEditingCommentNotes(
                                                  (prev) => ({
                                                    ...prev,
                                                    [comment.id]:
                                                      !prev[comment.id]
                                                  })
                                                );
                                                // Load note if not already loaded
                                                if (!commentNotes[comment.id]) {
                                                  fetch(
                                                    `http://localhost:5000/api/comments/${comment.id}/note`
                                                  )
                                                    .then((res) =>
                                                      res.ok ? res.json() : null
                                                    )
                                                    .then((data) => {
                                                      if (data?.note) {
                                                        setCommentNotes(
                                                          (prev) => ({
                                                            ...prev,
                                                            [comment.id]:
                                                              data.note
                                                          })
                                                        );
                                                      }
                                                    })
                                                    .catch((err) =>
                                                      console.error(
                                                        "Error loading comment note:",
                                                        err
                                                      )
                                                    );
                                                }
                                              }}
                                              title={
                                                commentNotes[comment.id]
                                                  ? "Edit personal note"
                                                  : "Add personal note"
                                              }
                                            >
                                              {commentNotes[comment.id]
                                                ? "📝"
                                                : "📄"}
                                            </button>
                                          </div>
                                          <div className="comment-content">
                                            {comment.content}
                                          </div>
                                          {editingCommentNotes[comment.id] && (
                                            <div className="comment-note-editor">
                                              <textarea
                                                className="comment-note-input"
                                                placeholder="Add a personal note about this comment..."
                                                value={
                                                  commentNotes[comment.id] || ""
                                                }
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
                                                    handleUpdateCommentNote(
                                                      comment.id,
                                                      commentNotes[comment.id]
                                                    )
                                                  }
                                                >
                                                  Save
                                                </button>
                                                <button
                                                  className="comment-note-cancel"
                                                  onClick={() => {
                                                    setEditingCommentNotes(
                                                      (prev) => ({
                                                        ...prev,
                                                        [comment.id]: false
                                                      })
                                                    );
                                                  }}
                                                >
                                                  Cancel
                                                </button>
                                              </div>
                                            </div>
                                          )}
                                          {!editingCommentNotes[comment.id] &&
                                            commentNotes[comment.id] && (
                                              <div className="comment-note-display">
                                                <div className="comment-note-header">
                                                  <span className="comment-note-label">
                                                    Personal Note:
                                                  </span>
                                                  <button
                                                    className="comment-note-delete-button"
                                                    onClick={() =>
                                                      handleDeleteCommentNote(
                                                        comment.id
                                                      )
                                                    }
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
                                  );
                                })()
                              )}
                            </div>
                            <div className="comment-input-container">
                              <textarea
                                className="comment-input"
                                placeholder="Add a comment..."
                                value={newCommentText[assignment.id] || ""}
                                onChange={(e) =>
                                  setNewCommentText((prev) => ({
                                    ...prev,
                                    [assignment.id]: e.target.value
                                  }))
                                }
                                onKeyDown={(e) => {
                                  if (
                                    e.key === "Enter" &&
                                    (e.ctrlKey || e.metaKey)
                                  ) {
                                    e.preventDefault();
                                    handleAddComment(assignment.id);
                                  }
                                }}
                                rows={3}
                              />
                              <button
                                className="comment-submit-button"
                                onClick={() => handleAddComment(assignment.id)}
                                disabled={
                                  !newCommentText[assignment.id]?.trim()
                                }
                              >
                                Post
                              </button>
                            </div>
                          </div>
                        )}

                        {openAttachments[assignment.id] && (
                          <div className="attachments-section">
                            <div className="attachments-list">
                              {loadingAttachments[assignment.id] ? (
                                <div className="attachments-loading">
                                  Loading attachments...
                                </div>
                              ) : (attachments[assignment.id] || []).length >
                                0 ? (
                                (attachments[assignment.id] || []).map(
                                  (attachment) => (
                                    <div
                                      key={attachment.id}
                                      className="attachment-item"
                                    >
                                      <div className="attachment-actions">
                                        <button
                                          className="attachment-link"
                                          onClick={() =>
                                            handleDownloadAttachment(
                                              assignment.id,
                                              attachment
                                            )
                                          }
                                          title={`Download ${attachment.fileName}`}
                                        >
                                          {attachment.fileName}
                                        </button>
                                        <button
                                          className="attachment-preview-button"
                                          onClick={() =>
                                            handlePreviewAttachment(
                                              assignment.id,
                                              attachment
                                            )
                                          }
                                          disabled={previewingAttachment}
                                          title={`Preview ${attachment.fileName}`}
                                        >
                                          {previewingAttachment
                                            ? "Opening..."
                                            : "Preview"}
                                        </button>
                                      </div>
                                      <span className="attachment-size">
                                        {Math.max(
                                          1,
                                          Math.round(
                                            attachment.sizeBytes / 1024
                                          )
                                        )}{" "}
                                        KB
                                      </span>
                                    </div>
                                  )
                                )
                              ) : (
                                <div className="attachments-empty">
                                  No attachments yet.
                                </div>
                              )}
                            </div>
                            <div className="attachment-upload-container">
                              <input
                                type="file"
                                className="attachment-upload-input"
                                onChange={(e) => {
                                  const file = e.target.files?.[0];
                                  if (file) {
                                    handleUploadAttachment(assignment.id, file);
                                    e.target.value = "";
                                  }
                                }}
                                disabled={uploadingAttachment[assignment.id]}
                              />
                              {uploadingAttachment[assignment.id] && (
                                <span className="attachment-uploading">
                                  Uploading...
                                </span>
                              )}
                            </div>
                          </div>
                        )}

                        {openSubtasks[assignment.id] && (
                          <div className="subtasks-section">
                            <div className="subtasks-list">
                              {loadingSubtasks[assignment.id] ? (
                                <div className="subtasks-loading">
                                  Loading subtasks...
                                </div>
                              ) : (
                                (() => {
                                  const assignmentSubtasks =
                                    subtasks[assignment.id] || [];

                                  return assignmentSubtasks.length > 0 ? (
                                    [...assignmentSubtasks]
                                      .sort(
                                        (a, b) => a.isCompleted - b.isCompleted
                                      )
                                      .map((subtask) => (
                                        <div
                                          key={subtask.id}
                                          className={`subtask-item ${draggedSubtaskId === subtask.id ? "dragging" : ""} ${dragOverSubtaskId === subtask.id ? "drag-over" : ""}`}
                                          draggable
                                          onDragStart={(e) =>
                                            handleDragStart(e, subtask.id)
                                          }
                                          onDragOver={(e) =>
                                            handleDragOver(e, subtask.id)
                                          }
                                          onDragLeave={handleDragLeave}
                                          onDrop={(e) =>
                                            handleDrop(
                                              e,
                                              subtask.id,
                                              assignment.id
                                            )
                                          }
                                          onDragEnd={handleDragEnd}
                                        >
                                          <div className="subtask-drag-handle">
                                            ⋮⋮
                                          </div>
                                          <div className="subtask-content">
                                            <div className="subtask-main">
                                              <label className="subtask-checkbox-label">
                                                <input
                                                  type="checkbox"
                                                  className="subtask-checkbox"
                                                  checked={subtask.isCompleted}
                                                  onChange={(e) =>
                                                    handleToggleSubtask(
                                                      subtask.id,
                                                      e.target.checked
                                                    )
                                                  }
                                                  onMouseDown={(e) =>
                                                    e.stopPropagation()
                                                  }
                                                />
                                                {editingSubtasks[subtask.id] ? (
                                                  <input
                                                    type="text"
                                                    className="subtask-title-input"
                                                    value={
                                                      subtaskTitleDrafts[
                                                        subtask.id
                                                      ] ?? ""
                                                    }
                                                    onChange={(e) =>
                                                      setSubtaskTitleDrafts(
                                                        (prev) => ({
                                                          ...prev,
                                                          [subtask.id]:
                                                            e.target.value
                                                        })
                                                      )
                                                    }
                                                    onKeyDown={(e) => {
                                                      if (e.key === "Enter") {
                                                        e.preventDefault();
                                                        handleSaveSubtaskTitle(
                                                          subtask.id
                                                        );
                                                      } else if (
                                                        e.key === "Escape"
                                                      ) {
                                                        e.preventDefault();
                                                        handleCancelSubtaskEdit(
                                                          subtask.id
                                                        );
                                                      }
                                                    }}
                                                    onMouseDown={(e) =>
                                                      e.stopPropagation()
                                                    }
                                                    onClick={(e) =>
                                                      e.stopPropagation()
                                                    }
                                                    autoFocus
                                                  />
                                                ) : (
                                                  <span
                                                    className={`subtask-title ${subtask.isCompleted ? "completed" : ""}`}
                                                  >
                                                    {subtask.title}
                                                  </span>
                                                )}
                                              </label>
                                              {editingSubtasks[subtask.id] ? (
                                                <>
                                                  <button
                                                    className="subtask-edit-save"
                                                    onMouseDown={(e) =>
                                                      e.stopPropagation()
                                                    }
                                                    onClick={() =>
                                                      handleSaveSubtaskTitle(
                                                        subtask.id
                                                      )
                                                    }
                                                    title="Save title"
                                                  >
                                                    Save
                                                  </button>
                                                  <button
                                                    className="subtask-edit-cancel"
                                                    onMouseDown={(e) =>
                                                      e.stopPropagation()
                                                    }
                                                    onClick={() =>
                                                      handleCancelSubtaskEdit(
                                                        subtask.id
                                                      )
                                                    }
                                                    title="Cancel edit"
                                                  >
                                                    Cancel
                                                  </button>
                                                </>
                                              ) : (
                                                <button
                                                  className="subtask-edit-button"
                                                  onMouseDown={(e) =>
                                                    e.stopPropagation()
                                                  }
                                                  onClick={() =>
                                                    handleStartSubtaskEdit(
                                                      subtask
                                                    )
                                                  }
                                                  title="Edit subtask"
                                                >
                                                  ✏️
                                                </button>
                                              )}
                                              <button
                                                className="subtask-note-button"
                                                onMouseDown={(e) =>
                                                  e.stopPropagation()
                                                }
                                                onClick={() => {
                                                  setEditingNotes((prev) => ({
                                                    ...prev,
                                                    [subtask.id]:
                                                      !prev[subtask.id]
                                                  }));
                                                  // Initialize note in state if not already set
                                                  if (
                                                    !subtaskNotes[subtask.id] &&
                                                    subtask.personalNote
                                                  ) {
                                                    setSubtaskNotes((prev) => ({
                                                      ...prev,
                                                      [subtask.id]:
                                                        subtask.personalNote
                                                    }));
                                                  }
                                                }}
                                                title={
                                                  subtaskNotes[subtask.id] ||
                                                  subtask.personalNote
                                                    ? "Edit note"
                                                    : "Add note"
                                                }
                                              >
                                                {subtaskNotes[subtask.id] ||
                                                subtask.personalNote
                                                  ? "📝"
                                                  : "📄"}
                                              </button>
                                              <button
                                                className="subtask-delete-button"
                                                onMouseDown={(e) =>
                                                  e.stopPropagation()
                                                }
                                                onClick={() => {
                                                  if (
                                                    window.confirm(
                                                      "Are you sure you want to delete this subtask?"
                                                    )
                                                  ) {
                                                    handleDeleteSubtask(
                                                      subtask.id,
                                                      assignment.id
                                                    );
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
                                                    subtaskNotes[subtask.id] ??
                                                    subtask.personalNote ??
                                                    ""
                                                  }
                                                  onChange={(e) =>
                                                    setSubtaskNotes((prev) => ({
                                                      ...prev,
                                                      [subtask.id]:
                                                        e.target.value
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
                                                      setEditingNotes(
                                                        (prev) => ({
                                                          ...prev,
                                                          [subtask.id]: false
                                                        })
                                                      );
                                                      // Restore original note
                                                      setSubtaskNotes(
                                                        (prev) => {
                                                          const updated = {
                                                            ...prev
                                                          };
                                                          if (
                                                            subtask.personalNote
                                                          ) {
                                                            updated[
                                                              subtask.id
                                                            ] =
                                                              subtask.personalNote;
                                                          } else {
                                                            delete updated[
                                                              subtask.id
                                                            ];
                                                          }
                                                          return updated;
                                                        }
                                                      );
                                                    }}
                                                  >
                                                    Cancel
                                                  </button>
                                                </div>
                                              </div>
                                            )}
                                            {!editingNotes[subtask.id] &&
                                              (subtaskNotes[subtask.id] ||
                                                subtask.personalNote) && (
                                                <div className="subtask-note-display">
                                                  <div className="subtask-note-header">
                                                    <span className="subtask-note-label">
                                                      Note:
                                                    </span>
                                                    <button
                                                      className="subtask-note-delete-button"
                                                      onClick={() =>
                                                        handleDeleteSubtaskNote(
                                                          subtask.id
                                                        )
                                                      }
                                                      title="Delete personal note"
                                                    >
                                                      🗑️
                                                    </button>
                                                  </div>
                                                  <span className="subtask-note-text">
                                                    {subtaskNotes[subtask.id] ||
                                                      subtask.personalNote}
                                                  </span>
                                                </div>
                                              )}
                                          </div>
                                        </div>
                                      ))
                                  ) : (
                                    <div className="subtasks-empty">
                                      No subtasks yet. Add one below!
                                    </div>
                                  );
                                })()
                              )}
                            </div>
                            <div className="subtask-input-container">
                              <input
                                type="text"
                                className="subtask-input"
                                placeholder="Add a subtask..."
                                value={newSubtaskText[assignment.id] || ""}
                                onChange={(e) =>
                                  setNewSubtaskText((prev) => ({
                                    ...prev,
                                    [assignment.id]: e.target.value
                                  }))
                                }
                                onKeyPress={(e) => {
                                  if (e.key === "Enter" && !e.shiftKey) {
                                    e.preventDefault();
                                    handleAddSubtask(assignment.id);
                                  }
                                }}
                              />
                              <button
                                className="subtask-submit-button"
                                onClick={() => handleAddSubtask(assignment.id)}
                                disabled={
                                  !newSubtaskText[assignment.id]?.trim()
                                }
                              >
                                Add
                              </button>
                            </div>
                          </div>
                        )}
                      </div>
                    ))}
                </div>
              );
            })()
          )}
        </div>
      )}
      {attachmentPreview && (
        <div className="attachment-preview-overlay" onClick={closeAttachmentPreview}>
          <div
            className="attachment-preview-modal"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="attachment-preview-header">
              <h3 className="attachment-preview-title">
                Preview: {attachmentPreview.fileName}
              </h3>
              <button
                className="attachment-preview-close"
                onClick={closeAttachmentPreview}
              >
                Close
              </button>
            </div>
            <div className="attachment-preview-content">
              {attachmentPreview.kind === "image" && (
                <img
                  src={attachmentPreview.objectUrl}
                  alt={attachmentPreview.fileName}
                  className="attachment-preview-image"
                />
              )}
              {attachmentPreview.kind === "pdf" && (
                <iframe
                  src={attachmentPreview.objectUrl}
                  title={attachmentPreview.fileName}
                  className="attachment-preview-pdf"
                />
              )}
              {attachmentPreview.kind === "text" && (
                <pre className="attachment-preview-text">
                  {attachmentPreview.text}
                </pre>
              )}
              {attachmentPreview.kind === "docx" && (
                <div
                  className="attachment-preview-docx"
                  dangerouslySetInnerHTML={{ __html: attachmentPreview.html }}
                />
              )}
              {attachmentPreview.kind === "unsupported" && (
                <div className="attachment-preview-unsupported">
                  {attachmentPreview.error ||
                    "Preview is not available for this file type."}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
