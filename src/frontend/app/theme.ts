"use client";

import { createTheme } from "@mui/material/styles";

/**
 * Shared Material UI theme giving the app a professional, consistent look.
 */
const theme = createTheme({
  palette: {
    mode: "light",
    primary: { main: "#b4232c", dark: "#7f1d1d", contrastText: "#ffffff" },
    secondary: { main: "#176b87", dark: "#0e4f66" },
    background: { default: "#f6f7f8", paper: "#ffffff" },
    text: { primary: "#17202a", secondary: "#59636e" },
    divider: "#dfe3e7",
  },
  shape: { borderRadius: 8 },
  typography: {
    fontFamily: "var(--font-geist-sans), sans-serif",
    h1: { fontSize: "clamp(2rem, 4vw, 3rem)", fontWeight: 760, lineHeight: 1.08, letterSpacing: 0 },
    h2: { fontSize: "1.25rem", fontWeight: 750, letterSpacing: 0 },
    button: { fontWeight: 700, textTransform: "none", letterSpacing: 0 },
  },
  components: {
    MuiPaper: { styleOverrides: { root: { backgroundImage: "none" } } },
    MuiButton: { defaultProps: { disableElevation: true } },
    MuiTableHead: { styleOverrides: { root: { backgroundColor: "#f1f3f5" } } },
  },
});

export default theme;
