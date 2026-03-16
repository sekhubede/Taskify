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
  openComments,
  toggleComments,
  subtasks,
  openSubtasks,
  toggleSubtasks,
  attachmentCounts,
  attachments,
  openAttachments,
  toggleAttachments,
  comments,
  loadingComments,
  commentFilters,
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
  const unread = hasUnreadComments(assignment.id);
  const assignmentSubtasks = subtasks[assignment.id] || assignment.subtasks || [];
  const completedCount = assignmentSubtasks.filter((s) => s.isCompleted).length;
  const assignedToLabel =
    assignment.assignedTo && assignment.assignedTo.trim().length > 0
      ? assignment.assignedTo
      : "Unknown";

  return (
    <div
      className={`assignment-card ${isWorkingOnCard ? "working-on" : ""} ${unread ? "has-unread-comments" : ""}`}
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
            <span className="assignment-unread-comments-badge">New comments</span>
          )}
        </div>
        <span
          className="status-badge"
          style={{ backgroundColor: getStatusColor(assignment.status) }}
        >
          {getStatusLabel(assignment.status)}
        </span>
      </div>

      <p className="assignment-assignee-line" title={`Assigned to: ${assignedToLabel}`}>
        Assigned to: {assignedToLabel}
      </p>

      {assignment.description && (
        <p className="assignment-description">{assignment.description}</p>
      )}

      <div className="assignment-meta">
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
        <button
          className="comments-button"
          onClick={() => toggleComments(assignment.id)}
        >
          💬 {commentCounts[assignment.id] || 0}{" "}
          {openComments[assignment.id] ? "Hide" : "Comments"}
          {!openComments[assignment.id] && unread && (
            <span className="comments-new-indicator">New</span>
          )}
        </button>
        <button
          className="subtasks-button"
          onClick={() => toggleSubtasks(assignment.id)}
        >
          ✓ {subtasks[assignment.id]?.length ?? assignment.subtasks?.length ?? 0}{" "}
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
          {openAttachments[assignment.id] ? "Hide" : "Attachments"}
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
        <CommentsSection
          assignmentId={assignment.id}
          commentsForAssignment={comments[assignment.id]}
          loadingComments={loadingComments[assignment.id]}
          filter={commentFilters[assignment.id]}
          setCommentFilters={setCommentFilters}
          commentSortNewestFirst={commentSortNewestFirst}
          setCommentSortNewestFirst={setCommentSortNewestFirst}
          commentFlags={commentFlags}
          commentNotes={commentNotes}
          editingCommentNotes={editingCommentNotes}
          setEditingCommentNotes={setEditingCommentNotes}
          setCommentNotes={setCommentNotes}
          sortCommentsByDate={sortCommentsByDate}
          currentUser={currentUser}
          getUserColor={getUserColor}
          formatCommentDate={formatCommentDate}
          handleToggleCommentFlag={handleToggleCommentFlag}
          handleUpdateCommentNote={handleUpdateCommentNote}
          handleDeleteCommentNote={handleDeleteCommentNote}
          newCommentText={newCommentText[assignment.id]}
          setNewCommentText={setNewCommentText}
          handleAddComment={handleAddComment}
        />
      )}

      {openAttachments[assignment.id] && (
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

      {openSubtasks[assignment.id] && (
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
  );
}

export default AssignmentCard;
