import React from "react";

function AttachmentsSection({
  assignmentId,
  attachmentsForAssignment,
  loadingAttachments,
  uploadingAttachment,
  previewingAttachment,
  handleDownloadAttachment,
  handlePreviewAttachment,
  handleUploadAttachment
}) {
  const items = attachmentsForAssignment || [];

  return (
    <div className="attachments-section">
      <div className="attachments-list">
        {loadingAttachments ? (
          <div className="attachments-loading">Loading attachments...</div>
        ) : items.length > 0 ? (
          items.map((attachment) => (
            <div key={attachment.id} className="attachment-item">
              <div className="attachment-actions">
                <button
                  className="attachment-link"
                  onClick={() => handleDownloadAttachment(assignmentId, attachment)}
                  title={`Download ${attachment.fileName}`}
                >
                  {attachment.fileName}
                </button>
                <button
                  className="attachment-preview-button"
                  onClick={() => handlePreviewAttachment(assignmentId, attachment)}
                  disabled={previewingAttachment}
                  title={`Preview ${attachment.fileName}`}
                >
                  {previewingAttachment ? "Opening..." : "Preview"}
                </button>
              </div>
              <span className="attachment-size">
                {Math.max(1, Math.round(attachment.sizeBytes / 1024))} KB
              </span>
            </div>
          ))
        ) : (
          <div className="attachments-empty">No attachments yet.</div>
        )}
      </div>
      <div className="attachment-upload-container">
        <input
          type="file"
          className="attachment-upload-input"
          onChange={(e) => {
            const file = e.target.files?.[0];
            if (file) {
              handleUploadAttachment(assignmentId, file);
              e.target.value = "";
            }
          }}
          disabled={uploadingAttachment}
        />
        {uploadingAttachment && (
          <span className="attachment-uploading">Uploading...</span>
        )}
      </div>
    </div>
  );
}

export default AttachmentsSection;
