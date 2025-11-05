import React, { useEffect, useState } from "react";

const ASSIGNMENTS_API_URL = "http://localhost:5000/api/assignments";

function App() {
  const [assignments, setAssignments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetch(ASSIGNMENTS_API_URL)
      .then((res) => {
        if (!res.ok) throw new Error("Failed to fetch assignments");
        return res.json();
      })
      .then((data) => {
        setAssignments(data);
        setLoading(false);
      })
      .catch((err) => {
        setError(err.toString());
        setLoading(false);
      });
  }, []);

  return (
    <div>
      <h1>Taskify</h1>
      {loading && <p>Loading assignments...</p>}
      {error && <p style={{ color: "red" }}>Error: {error}</p>}
      {!loading && !error && (
        <ul>
          {assignments.map((assignment) => (
            <li key={assignment.id}>
              <strong>{assignment.title}</strong> - Status: {assignment.status}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default App;