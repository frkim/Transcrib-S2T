"use client";

import { createTheme } from "@mui/material/styles";

/**
 * Shared Material UI theme giving the app a professional, consistent look.
 */
const theme = createTheme({
  palette: {
    mode: "light",
    primary: { main: "#1565c0" },
    secondary: { main: "#00897b" },
    background: { default: "#f4f6f8" },
  },
  shape: { borderRadius: 10 },
  typography: {
    fontFamily:
      "var(--font-geist-sans), Roboto, Arial, Helvetica, sans-serif",
    h1: { fontSize: "2rem", fontWeight: 700 },
    h2: { fontSize: "1.25rem", fontWeight: 600 },
  },
});

export default theme;
