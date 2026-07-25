"use client";

import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import type { StepResult } from "@/lib/discourse-analysis";

const colors = ["#c62828", "#1565c0", "#2e7d32", "#ed6c02", "#6a1b9a", "#00838f", "#263238"];

export default function AnalysisChart({ steps }: { steps: StepResult[] }) {
  return (
    <Box role="img" aria-label="Scores des sept étapes du discours" sx={{ display: "grid", gap: 1.5 }}>
      {steps.map((step, index) => (
        <Box key={step.id} sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "170px 1fr 52px" }, gap: 1, alignItems: "center" }}>
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            {step.shortName}
          </Typography>
          <Box sx={{ height: 16, bgcolor: "action.hover", borderRadius: 0.75, overflow: "hidden" }}>
            <Box
              sx={{
                width: `${step.score}%`,
                height: "100%",
                bgcolor: colors[index % colors.length],
                transition: "width 360ms ease",
              }}
            />
          </Box>
          <Typography variant="body2" sx={{ fontWeight: 700, fontVariantNumeric: "tabular-nums" }}>
            {step.score}/100
          </Typography>
        </Box>
      ))}
    </Box>
  );
}
