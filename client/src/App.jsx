import React, { useCallback, useEffect, useRef, useState } from "react";
import "./App.css";
import AssignmentCard from "./components/AssignmentCard";
import WorkingOnSection from "./components/WorkingOnSection";
import QuickTasksSection from "./components/QuickTasksSection";

const ASSIGNMENTS_API_URL = "http://localhost:5000/api/assignments";
const LAST_SEEN_COMMENT_COUNTS_KEY = "lastSeenCommentCounts";
const SHOW_ONLY_UNREAD_ASSIGNMENTS_KEY = "showOnlyUnreadAssignments";
const SORT_UNREAD_TO_TOP_KEY = "sortUnreadToTop";
const COMMENT_SORT_NEWEST_FIRST_KEY = "commentSortNewestFirst";
const AUTO_REFRESH_INTERVAL_SECONDS_KEY = "assignmentAutoRefreshSeconds";
const SHOW_ALL_ASSIGNMENTS_KEY = "showAllAssignments";
const AI_SUMMARY_STATE_KEY = "aiSummaryStateByAssignment";
const SCOPE_MINE = "mine";
const SCOPE_ALL = "all";
const DETAIL_TABS = {
  COMMENTS: "comments",
  SUBTASKS: "subtasks",
  ATTACHMENTS: "attachments"
};

function App() {
  const [assignments, setAssignments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [expandedByAssignment, setExpandedByAssignment] = useState({});
  const [activeDetailTabByAssignment, setActiveDetailTabByAssignment] = useState(
    {}
  );
  const [comments, setComments] = useState({});
  const [loadingComments, setLoadingComments] = useState({});
  const [attachments, setAttachments] = useState({});
  const [attachmentCountsByScope, setAttachmentCountsByScope] = useState({
    [SCOPE_MINE]: {},
    [SCOPE_ALL]: {}
  });
  const [loadingAttachments, setLoadingAttachments] = useState({});
  const [uploadingAttachment, setUploadingAttachment] = useState({});
  const [previewingAttachment, setPreviewingAttachment] = useState(false);
  const [attachmentPreview, setAttachmentPreview] = useState(null);
  const [newCommentText, setNewCommentText] = useState({});
  const [currentUser, setCurrentUser] = useState(null);
  const [commentCountsByScope, setCommentCountsByScope] = useState({
    [SCOPE_MINE]: {},
    [SCOPE_ALL]: {}
  });
  const [commentFilters, setCommentFilters] = useState({});
  const [lastSeenCommentCounts, setLastSeenCommentCounts] = useState(() => {
    try {
      const raw = localStorage.getItem(LAST_SEEN_COMMENT_COUNTS_KEY);
      return raw ? JSON.parse(raw) : {};
    } catch {
      return {};
    }
  });
  const [subtasks, setSubtasks] = useState({});
  const [loadingSubtasks, setLoadingSubtasks] = useState({});
  const [newSubtaskText, setNewSubtaskText] = useState({});
  const [editingNotes, setEditingNotes] = useState({});
  const [subtaskNotes, setSubtaskNotes] = useState({});
  const [editingSubtasks, setEditingSubtasks] = useState({});
  const [subtaskTitleDrafts, setSubtaskTitleDrafts] = useState({});
  const [editingCommentNotes, setEditingCommentNotes] = useState({});
  const [commentNotes, setCommentNotes] = useState({});
  const [commentNoteTimestamps, setCommentNoteTimestamps] = useState({});
  const [subtaskNoteTimestamps, setSubtaskNoteTimestamps] = useState({});
  const [commentFlags, setCommentFlags] = useState({});
  const [commentChecklistSummaries, setCommentChecklistSummaries] = useState({});
  const [aiSummaryStateByAssignment, setAiSummaryStateByAssignment] = useState(
    () => {
      try {
        const raw = localStorage.getItem(AI_SUMMARY_STATE_KEY);
        return raw ? JSON.parse(raw) : {};
      } catch {
        return {};
      }
    }
  );
  const [draggedSubtaskId, setDraggedSubtaskId] = useState(null);
  const [dragOverSubtaskId, setDragOverSubtaskId] = useState(null);
  const [workingOn, setWorkingOn] = useState(new Set()); // assignmentIds that are marked as "working on"
  const [allAssignmentsCollapsed, setAllAssignmentsCollapsed] = useState(false);
  const [assignmentSearch, setAssignmentSearch] = useState("");
  const [showAllAssignments, setShowAllAssignments] = useState(() => {
    try {
      return localStorage.getItem(SHOW_ALL_ASSIGNMENTS_KEY) === "true";
    } catch {
      return false;
    }
  });
  const [assigneeFilter, setAssigneeFilter] = useState("");
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
  const [assignmentControlsOffset, setAssignmentControlsOffset] = useState(96);
  const assignmentControlsRef = useRef(null);
  const commentCountsRequestInFlightRef = useRef({
    [SCOPE_MINE]: false,
    [SCOPE_ALL]: false
  });
  const attachmentCountsRequestInFlightRef = useRef({
    [SCOPE_MINE]: false,
    [SCOPE_ALL]: false
  });
  const currentScopeKey = showAllAssignments ? SCOPE_ALL : SCOPE_MINE;
  const commentCounts = commentCountsByScope[currentScopeKey] || {};
  const attachmentCounts = attachmentCountsByScope[currentScopeKey] || {};

  const refreshData = useCallback(async (isInitialLoad = false) => {
    try {
      const assignmentScopeQuery = showAllAssignments ? "?all=true" : "";
      const scopeKey = showAllAssignments ? SCOPE_ALL : SCOPE_MINE;
      const assignmentsRes = await fetch(
        `${ASSIGNMENTS_API_URL}${assignmentScopeQuery}`
      );

      if (!assignmentsRes.ok) throw new Error("Failed to fetch assignments");
      const assignmentsData = await assignmentsRes.json();
      setAssignments(assignmentsData);

      // Do not block assignment render on user/working-on lookups.
      fetch("http://localhost:5000/api/user/current")
        .then((res) => (res.ok ? res.json() : null))
        .then((userData) => {
          if (userData?.userName) {
            setCurrentUser(userData.userName);
          }
        })
        .catch((err) =>
          console.error("Error refreshing current user:", err)
        );

      // Keep assignment loading fast; hydrate comment counts asynchronously.
      if (!commentCountsRequestInFlightRef.current[scopeKey]) {
        commentCountsRequestInFlightRef.current[scopeKey] = true;
        fetch(
          `http://localhost:5000/api/assignments/comments/counts${assignmentScopeQuery}`
        )
          .then((res) => (res.ok ? res.json() : null))
          .then((counts) => {
            if (counts) {
              setCommentCountsByScope((prev) => ({
                ...prev,
                [scopeKey]: counts
              }));
            }
          })
          .catch((err) =>
            console.error("Error refreshing assignment comment counts:", err)
          )
          .finally(() => {
            commentCountsRequestInFlightRef.current[scopeKey] = false;
          });
      }

      // Load attachment counts in the background so badges are visible
      // without opening every attachment panel.
      if (!attachmentCountsRequestInFlightRef.current[scopeKey]) {
        attachmentCountsRequestInFlightRef.current[scopeKey] = true;
        fetch(
          `http://localhost:5000/api/assignments/attachments/counts${assignmentScopeQuery}`
        )
          .then((res) => (res.ok ? res.json() : null))
          .then((counts) => {
            if (counts) {
              setAttachmentCountsByScope((prev) => ({
                ...prev,
                [scopeKey]: counts
              }));
            }
          })
          .catch((err) =>
            console.error("Error refreshing attachment counts:", err)
          )
          .finally(() => {
            attachmentCountsRequestInFlightRef.current[scopeKey] = false;
          });
      }

      fetch("http://localhost:5000/api/assignments/working-on")
        .then((res) => (res.ok ? res.json() : null))
        .then((workingOnData) => {
          if (Array.isArray(workingOnData)) {
            setWorkingOn(new Set(workingOnData));
          }
        })
        .catch((err) =>
          console.error("Error refreshing working-on assignments:", err)
        );

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
  }, [showAllAssignments]);

  useEffect(() => {
    setAssigneeFilter("");
  }, [showAllAssignments]);

  useEffect(() => {
    try {
      localStorage.setItem(
        SHOW_ALL_ASSIGNMENTS_KEY,
        String(showAllAssignments)
      );
    } catch {
      // ignore storage write failures
    }
  }, [showAllAssignments]);

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
    try {
      localStorage.setItem(
        AI_SUMMARY_STATE_KEY,
        JSON.stringify(aiSummaryStateByAssignment)
      );
    } catch {
      // ignore storage write failures
    }
  }, [aiSummaryStateByAssignment]);

  useEffect(() => {
    if (!autoRefreshIntervalSeconds) return undefined;

    const intervalId = setInterval(() => {
      refreshData(false);
    }, autoRefreshIntervalSeconds * 1000);

    return () => clearInterval(intervalId);
  }, [autoRefreshIntervalSeconds, refreshData]);

  useEffect(() => {
    const controlsEl = assignmentControlsRef.current;
    if (!controlsEl) return undefined;

    const cssLengthToPx = (rawValue) => {
      const value = (rawValue || "").trim();
      if (!value) return 0;
      if (value.endsWith("px")) return Number.parseFloat(value) || 0;
      if (value.endsWith("rem")) {
        const rootFontSize =
          Number.parseFloat(
            window.getComputedStyle(document.documentElement).fontSize || "16"
          ) || 16;
        return (Number.parseFloat(value) || 0) * rootFontSize;
      }
      return Number.parseFloat(value) || 0;
    };

    const updateOffset = () => {
      const styles = window.getComputedStyle(controlsEl);
      const stickyTop = cssLengthToPx(styles.top);
      const marginBottom = cssLengthToPx(styles.marginBottom);
      const measured = Math.ceil(
        controlsEl.getBoundingClientRect().height + stickyTop + marginBottom + 20
      );
      setAssignmentControlsOffset(Math.max(measured, 70));
    };

    updateOffset();

    const observer = new ResizeObserver(() => {
      updateOffset();
    });
    observer.observe(controlsEl);
    window.addEventListener("resize", updateOffset);

    return () => {
      observer.disconnect();
      window.removeEventListener("resize", updateOffset);
    };
  }, []);

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

  const markCommentsAsSeen = (assignmentId) => {
    setLastSeenCommentCounts((prev) => ({
      ...prev,
      [assignmentId]:
        commentCounts[assignmentId] ?? comments[assignmentId]?.length ?? 0
    }));
  };

  const loadCommentsIfNeeded = async (assignmentId) => {
    if (comments[assignmentId]) return;

    setLoadingComments((prev) => ({ ...prev, [assignmentId]: true }));
    try {
      const response = await fetch(
        `${ASSIGNMENTS_API_URL}/${assignmentId}/comments`
      );
      if (!response.ok) throw new Error("Failed to fetch comments");
      const data = await response.json();
      setComments((prev) => ({ ...prev, [assignmentId]: data }));
      // Update comment count if it's different
      setCommentCountsByScope((prev) => ({
        ...prev,
        [currentScopeKey]: {
          ...(prev[currentScopeKey] || {}),
          [assignmentId]: data.length
        }
      }));
      // Load comment notes and flags for all comments
      const notesPromises = data.map((comment) =>
        fetch(
          `http://localhost:5000/api/assignments/${assignmentId}/comments/${comment.id}/note`
        )
          .then((res) => (res.ok ? res.json() : null))
          .then((noteData) => ({
            commentId: comment.id,
            noteKey: getCommentNoteKey(assignmentId, comment.id),
            noteData
          }))
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
        const noteTimestampState = {};
        const flagsState = {};
        results.forEach((result) => {
          if (result) {
            if ("noteData" in result) {
              if (result.noteData?.note) {
                notesState[result.noteKey] = result.noteData.note;
              }
              if (result.noteData?.createdDate || result.noteData?.updatedDate) {
                noteTimestampState[result.noteKey] = {
                  createdDate: result.noteData.createdDate || null,
                  updatedDate: result.noteData.updatedDate || null
                };
              }
            } else if ("isFlagged" in result) {
              flagsState[result.id] = result.isFlagged;
            }
          }
        });
        setCommentNotes((prev) => ({ ...prev, ...notesState }));
        setCommentNoteTimestamps((prev) => ({
          ...prev,
          ...noteTimestampState
        }));
        setCommentFlags((prev) => ({ ...prev, ...flagsState }));
      });
    } catch (err) {
      setError(err.toString());
    } finally {
      setLoadingComments((prev) => ({ ...prev, [assignmentId]: false }));
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
      setCommentCountsByScope((prev) => ({
        ...prev,
        [currentScopeKey]: {
          ...(prev[currentScopeKey] || {}),
          [assignmentId]: nextCount
        }
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

  const loadAttachmentsIfNeeded = async (assignmentId) => {
    if (attachments[assignmentId]) return;

    setLoadingAttachments((prev) => ({ ...prev, [assignmentId]: true }));
    try {
      const response = await fetch(
        `${ASSIGNMENTS_API_URL}/${assignmentId}/attachments`
      );
      if (!response.ok) throw new Error("Failed to fetch attachments");
      const data = await response.json();
      setAttachments((prev) => ({ ...prev, [assignmentId]: data }));
      setAttachmentCountsByScope((prev) => ({
        ...prev,
        [currentScopeKey]: {
          ...(prev[currentScopeKey] || {}),
          [assignmentId]: data.length
        }
      }));
    } catch (err) {
      setError(err.toString());
    } finally {
      setLoadingAttachments((prev) => ({ ...prev, [assignmentId]: false }));
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
      setAttachmentCountsByScope((prev) => ({
        ...prev,
        [currentScopeKey]: {
          ...(prev[currentScopeKey] || {}),
          [assignmentId]:
            ((prev[currentScopeKey] || {})[assignmentId] ?? 0) + 1
        }
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
    if (
      ext === "xlsx" ||
      normalizedType ===
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    ) {
      return "xlsx";
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
          previewNote: "",
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
          previewNote: "",
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
          previewNote: "",
          error: null
        });
        return;
      }

      if (kind === "xlsx") {
        const arrayBuffer = await blob.arrayBuffer();
        const xlsxModule = await import("xlsx");
        const xlsxApi = xlsxModule.default ?? xlsxModule;
        const workbook = xlsxApi.read(arrayBuffer, { type: "array" });
        const firstSheetName = workbook.SheetNames?.[0];

        if (!firstSheetName) {
          throw new Error("Workbook has no sheets");
        }

        const firstSheet = workbook.Sheets[firstSheetName];
        if (!firstSheet) {
          throw new Error("Unable to read first sheet");
        }

        let previewNote = `Sheet: ${firstSheetName}`;
        if (firstSheet["!ref"]) {
          const range = xlsxApi.utils.decode_range(firstSheet["!ref"]);
          const maxPreviewRows = 100;
          const rowCount = range.e.r - range.s.r + 1;
          if (rowCount > maxPreviewRows) {
            range.e.r = range.s.r + maxPreviewRows - 1;
            firstSheet["!ref"] = xlsxApi.utils.encode_range(range);
            previewNote += ` (showing first ${maxPreviewRows} rows)`;
          }
        }

        const html = xlsxApi.utils.sheet_to_html(firstSheet);
        setAttachmentPreview({
          fileName: attachment.fileName,
          kind,
          contentType,
          objectUrl: "",
          text: "",
          html,
          previewNote,
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
        previewNote: "",
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
        previewNote: "",
        error: `Preview failed: ${err.toString()}`
      });
    } finally {
      setPreviewingAttachment(false);
    }
  };

  const loadSubtasksIfNeeded = async (assignmentId) => {
    if (subtasks[assignmentId]) return;

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
      const noteSubtasks = data.filter((subtask) => subtask.personalNote);
      if (noteSubtasks.length > 0) {
        Promise.all(
          noteSubtasks.map((subtask) =>
            fetch(`http://localhost:5000/api/subtasks/${subtask.id}/note`)
              .then((res) => (res.ok ? res.json() : null))
              .then((noteData) => ({ subtaskId: subtask.id, noteData }))
              .catch(() => null)
          )
        ).then((noteResults) => {
          const nextTimestamps = {};
          noteResults.forEach((result) => {
            if (result?.noteData?.createdDate || result?.noteData?.updatedDate) {
              nextTimestamps[result.subtaskId] = {
                createdDate: result.noteData.createdDate || null,
                updatedDate: result.noteData.updatedDate || null
              };
            }
          });
          if (Object.keys(nextTimestamps).length > 0) {
            setSubtaskNoteTimestamps((prev) => ({
              ...prev,
              ...nextTimestamps
            }));
          }
        });
      }
    } catch (err) {
      setError(err.toString());
    } finally {
      setLoadingSubtasks((prev) => ({ ...prev, [assignmentId]: false }));
    }
  };

  const ensureDetailTabData = async (assignmentId, tab) => {
    if (tab === DETAIL_TABS.COMMENTS) {
      await loadCommentsIfNeeded(assignmentId);
      return;
    }
    if (tab === DETAIL_TABS.SUBTASKS) {
      await loadSubtasksIfNeeded(assignmentId);
      return;
    }
    if (tab === DETAIL_TABS.ATTACHMENTS) {
      await loadAttachmentsIfNeeded(assignmentId);
    }
  };

  const openDetailTab = async (assignmentId, tab) => {
    const isExpanded = Boolean(expandedByAssignment[assignmentId]);
    const previousTab =
      activeDetailTabByAssignment[assignmentId] || DETAIL_TABS.COMMENTS;

    if (
      isExpanded &&
      previousTab === DETAIL_TABS.COMMENTS &&
      tab !== DETAIL_TABS.COMMENTS
    ) {
      // Preserve existing unread behavior when leaving comments.
      markCommentsAsSeen(assignmentId);
    }

    setExpandedByAssignment((prev) => ({
      ...prev,
      [assignmentId]: true
    }));
    setActiveDetailTabByAssignment((prev) => ({
      ...prev,
      [assignmentId]: tab
    }));
    await ensureDetailTabData(assignmentId, tab);
  };

  const toggleAssignmentDetails = async (assignmentId) => {
    const isExpanded = Boolean(expandedByAssignment[assignmentId]);
    const activeTab =
      activeDetailTabByAssignment[assignmentId] || DETAIL_TABS.COMMENTS;

    if (isExpanded) {
      if (activeTab === DETAIL_TABS.COMMENTS) {
        // Closing comments marks current comments as seen.
        markCommentsAsSeen(assignmentId);
      }
      setExpandedByAssignment((prev) => ({
        ...prev,
        [assignmentId]: false
      }));
      return;
    }

    await openDetailTab(assignmentId, activeTab);
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
            const noteSubtasks = data.filter((subtask) => subtask.personalNote);
            if (noteSubtasks.length > 0) {
              Promise.all(
                noteSubtasks.map((subtask) =>
                  fetch(`http://localhost:5000/api/subtasks/${subtask.id}/note`)
                    .then((res) => (res.ok ? res.json() : null))
                    .then((noteData) => ({ subtaskId: subtask.id, noteData }))
                    .catch(() => null)
                )
              ).then((noteResults) => {
                const nextTimestamps = {};
                noteResults.forEach((result) => {
                  if (
                    result?.noteData?.createdDate ||
                    result?.noteData?.updatedDate
                  ) {
                    nextTimestamps[result.subtaskId] = {
                      createdDate: result.noteData.createdDate || null,
                      updatedDate: result.noteData.updatedDate || null
                    };
                  }
                });
                if (Object.keys(nextTimestamps).length > 0) {
                  setSubtaskNoteTimestamps((prev) => ({
                    ...prev,
                    ...nextTimestamps
                  }));
                }
              });
            }
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
      setSubtaskNoteTimestamps((prev) => {
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
        const noteSubtasks = data.filter((subtask) => subtask.personalNote);
        if (noteSubtasks.length > 0) {
          Promise.all(
            noteSubtasks.map((subtask) =>
              fetch(`http://localhost:5000/api/subtasks/${subtask.id}/note`)
                .then((res) => (res.ok ? res.json() : null))
                .then((noteData) => ({ subtaskId: subtask.id, noteData }))
                .catch(() => null)
            )
          ).then((noteResults) => {
            const nextTimestamps = {};
            noteResults.forEach((result) => {
              if (result?.noteData?.createdDate || result?.noteData?.updatedDate) {
                nextTimestamps[result.subtaskId] = {
                  createdDate: result.noteData.createdDate || null,
                  updatedDate: result.noteData.updatedDate || null
                };
              }
            });
            if (Object.keys(nextTimestamps).length > 0) {
              setSubtaskNoteTimestamps((prev) => ({
                ...prev,
                ...nextTimestamps
              }));
            }
          });
        }
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
      const noteResponse = await fetch(
        `http://localhost:5000/api/subtasks/${subtaskId}/note`
      );
      if (noteResponse.ok) {
        const noteData = await noteResponse.json();
        setSubtaskNoteTimestamps((prev) => ({
          ...prev,
          [subtaskId]:
            noteData?.createdDate || noteData?.updatedDate
              ? {
                  createdDate: noteData.createdDate || null,
                  updatedDate: noteData.updatedDate || null
                }
              : null
        }));
      }
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

  const getCommentNoteKey = (assignmentId, commentId) =>
    `${assignmentId}:${commentId}`;

  const handleUpdateCommentNote = async (assignmentId, commentId, note) => {
    try {
      const response = await fetch(
        `http://localhost:5000/api/assignments/${assignmentId}/comments/${commentId}/note`,
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

      const noteData = await response.json();
      const noteKey = getCommentNoteKey(assignmentId, commentId);
      setCommentNotes((prev) => ({
        ...prev,
        [noteKey]: noteData?.note || null
      }));
      setCommentNoteTimestamps((prev) => ({
        ...prev,
        [noteKey]:
          noteData?.createdDate || noteData?.updatedDate
            ? {
                createdDate: noteData.createdDate || null,
                updatedDate: noteData.updatedDate || null
              }
            : null
      }));
      setEditingCommentNotes((prev) => ({
        ...prev,
        [noteKey]: false
      }));
    } catch (err) {
      setError(err.toString());
    }
  };

  const handleDeleteCommentNote = async (assignmentId, commentId) => {
    if (window.confirm("Are you sure you want to delete this personal note?")) {
      await handleUpdateCommentNote(assignmentId, commentId, null);
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

  const getAssigneeNames = (assignment) => {
    const raw = assignment?.assignedTo?.trim();
    if (!raw) return ["Unknown"];

    const names = raw
      .split(/[;,|]/)
      .map((name) => name.trim())
      .filter(Boolean);

    return names.length > 0 ? names : ["Unknown"];
  };

  const availableAssignees = [
    ...new Set(assignments.flatMap((assignment) => getAssigneeNames(assignment)))
  ].sort((a, b) => a.localeCompare(b));

  const normalizedSearchForMeta = assignmentSearch.trim().toLowerCase();
  const assignmentsAfterSearch = normalizedSearchForMeta
    ? assignments.filter((assignment) =>
        [
          assignment.title,
          assignment.description,
          getAssigneeNames(assignment).join(", "),
          assignment.assigneeName
        ]
          .filter(Boolean)
          .some((value) => value.toLowerCase().includes(normalizedSearchForMeta))
      )
    : assignments;
  const assignmentsAfterAssignee =
    showAllAssignments && assigneeFilter
      ? assignmentsAfterSearch.filter((assignment) =>
          getAssigneeNames(assignment).some((name) => name === assigneeFilter)
        )
      : assignmentsAfterSearch;
  const assignmentsInCurrentView = showOnlyUnreadAssignments
    ? assignmentsAfterAssignee.filter((assignment) => hasUnreadComments(assignment.id))
    : assignmentsAfterAssignee;
  const hotZoneCountInCurrentView = assignmentsInCurrentView.filter((a) =>
    workingOn.has(a.id)
  ).length;
  const handleClearAssignmentSearch = (event) => {
    event.preventDefault();
    event.stopPropagation();
    setAssignmentSearch("");
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
        <div
          className="assignments-container"
          style={{ "--assignment-controls-offset": `${assignmentControlsOffset}px` }}
        >
          <QuickTasksSection />
          <div className="assignment-controls" ref={assignmentControlsRef}>
            <div className="assignment-search-container">
              <input
                type="text"
                className="assignment-search-input"
                placeholder="Search assignments..."
                value={assignmentSearch}
                onChange={(e) => setAssignmentSearch(e.target.value)}
              />
              {assignmentSearch && (
                <button
                  type="button"
                  className="assignment-search-clear"
                  onMouseDown={handleClearAssignmentSearch}
                  onClick={handleClearAssignmentSearch}
                  aria-label="Clear assignment search"
                  title="Clear search"
                >
                  ×
                </button>
              )}
            </div>
            <div className="assignment-filter-controls">
              <button
                className={`assignment-filter-toggle ${showAllAssignments ? "active" : ""}`}
                onClick={() => setShowAllAssignments(!showAllAssignments)}
                title={
                  showAllAssignments
                    ? "Showing all assignments across the team"
                    : "Showing only your assignments"
                }
              >
                {showAllAssignments ? "View: Team" : "View: Mine"}
              </button>
              {showAllAssignments && (
                <select
                  className="assignment-assignee-filter"
                  value={assigneeFilter}
                  onChange={(e) => setAssigneeFilter(e.target.value)}
                  title="Filter assignments by assignee"
                >
                  <option value="">Assignee: All</option>
                  {availableAssignees.map((assignee) => (
                    <option key={assignee} value={assignee}>
                      {assignee}
                    </option>
                  ))}
                </select>
              )}
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
            <div className="assignment-view-meta">
              {showAllAssignments ? "Team" : "Mine"} assignments:{" "}
              {assignmentsInCurrentView.length} total ({hotZoneCountInCurrentView}{" "}
              in Hot Zone)
            </div>
          </div>
          {assignments.length === 0 ? (
            <div className="empty-state">
              <p>No assignments found</p>
            </div>
          ) : (
            (() => {
              const getAssigneeLabel = (assignment) =>
                getAssigneeNames(assignment).join(", ");
              const normalizedSearch = assignmentSearch.trim().toLowerCase();
              const filteredAssignments = normalizedSearch
                ? assignments.filter((assignment) =>
                    [
                      assignment.title,
                      assignment.description,
                      getAssigneeLabel(assignment),
                      assignment.assigneeName
                    ]
                      .filter(Boolean)
                      .some((value) =>
                        value.toLowerCase().includes(normalizedSearch)
                      )
                  )
                : assignments;
              const assigneeFilteredAssignments =
                showAllAssignments && assigneeFilter
                  ? filteredAssignments.filter(
                      (assignment) =>
                        getAssigneeNames(assignment).some(
                          (name) => name === assigneeFilter
                        )
                    )
                  : filteredAssignments;

              if (assigneeFilteredAssignments.length === 0) {
                return (
                  <div className="empty-state">
                    <p>
                      No assignments match the current filters.
                      {assignmentSearch
                        ? ` Search term: "${assignmentSearch}".`
                        : ""}
                      {assigneeFilter ? ` Assignee: ${assigneeFilter}.` : ""}
                    </p>
                  </div>
                );
              }

              const unreadFilteredAssignments = showOnlyUnreadAssignments
                ? assigneeFilteredAssignments.filter((a) =>
                    hasUnreadComments(a.id)
                  )
                : assigneeFilteredAssignments;

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

              const sharedAssignmentCardProps = {
                hasUnreadComments,
                formatDate,
                isOverdue,
                getStatusColor,
                getStatusLabel,
                handleToggleWorkingOn,
                handleCompleteAssignment,
                commentCounts,
                expandedByAssignment,
                activeDetailTabByAssignment,
                openDetailTab,
                toggleAssignmentDetails,
                subtasks,
                attachmentCounts,
                attachments,
                comments,
                loadingComments,
                commentFilters,
                setCommentFilters,
                commentSortNewestFirst,
                setCommentSortNewestFirst,
                commentFlags,
                commentNotes,
                commentNoteTimestamps,
                editingCommentNotes,
                setEditingCommentNotes,
                setCommentNotes,
                setCommentNoteTimestamps,
                sortCommentsByDate,
                currentUser,
                getUserColor,
                formatCommentDate,
                handleToggleCommentFlag,
                handleUpdateCommentNote,
                handleDeleteCommentNote,
                aiSummaryStateByAssignment,
                setAiSummaryStateByAssignment,
                commentChecklistSummaries,
                setCommentChecklistSummaries,
                newCommentText,
                setNewCommentText,
                handleAddComment,
                loadingAttachments,
                uploadingAttachment,
                previewingAttachment,
                handleDownloadAttachment,
                handlePreviewAttachment,
                handleUploadAttachment,
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
              };

              return (
                <div className="assignments-list">
                  <WorkingOnSection
                    assignments={workingOnAssignments}
                    renderCard={(assignment) => (
                      <AssignmentCard
                        key={assignment.id}
                        assignment={assignment}
                        isWorkingOnCard
                        {...sharedAssignmentCardProps}
                      />
                    )}
                  />
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
                      <AssignmentCard
                        key={assignment.id}
                        assignment={assignment}
                        isWorkingOnCard={false}
                        {...sharedAssignmentCardProps}
                      />
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
              {attachmentPreview.kind === "xlsx" && (
                <div className="attachment-preview-xlsx">
                  {attachmentPreview.previewNote && (
                    <div className="attachment-preview-note">
                      {attachmentPreview.previewNote}
                    </div>
                  )}
                  <div
                    className="attachment-preview-xlsx-table"
                    dangerouslySetInnerHTML={{ __html: attachmentPreview.html }}
                  />
                </div>
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
