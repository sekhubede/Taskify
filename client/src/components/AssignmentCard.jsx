import React from "react";
import AttachmentsSection from "./AttachmentsSection";
import CommentsSection from "./CommentsSection";
import SubtasksSection from "./SubtasksSection";

function AssignmentCard({
  assignment,
  isWorkingOnCard,
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
}) {
  const DETAIL_TABS = {
    COMMENTS: "comments",
    SUBTASKS: "subtasks",
    ATTACHMENTS: "attachments"
  };
  const unread = hasUnreadComments(assignment.id);
  const assignmentSubtasks = subtasks[assignment.id] || assignment.subtasks || [];
  const completedCount = assignmentSubtasks.filter((s) => s.isCompleted).length;
  const checklistSummary = commentChecklistSummaries?.[assignment.id] || {
    total: 0,
    completed: 0
  };
  const commentCountDisplay =
    commentCounts[assignment.id] ?? comments?.[assignment.id]?.length ?? "…";
  const attachmentCountDisplay =
    attachmentCounts[assignment.id] ??
    attachments?.[assignment.id]?.length ??
    "…";
  const assignedToLabel =
    assignment.assignedTo && assignment.assignedTo.trim().length > 0
      ? assignment.assignedTo
      : "Unknown";
  const hasExpandedDetails = Boolean(expandedByAssignment[assignment.id]);
  const activeDetailTab =
    activeDetailTabByAssignment[assignment.id] || DETAIL_TABS.COMMENTS;
  const isCommentsTabActive =
    hasExpandedDetails && activeDetailTab === DETAIL_TABS.COMMENTS;
  const isSubtasksTabActive =
    hasExpandedDetails && activeDetailTab === DETAIL_TABS.SUBTASKS;
  const isAttachmentsTabActive =
    hasExpandedDetails && activeDetailTab === DETAIL_TABS.ATTACHMENTS;
  const detailsPanelId = `assignment-details-${assignment.id}`;
  const tabs = [
    {
      id: DETAIL_TABS.COMMENTS,
      label: "Comments",
      count: commentCountDisplay,
      prefix: "💬",
      showUnreadIndicator: unread
    },
    {
      id: DETAIL_TABS.SUBTASKS,
      label: "Subtasks",
      count: `${completedCount}/${assignmentSubtasks.length}`,
      prefix: "✓"
    },
    {
      id: DETAIL_TABS.ATTACHMENTS,
      label: "Attachments",
      count: attachmentCountDisplay,
      prefix: "📎"
    }
  ];

  const focusTabByIndex = (index) => {
    const button = document.getElementById(`assignment-tab-${assignment.id}-${index}`);
    if (button) {
      button.focus();
    }
  };

  const handleTabKeyDown = (event, tabIndex) => {
    if (tabs.length === 0) return;

    if (event.key === "ArrowRight") {
      event.preventDefault();
      focusTabByIndex((tabIndex + 1) % tabs.length);
      return;
    }
    if (event.key === "ArrowLeft") {
      event.preventDefault();
      focusTabByIndex((tabIndex - 1 + tabs.length) % tabs.length);
      return;
    }
    if (event.key === "Home") {
      event.preventDefault();
      focusTabByIndex(0);
      return;
    }
    if (event.key === "End") {
      event.preventDefault();
      focusTabByIndex(tabs.length - 1);
      return;
    }
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      const tab = tabs[tabIndex];
      if (tab) {
        openDetailTab(assignment.id, tab.id);
      }
    }
  };

  return (
    <div
      className={`assignment-card ${isWorkingOnCard ? "working-on" : ""} ${unread ? "has-unread-comments" : ""}`}
    >
      <div
        className={`assignment-context-header ${hasExpandedDetails ? "sticky" : ""}`}
      >
        <div className="assignment-header">
          <div className="assignment-title-row">
            <button
              className={`working-on-toggle ${isWorkingOnCard ? "active" : ""}`}
              onClick={() => handleToggleWorkingOn(assignment.id, !isWorkingOnCard)}
              title={isWorkingOnCard ? "Remove from Hot Zone" : "Add to Hot Zone"}
            >
              {isWorkingOnCard ? "🔥" : "⭐"}
            </button>
            <h2 className="assignment-title">{assignment.title}</h2>
            {unread && (
              <button
                type="button"
                className="assignment-unread-comments-badge"
                onClick={() => openDetailTab(assignment.id, DETAIL_TABS.COMMENTS)}
              >
                New comments
              </button>
            )}
          </div>
          <span
            className="status-badge"
            style={{ backgroundColor: getStatusColor(assignment.status) }}
          >
            {getStatusLabel(assignment.status)}
          </span>
        </div>
        <p
          className="assignment-assignee-line"
          title={`Assigned to: ${assignedToLabel}`}
        >
          Assigned to: {assignedToLabel}
        </p>
      </div>

      {assignment.description && (
        <p className="assignment-description">{assignment.description}</p>
      )}

      <div
        className={`assignment-meta ${hasExpandedDetails ? "sticky-actions" : ""}`}
      >
        {assignment.dueDate && (
          <span
            className={`due-date ${isOverdue(assignment.dueDate, assignment.status) ? "overdue" : ""}`}
          >
            {isOverdue(assignment.dueDate, assignment.status) ? "⚠ " : ""}
            Due {formatDate(assignment.dueDate)}
          </span>
        )}
        {assignmentSubtasks.length > 0 && (
          <span className="subtask-count">
            {completedCount} / {assignmentSubtasks.length} subtasks
          </span>
        )}
        <div className="assignment-detail-tabs" role="tablist" aria-label="Details">
          {tabs.map((tab, index) => {
            const isActive = hasExpandedDetails && activeDetailTab === tab.id;
            return (
              <button
                key={tab.id}
                id={`assignment-tab-${assignment.id}-${index}`}
                type="button"
                role="tab"
                className={`assignment-detail-tab ${isActive ? "active" : ""}`}
                aria-selected={isActive}
                aria-controls={detailsPanelId}
                tabIndex={isActive ? 0 : -1}
                onClick={() => openDetailTab(assignment.id, tab.id)}
                onKeyDown={(event) => handleTabKeyDown(event, index)}
              >
                {tab.prefix} {tab.label} <span className="detail-tab-count">{tab.count}</span>
                {tab.id === DETAIL_TABS.COMMENTS &&
                  checklistSummary.total > 0 && (
                    <span className="comments-checklist-summary">
                      {" "}
                      ☑ {checklistSummary.completed}/{checklistSummary.total}
                    </span>
                  )}
                {tab.id === DETAIL_TABS.COMMENTS &&
                  unread &&
                  !isCommentsTabActive && (
                    <span className="comments-new-indicator">New</span>
                  )}
              </button>
            );
          })}
        </div>
        {assignment.status !== 2 && (
          <button
            className="complete-button"
            onClick={() => handleCompleteAssignment(assignment.id)}
          >
            Complete
          </button>
        )}
        <button
          type="button"
          className="assignment-details-chevron"
          aria-expanded={hasExpandedDetails}
          aria-controls={detailsPanelId}
          onClick={() => toggleAssignmentDetails(assignment.id)}
          title={hasExpandedDetails ? "Collapse details" : "Expand details"}
        >
          {hasExpandedDetails ? "▾" : "▸"}
        </button>
      </div>

      {hasExpandedDetails && (
        <div id={detailsPanelId} role="tabpanel" className="assignment-details-panel">
          {isCommentsTabActive && (
            <CommentsSection
              assignmentId={assignment.id}
              assignmentTitle={assignment.title}
              assignmentDescription={assignment.description}
              assignmentSubtasks={assignmentSubtasks}
              commentsForAssignment={comments[assignment.id]}
              loadingComments={loadingComments[assignment.id]}
              filter={commentFilters[assignment.id]}
              setCommentFilters={setCommentFilters}
              commentSortNewestFirst={commentSortNewestFirst}
              setCommentSortNewestFirst={setCommentSortNewestFirst}
              commentFlags={commentFlags}
              commentNotes={commentNotes}
              commentNoteTimestamps={commentNoteTimestamps}
              editingCommentNotes={editingCommentNotes}
              setEditingCommentNotes={setEditingCommentNotes}
              setCommentNotes={setCommentNotes}
              setCommentNoteTimestamps={setCommentNoteTimestamps}
              sortCommentsByDate={sortCommentsByDate}
              currentUser={currentUser}
              getUserColor={getUserColor}
              formatCommentDate={formatCommentDate}
              handleToggleCommentFlag={handleToggleCommentFlag}
              handleUpdateCommentNote={handleUpdateCommentNote}
              handleDeleteCommentNote={handleDeleteCommentNote}
              aiSummaryState={aiSummaryStateByAssignment?.[assignment.id]}
              setAiSummaryStateByAssignment={setAiSummaryStateByAssignment}
              onChecklistSummaryChange={(summary) =>
                setCommentChecklistSummaries((prev) => ({
                  ...prev,
                  [assignment.id]: summary
                }))
              }
              newCommentText={newCommentText[assignment.id]}
              setNewCommentText={setNewCommentText}
              handleAddComment={handleAddComment}
            />
          )}

          {isAttachmentsTabActive && (
            <AttachmentsSection
              assignmentId={assignment.id}
              attachmentsForAssignment={attachments[assignment.id]}
              loadingAttachments={loadingAttachments[assignment.id]}
              uploadingAttachment={uploadingAttachment[assignment.id]}
              previewingAttachment={previewingAttachment}
              handleDownloadAttachment={handleDownloadAttachment}
              handlePreviewAttachment={handlePreviewAttachment}
              handleUploadAttachment={handleUploadAttachment}
            />
          )}

          {isSubtasksTabActive && (
            <SubtasksSection
              assignmentId={assignment.id}
              subtasksForAssignment={subtasks[assignment.id]}
              loadingSubtasks={loadingSubtasks[assignment.id]}
              draggedSubtaskId={draggedSubtaskId}
              dragOverSubtaskId={dragOverSubtaskId}
              editingSubtasks={editingSubtasks}
              subtaskTitleDrafts={subtaskTitleDrafts}
              editingNotes={editingNotes}
              subtaskNotes={subtaskNotes}
              subtaskNoteTimestamps={subtaskNoteTimestamps}
              handleDragStart={handleDragStart}
              handleDragOver={handleDragOver}
              handleDragLeave={handleDragLeave}
              handleDrop={handleDrop}
              handleDragEnd={handleDragEnd}
              handleToggleSubtask={handleToggleSubtask}
              setSubtaskTitleDrafts={setSubtaskTitleDrafts}
              handleSaveSubtaskTitle={handleSaveSubtaskTitle}
              handleCancelSubtaskEdit={handleCancelSubtaskEdit}
              handleStartSubtaskEdit={handleStartSubtaskEdit}
              setEditingNotes={setEditingNotes}
              setSubtaskNotes={setSubtaskNotes}
              handleDeleteSubtask={handleDeleteSubtask}
              handleUpdateSubtaskNote={handleUpdateSubtaskNote}
              handleDeleteSubtaskNote={handleDeleteSubtaskNote}
              newSubtaskText={newSubtaskText[assignment.id]}
              setNewSubtaskText={setNewSubtaskText}
              handleAddSubtask={handleAddSubtask}
            />
          )}
        </div>
      )}
    </div>
  );
}

export default AssignmentCard;
