// Takes "09/06/2026", returns a Date object
export function parseDDMMYYYY(dateString) {
  if (!dateString) return null;

  const parts = dateString.split("/");
  if (parts.length !== 3) return null;

  const day = parseInt(parts[0], 10);
  // Subtract 1 from the month because JavaScript months are 0-indexed (Jan = 0)
  const month = parseInt(parts[1], 10) - 1;
  const year = parseInt(parts[2], 10);

  return new Date(year, month, day);
}

// Takes a Date object, returns "June 9, 2026"
export function formatDate(date) {
  const options = {
    month: "long",
    day: "numeric",
    year: "numeric"
  };

  return date.toLocaleDateString("en-US", options);
}
