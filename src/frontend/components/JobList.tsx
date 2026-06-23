"use client";

import { useState } from "react";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import IconButton from "@mui/material/IconButton";
import Paper from "@mui/material/Paper";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import DownloadIcon from "@mui/icons-material/Download";
import type { JobStatus, TranscriptionJob } from "@/lib/types";
import { canDownloadTranscript, statusLabel } from "@/lib/jobs";
import { transcriptUrl } from "@/lib/api";

interface JobListProps {
  jobs: TranscriptionJob[];
  onDelete?: (jobId: string) => void | Promise<void>;
}

type ChipColor = "default" | "info" | "success" | "error" | "warning";

function statusColor(status: JobStatus): ChipColor {
  switch (status) {
    case "Processing":
      return "info";
    case "Completed":
      return "success";
    case "Failed":
      return "error";
    case "Purged":
      return "warning";
    default:
      return "default";
  }
}

function StatusCell({ status }: { status: JobStatus }) {
  if (status === "Processing") {
    return (
      <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
        <CircularProgress size={16} aria-label="processing" />
        <Chip
          label={statusLabel(status)}
          color={statusColor(status)}
          size="small"
          variant="outlined"
          data-status={status}
        />
      </Box>
    );
  }

  return (
    <Chip
      label={statusLabel(status)}
      color={statusColor(status)}
      size="small"
      data-status={status}
    />
  );
}

export default function JobList({ jobs, onDelete }: JobListProps) {
  const [deletingId, setDeletingId] = useState<string | null>(null);

  if (jobs.length === 0) {
    return (
      <Typography color="text.secondary">No transcription jobs yet.</Typography>
    );
  }

  async function handleDelete(jobId: string) {
    if (!onDelete) {
      return;
    }
    setDeletingId(jobId);
    try {
      await onDelete(jobId);
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <TableContainer component={Paper} variant="outlined">
      <Table aria-label="transcription jobs">
        <TableHead>
          <TableRow>
            <TableCell>File</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Created</TableCell>
            <TableCell>Transcript</TableCell>
            <TableCell align="right">Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {jobs.map((job) => (
            <TableRow key={job.id} hover>
              <TableCell>{job.fileName}</TableCell>
              <TableCell>
                <StatusCell status={job.status} />
              </TableCell>
              <TableCell>{new Date(job.createdAt).toLocaleString()}</TableCell>
              <TableCell>
                {canDownloadTranscript(job.status) ? (
                  <Tooltip title="Download transcript">
                    <IconButton
                      component="a"
                      href={transcriptUrl(job.id)}
                      aria-label={`download transcript for ${job.fileName}`}
                      size="small"
                      color="primary"
                    >
                      <DownloadIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                ) : job.status === "Failed" ? (
                  <Typography
                    component="span"
                    color="text.secondary"
                    title={job.error ?? undefined}
                  >
                    —
                  </Typography>
                ) : (
                  <Typography component="span" color="text.secondary">
                    —
                  </Typography>
                )}
              </TableCell>
              <TableCell align="right">
                <Tooltip title="Delete job">
                  <span>
                    <IconButton
                      aria-label={`delete ${job.fileName}`}
                      size="small"
                      color="error"
                      disabled={!onDelete || deletingId === job.id}
                      onClick={() => handleDelete(job.id)}
                    >
                      {deletingId === job.id ? (
                        <CircularProgress size={16} />
                      ) : (
                        <DeleteOutlineIcon fontSize="small" />
                      )}
                    </IconButton>
                  </span>
                </Tooltip>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
