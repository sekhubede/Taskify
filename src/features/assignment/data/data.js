/**
 * @typedef {object} Comment
 * @property {number} id
 * @property {string} author
 * @property {string} date - DD/MM/YYYY
 * @property {string} text
 *
 * @typedef {object} Subtask
 * @property {number} id
 * @property {string} text
 * @property {string} createdAt - DD/MM/YYYY
 * @property {boolean} done
 *
 * @typedef {object} Note
 * @property {number} id
 * @property {string} text
 * @property {string} createdAt - DD/MM/YYYY
 */

/**
 * @typedef {object} Assignment
 * @property {number} id - M-Files Object ID
 * @property {string} title - Assignment title
 * @property {string} description - Assignment description
 * @property {string} client - O'Neil client name
 * @property {number} priority - 1=Critical, 2=High, 3=Medium, 4=Low, 5=Not Determined
 * @property {string} status - M-Files workflow state. See ASSIGNMENT_STATES
 * @property {string} assignee - Assigned team member
 * @property {string} deadline - ISO 8601 date string
 * @property {Array<Comment>} comments - Version-specific comments on the assignment
 * @property {Array<Subtask>} subtasks - Actionable sub-items for this assignment
 * @property {Array<Note>} notes - Internal notes on the assignment
 * @property {string} reminder - Assignment reminder for notification
 */

export const PRIORITY_LABELS = {
  1: "Critical",
  2: "High",
  3: "Medium",
  4: "Low",
  5: "Not Determined Yet"
};

export const ASSIGNMENT_STATES = {
  ASSIGNED: "Assigned",
  // User reviews description, moves to In Progress when understood
  // TODO: V2 - AI breakdown of assignment into actionables

  IN_PROGRESS: "In Progress",
  // User is actively working on the assignment

  ON_HOLD: "On Hold",
  // Blocked - waiting on client response or external dependency

  UPDATE_REQUIRED: "Update Required",
  // Manager/HOD needs an update from assignee
  // TODO: v2 - UI alert to replace current email notification (email unreliable)

  AWAITING_REVIEW: "Awaiting Review",
  // User submits for review, Approval Assignment created and linked
  // Both team lead and HOD must approve
  // On rejection: returns to In Progress, user checks assignment + approval for comments

  APPROVED: "Approved",
  // Dual approval complete, user can mark as complete

  COMPLETED: "Completed",
  // Assignment marked complete by user

  BILLED: "Billed"
  // Completed and billed, archive state
};

export const ASSIGNMENTS = [
  {
    id: 1,
    title: "2026/07/09 NBN - M FILES Demo - Management",
    description:
      "Prepare a tailored M-Files demonstration for Nedbank Namibia's executive and senior management team, highlighting key M-Files capabilities and opportunities to complement existing Document Warehouse solutions. Attached please see screenshot of specifications.",
    client: "N039 NEDBANK NAMIBIA LIMITED - WINDHOEK",
    priority: 1,
    status: ASSIGNMENT_STATES.ASSIGNED,
    assignee: "Fiina Amupolo",
    deadline: "09/07/2026",
    comments: [
      {
        id: 1,
        author: "Fiina Amupolo",
        date: "10/07/2026",
        text: "Initial draft prepared, waiting for feedback."
      },
      {
        id: 2,
        author: "John Doe",
        date: "11/07/2026",
        text: "Please include the ROI section."
      }
    ],
    subtasks: [
      {
        id: 1,
        text: "Prepare slide deck",
        createdAt: "08/07/2026",
        done: true
      },
      {
        id: 2,
        text: "Schedule demo room",
        createdAt: "09/07/2026",
        done: false
      },
      {
        id: 3,
        text: "Send agenda to attendees",
        createdAt: "09/07/2026",
        done: false
      }
    ],
    notes: [
      { id: 1, text: "Meeting rescheduled to 10AM", createdAt: "08/07/2026" },
      { id: 2, text: "Client confirmed attendance", createdAt: "09/07/2026" }
    ],
    reminder: "2 hours before"
  },
  {
    id: 2,
    title: "2026/04/24 Homeloan Data Import & Validation",
    description:
      "🎯 Objective Perform a data import and validation within the client environment using the provided spreadsheet.🧾Overview▪️ Primary Tasks:◽Data Import.◽Validation: Conduct a thorough double-check and final confirmation of all entries. ▪️ System Access: This task requires active credentials/access to the client environment. ▪️ Data Source: Refer to the attached spreadsheet for all specific entry details.",
    client: "F032 FNB FIDUCIARY (NAMIBIA) (PTY) LTD",
    priority: 2,
    status: ASSIGNMENT_STATES.IN_PROGRESS,
    assignee: "Casey Damens",
    deadline: "24/04/2026",
    comments: [
      {
        id: 1,
        author: "Casey Damens",
        date: "20/04/2026",
        text: "Data import complete, starting validation."
      }
    ],
    subtasks: [
      {
        id: 1,
        text: "Import data from CSV",
        createdAt: "20/04/2026",
        done: true
      },
      {
        id: 2,
        text: "Validate all entries",
        createdAt: "20/04/2026",
        done: false
      },
      {
        id: 3,
        text: "Final confirmation",
        createdAt: "20/04/2026",
        done: false
      }
    ],
    notes: [
      {
        id: 1,
        text: "Waiting on IT, to provide access to the folder with the CSV.",
        createdAt: "20/04/2026"
      }
    ],
    reminder: "End of Day"
  },
  {
    id: 3,
    title: "2026/07/21 LSN Member Listing",
    description:
      "Compile a list of all members with their details as recorded on M-Files. After compiling the list, do a data validation and import on the attached member listing. Billable: Please record time spent",
    client: "T090 THE LAW SOCIETY OF NAMIBIA",
    priority: 3,
    status: ASSIGNMENT_STATES.ON_HOLD,
    assignee: "Johanna Hosea",
    deadline: "21/07/2026",
    comments: [],
    subtasks: [
      {
        id: 1,
        text: "Export member list",
        createdAt: "21/07/2026",
        done: false
      },
      { id: 2, text: "Validate data", createdAt: "21/07/2026", done: false }
    ],
    notes: [
      { id: 1, text: "Other tasks taking priority.", createdAt: "22/07/2026" }
    ],
    reminder: null
  },
  {
    id: 4,
    title: "2026/06/24  Case Management CMS - Case summary for Dashboard",
    description:
      " Consolidated Dashboards: Developing a single interactive tab capable of aggregating and displaying metadata from multiple objects simultaneously. Allow for for creation of Cases, Case summary and Complaints. Refer to the linked document of a CMS system used by the client.",
    client: "M003 MINISTRY OF JUSTICE",
    priority: 4,
    status: ASSIGNMENT_STATES.UPDATE_REQUIRED,
    assignee: "Malakia Jeremia",
    deadline: "24/06/2026",
    comments: [
      {
        id: 1,
        author: "Malakia Jeremia",
        date: "24/06/2026",
        text: "Stuck on installing tools for development. Following up with IT."
      }
    ],
    subtasks: [],
    notes: [],
    reminder: null
  },
  {
    id: 5,
    title: "2026/09/30 Retrievals Process - Dev",
    description:
      "To create, maintain, and improve systems that support the retrieval process. Including automating request tracking, integrating databases, and ensuring accurate document or data retrieval.  Also troubleshoot issues, enhance system performance, and support users throughout the retrieval workflow.",
    client: "T009 TDW NAMIBIA",
    priority: 5,
    status: ASSIGNMENT_STATES.AWAITING_REVIEW,
    assignee: "Michael Sekhubede",
    deadline: "30/09/2026",
    comments: [],
    subtasks: [],
    notes: [],
    reminder: null
  },
  {
    id: 6,
    title: "2026/12/18 VAF for resale",
    description:
      "Add to TDW's revenue by preparing at least 1 VAF for resale. Keep comments up to date (minimum weekly) on progress made.",
    client: "T009 TDW NAMIBIA",
    priority: 3,
    status: ASSIGNMENT_STATES.APPROVED,
    assignee: "Denilson Uariua",
    deadline: "18/12/2026",
    comments: [],
    subtasks: [],
    notes: [],
    reminder: null
  },
  {
    id: 7,
    title: "2026/06/22 Namib Mills Validation - Initials & Name",
    description:
      'Use the Namib Mills vault to conduct a validation across the "Employee Name" property definition. This should be a combination of the Surname & Initials. At times, it was noted that the initials composed of the first letter of the First Name & Surname, which is incorrect. The initials should only take from the first letter of any First/Second Names that the person has, and not include the surname. First test locally to ensure that validation process covers all steps including identifying all fields that need to be updates, as well as how the fields would be updated. Then have this process approved before moving it into production. Save validation steps taken in a document, as well as any findings, and link this to the assignment as separate documents.',
    client: "N005 NAMIB MILLS (PTY) LTD",
    priority: 2,
    status: ASSIGNMENT_STATES.COMPLETED,
    assignee: "David Van Rooyen",
    deadline: "22/06/2026",
    comments: [],
    subtasks: [],
    notes: [],
    reminder: null
  },
  {
    id: 8,
    title:
      "2026/05/27 Monthly Task: Namib Mills Vault - Data Validation (May 2026)",
    description:
      "Use this object to record time spent throughout the month for billing purposes, everytime data import/export/validation is done & to record Work Order Number. Use Purchase Order depending on the month - N005",
    client: "N005 NAMIB MILLS (PTY) LTD",
    priority: 2,
    status: ASSIGNMENT_STATES.BILLED,
    assignee: "Casey Damens",
    deadline: "27/05/2026",
    comments: [],
    subtasks: [],
    notes: [],
    reminder: null
  }
];
