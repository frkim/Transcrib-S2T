"use client";

import { useCallback, useEffect, useState } from "react";
import AppBar from "@mui/material/AppBar";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Paper from "@mui/material/Paper";
import Stack from "@mui/material/Stack";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import GraphicEqIcon from "@mui/icons-material/GraphicEq";
import type { TranscriptionJob } from "@/lib/types";
import { deleteJob, listJobs } from "@/lib/api";
import UploadForm from "@/components/UploadForm";
import JobList from "@/components/JobList";

export default function Home() {
  const [jobs, setJobs] = useState<TranscriptionJob[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const data = await listJobs();
        if (active) {
          setJobs(data);
          setError(null);
        }
      } catch (err) {
        if (active) {
          setError(err instanceof Error ? err.message : "Failed to load jobs.");
        }
      }
    }

    // Initial load plus periodic polling so statuses update over time.
    void load();
    const timer = setInterval(() => void load(), 5000);
    return () => {
      active = false;
      clearInterval(timer);
    };
  }, [reloadToken]);

  const handleDelete = useCallback(async (jobId: string) => {
    try {
      await deleteJob(jobId);
      setJobs((current) => current.filter((job) => job.id !== jobId));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete job.");
    }
  }, []);

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
      <AppBar position="static" elevation={0}>
        <Toolbar>
          <GraphicEqIcon sx={{ mr: 1.5 }} />
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            Transcrib-S2T
          </Typography>
        </Toolbar>
      </AppBar>

      <Container maxWidth="md" sx={{ py: 4 }}>
        <Stack spacing={4}>
          <Box>
            <Typography variant="h1" gutterBottom>
              Speech-to-Text Transcription
            </Typography>
            <Typography color="text.secondary">
              Upload one or more MP3 files to generate speech-to-text
              transcripts.
            </Typography>
          </Box>

          <Paper variant="outlined" sx={{ p: 3 }}>
            <Typography variant="h2" gutterBottom>
              Upload
            </Typography>
            <UploadForm onUploaded={() => setReloadToken((t) => t + 1)} />
          </Paper>

          <Box>
            <Typography variant="h2" gutterBottom>
              Jobs
            </Typography>
            {error && (
              <Alert severity="error" role="alert" sx={{ mb: 2 }}>
                {error}
              </Alert>
            )}
            <JobList jobs={jobs} onDelete={handleDelete} />
          </Box>
        </Stack>
      </Container>
    </Box>
  );
}
