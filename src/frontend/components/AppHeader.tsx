"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Toolbar from "@mui/material/Toolbar";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import AnalyticsOutlinedIcon from "@mui/icons-material/AnalyticsOutlined";
import GraphicEqIcon from "@mui/icons-material/GraphicEq";
import QueueMusicOutlinedIcon from "@mui/icons-material/QueueMusicOutlined";

const navigation = [
  { href: "/", label: "Transcriptions", icon: <QueueMusicOutlinedIcon fontSize="small" /> },
  { href: "/analysis", label: "Analyse qualité", icon: <AnalyticsOutlinedIcon fontSize="small" /> },
];

export default function AppHeader() {
  const pathname = usePathname();

  return (
    <AppBar position="sticky" elevation={0} color="inherit" sx={{ borderBottom: 1, borderColor: "divider" }}>
      <Container maxWidth="xl">
        <Toolbar disableGutters sx={{ minHeight: 68, gap: { xs: 1, sm: 3 } }}>
          <Box sx={{ display: "flex", alignItems: "center", gap: 1.25, mr: "auto" }}>
            <Box
              sx={{
                width: 36,
                height: 36,
                display: "grid",
                placeItems: "center",
                bgcolor: "primary.main",
                color: "primary.contrastText",
                borderRadius: 1.5,
              }}
            >
              <GraphicEqIcon />
            </Box>
            <Box>
              <Typography variant="subtitle1" sx={{ fontWeight: 800, lineHeight: 1.1, whiteSpace: "nowrap", fontSize: { xs: 14, sm: 16 } }}>
                Transcrib-S2T
              </Typography>
              <Typography variant="caption" color="text.secondary" sx={{ display: { xs: "none", sm: "block" } }}>
                Audio, transcription et qualité
              </Typography>
            </Box>
          </Box>

          <Box component="nav" aria-label="Navigation principale" sx={{ display: "flex", gap: 0.75 }}>
            {navigation.map((item) => {
              const active = item.href === "/" ? pathname === "/" : pathname.startsWith(item.href);
              return (
                <Tooltip key={item.href} title={item.label}>
                  <Button
                    component={Link}
                    href={item.href}
                    startIcon={item.icon}
                    color={active ? "primary" : "inherit"}
                    variant={active ? "contained" : "text"}
                    aria-current={active ? "page" : undefined}
                    aria-label={item.label}
                    sx={{
                      whiteSpace: "nowrap",
                      minWidth: { xs: 40, sm: 64 },
                      px: { xs: 1, sm: 2 },
                      "& .MuiButton-startIcon": { m: { xs: 0, sm: "0 8px 0 -4px" } },
                    }}
                  >
                    <Box component="span" sx={{ display: { xs: "none", sm: "inline" } }}>
                      {item.label}
                    </Box>
                  </Button>
                </Tooltip>
              );
            })}
          </Box>
        </Toolbar>
      </Container>
    </AppBar>
  );
}
